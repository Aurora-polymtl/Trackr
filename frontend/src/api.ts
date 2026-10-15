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

async function sendRequest(url: string, options: RequestInit, unauthorizedMessage: string): Promise<Response> {
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
    return response
}

async function request<T>(url: string, options: RequestInit, unauthorizedMessage: string): Promise<T> {
    const response = await sendRequest(url, options, unauthorizedMessage)
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

export type Issue = {
    id: number
    title: string
    description: string
    status: 'Backlog' | 'Todo' | 'InProgress' | 'Review' | 'Done'
    priority: 'Low' | 'Medium' | 'High' | 'Critical'
    dueDate: string | null
    createdAt: string
    updatedAt: string
    projectId: number
    assigneeId: string | null
}

export type PagedResponse<T> = {
    items: T[]
    page: number
    pageSize: number
    totalCount: number
    totalPages: number
}

export function getProjectIssues(token: string, projectId: number, page: number) {
    return request<PagedResponse<Issue>>(
        `/api/projects/${projectId}/issues?page=${page}`,
        { headers: { 'Authorization': `Bearer ${token}` } },
        'Session expirée. Veuillez vous reconnecter.',
    )
}

export type RegisterResponse = {
    id: string
    email: string
}

type RegisterProblem = {
    errors?: Record<string, string[]>
}

export async function signUp(email: string, password: string): Promise<RegisterResponse> {
    let response: Response
    try {
        response = await fetch('/api/auth/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password }),
        })
    } catch {
        throw new Error('API indisponible.')
    }

    if (response.status === 400) {
        const problem = (await response.json().catch(() => null)) as RegisterProblem | null
        const codes = Object.keys(problem?.errors ?? {})

        if (codes.includes('DuplicateEmail') || codes.includes('DuplicateUserName')) {
            throw new Error('Cette adresse courriel est déjà utilisée.')
        }
        if (codes.some((code) => code.startsWith('Password'))) {
            throw new Error('Le mot de passe ne respecte pas les exigences indiquées.')
        }
        throw new Error('Vérifiez les informations saisies et réessayez.')
    }

    if (!response.ok) {
        throw new Error('Inscription impossible pour le moment. Veuillez réessayer plus tard.') 
    }

    return response.json() as Promise<RegisterResponse>
}

export function createProject(token: string, name: string, description: string) {
    return request<Project>('/api/projects', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({name, description}),
    }, 'Session expirée. Veuillez vous reconnecter.')
}

export type CreateIssueInput = {
    title: string
    description: string
    priority: Issue['priority']
    dueDate: string | null
}

export function createIssue(token: string, projectId: number, input: CreateIssueInput) {
    return request<Issue>(`/api/projects/${projectId}/issues`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(input),
    }, 'Session expirée. Veuillez vous reconnecter.')
}

export async function updateIssueStatus(
    token: string,
    projectId: number,
    issueId: number,
    status: Issue['status'],
): Promise<void> {
    await sendRequest(
        `/api/projects/${projectId}/issues/${issueId}/status`,
        {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                Authorization: `Bearer ${token}`,
            },
            body: JSON.stringify({ status }),
        },
        'Session expirée. Veuillez vous reconnecter.',
    )
}