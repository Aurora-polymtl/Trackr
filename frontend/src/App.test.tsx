// @vitest-environment jsdom

import { afterEach, expect, it, vi } from "vitest"
import { cleanup, render, screen } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import App from "./App"

const session = {
    userId: 'user-1',
    email: 'alice@example.com',
    accessToken: 'test-token',
}

const project = {
    id: 1,
    name: 'Project Alpha',
    description: 'Premier projet',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
    isOwner: true,
}

function jsonResponse(body: unknown) {
    return { ok: true, status: 200, json: async () => body }
}

async function submitLogin() {
    const user = userEvent.setup()
    render(<App />)
    await user.type(screen.getByLabelText('Adresse courriel'), 'alice@example.com')
    await user.type(screen.getByLabelText('Mot de passe'), 'Password123!')
    await user.click(screen.getByRole('button', { name: 'Se connecter' }))

    return user
}

afterEach(() => {
    cleanup()
    vi.unstubAllGlobals()
})

it('affiche les projets après la connexion avec le jeton JWT', async () => {
    const fetchMock = vi.fn()
        .mockResolvedValueOnce(jsonResponse(session))
        .mockResolvedValueOnce(jsonResponse([project]))
    vi.stubGlobal('fetch', fetchMock)

    await submitLogin()

    expect(await screen.findByText('Project Alpha')).not.toBeNull()
    expect(screen.getByText('Propriétaire')).not.toBeNull()
    expect(fetchMock).toHaveBeenNthCalledWith(1, '/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ 
            email: 'alice@example.com', 
            password: 'Password123!' 
        }),
    })
    expect(fetchMock).toHaveBeenNthCalledWith(2, '/api/projects', {
        headers: { 'Authorization': 'Bearer test-token' },
    })
})

it('affiche une erreur sans charger les projets si la connexion échoue', async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce({ ok: false, status: 401 })
    vi.stubGlobal('fetch', fetchMock)

    await submitLogin()

    expect(await screen.findByRole('alert')).toHaveProperty('textContent', 'Adresse courriel ou mot de passe incorrect.')
    expect(screen.getByRole('heading', { name: 'Connexion' })).not.toBeNull()
    expect(fetchMock).toHaveBeenCalledTimes(1)
})

it('actualise les projets puis revient à la connexion après la déconnexion', async () => {
    const fetchMock = vi.fn()
        .mockResolvedValueOnce(jsonResponse(session))
        .mockResolvedValueOnce(jsonResponse([project]))
        .mockResolvedValueOnce(jsonResponse([
            { ...project, id: 2, name: 'Project Beta', isOwner: false },
        ]))
    vi.stubGlobal('fetch', fetchMock)

    const user = await submitLogin()
    await screen.findByText('Project Alpha')

    await user.click(screen.getByRole('button', { name: 'Actualiser' }))
    expect(await screen.findByText('Project Beta')).not.toBeNull()
    expect(screen.queryByText('Project Alpha')).toBeNull()
    expect(screen.getByText('Membre')).not.toBeNull()

    await user.click(screen.getByRole('button', { name: 'Déconnexion' }))
    expect(screen.getByRole('heading', { name: 'Connexion' })).not.toBeNull()
    expect(screen.queryByText('Project Beta')).toBeNull()
})

it('ouvre un projet et parcourt ses pages d’issues', async () => {
    const firstPage = {
        items: [{ id: 1, title: 'Première issue', description: '',
            status: 'Todo', priority: 'High' }],
        page: 1, pageSize: 10, totalCount: 11, totalPages: 2,
    }
    const secondPage = {
        ...firstPage,
        items: [{ id: 2, title: 'Deuxième issue', description: '',
            status: 'Done', priority: 'Low' }],
        page: 2,
    }
    const fetchMock = vi.fn()
        .mockResolvedValueOnce(jsonResponse(session))
        .mockResolvedValueOnce(jsonResponse([project]))
        .mockResolvedValueOnce(jsonResponse(firstPage))
        .mockResolvedValueOnce(jsonResponse(secondPage))
    vi.stubGlobal('fetch', fetchMock)

    const user = await submitLogin()
    await screen.findByText('Project Alpha')
    await user.click(screen.getByRole('button', { name: 'Project Alpha' }))

    expect(await screen.findByText('Première issue')).not.toBeNull()
    expect(fetchMock).toHaveBeenNthCalledWith(3, '/api/projects/1/issues?page=1', {
        headers: { Authorization: 'Bearer test-token' },
    })

    await user.click(screen.getByRole('button', { name: 'Suivante' }))
    expect(await screen.findByText('Deuxième issue')).not.toBeNull()
    expect(screen.queryByText('Première issue')).toBeNull()
    expect(fetchMock).toHaveBeenNthCalledWith(4, '/api/projects/1/issues?page=2', {
        headers: { Authorization: 'Bearer test-token' },
    })

    await user.click(screen.getByRole('button', { name: 'Retour aux projets' }))
    expect(screen.getByRole('heading', { name: 'Projets' })).not.toBeNull()
})