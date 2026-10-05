export type Session = {
    userId: string
    email: string
    accessToken: string
}

export type Project = {
    id: number
    name: string
    description: string
    createdAt: string
    updatedAt: string
    isOwner: boolean
}

async function request<T>(url: string, options: RequestInit, unauthorizedMessage: string): Promise<T> {
    let response: Response
    try {
        response = await fetch(url, options)
    } catch {
        throw new Error('API indisponible.')
    }

    if (response.status === 401) {
        throw new Error(unauthorizedMessage)
    }
    if (!response.ok) {
        throw new Error('La requête a échoué.')
    }
    return response.json() as Promise<T>
}

export function signIn(email: string, password: string) {
    return request<Session>('/api/auth/login', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email, password }),
    }, 'Adresse courriel ou mot de passe incorrect.') 
}

export function getProjects(token: string) {
    return request<Project[]>('/api/projects', {
        headers: {
            'Authorization': `Bearer ${token}`,
        },
    }, 'Session expirée. Veuillez vous reconnecter.')
}