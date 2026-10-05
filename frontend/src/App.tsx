import { useState, type SubmitEvent } from "react"
import { getProjects, signIn, type Project, type Session } from "./api"

function App() {
  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [session, setSession] = useState<Session | null>(null)
  const [projects, setProjects] = useState<Project[]>([])
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState("")

  async function handleLogin(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setError("")
    try {
      const nextSession = await signIn(email.trim(), password)
      const nextProjects = await getProjects(nextSession.accessToken)
      setSession(nextSession)
      setProjects(nextProjects)
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Erreur inattendue.')
    } finally {
      setBusy(false)
    }
  }

  async function refreshProjects() {
    if (!session) return
    setBusy(true)
    setError("")

    try {
      setProjects(await getProjects(session.accessToken))
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Erreur inattendue.')
    } finally {
      setBusy(false)
    }
  }

  function logout() {
    setSession(null)
    setProjects([])
    setError("")
  }

  if (!session) {
    return (
      <main className="login-page">
        <form className="login-form" onSubmit={handleLogin}>
          <strong className="brand">Trackr</strong>
          <h1>Connexion</h1>
          <label htmlFor="email">Adresse courriel</label>
          <input id="email" type="email" autoComplete="username" required
            value={email} onChange={(event) => setEmail(event.target.value)} />
          <label htmlFor="password">Mot de passe</label>
          <input id="password" type="password" autoComplete="current-password" required
            value={password} onChange={(event) => setPassword(event.target.value)} />
          {error && <p className="error" role="alert">{error}</p>}
          <button type="submit" disabled={busy}>{busy ? 'Connexion...' : 'Se connecter'}</button>
        </form>
      </main>
    )
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <strong className="brand">Trackr</strong>
        <div className="top-actions">
          <span>{session.email}</span>
          <button className="secondary" type="button" onClick={refreshProjects} disabled={busy}>Actualiser</button>
          <button className="secondary" type="button" onClick={logout} disabled={busy}>Déconnexion</button>
        </div>
      </header>
      <main className="content">
        <div className="content-heading">
          <h1>Projets</h1>
          <span>{projects.length} projet{projects.length === 1 ? '' : 's'}</span>
        </div>
        {error && <p className="error" role="alert">{error}</p>}
        {projects.length === 0 ? (
          <p className="empty">Aucun project accessible.</p>
        ) : (
          <ul className="project-list">
            {projects.map((project) => (
              <li className="project-row" key={project.id}>
                <div>
                  <h2>{project.name}</h2>
                  <p>{project.description || 'Aucune description'}</p>
                </div>
                <span className="role">
                  {project.isOwner ? 'Propriétaire' : 'Membre'}
                </span>
                </li>
            ))}
          </ul>
        )}
      </main>
    </div>
  )
}

export default App