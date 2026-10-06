import { useState, type SubmitEvent } from "react";
import {
  getProjectIssues,
  getProjects,
  signIn,
  type Issue,
  type PagedResponse,
  type Project,
  type Session,
} from "./api";

const statusLabels: Record<Issue["status"], string> = {
  Backlog: "En attente",
  Todo: "À faire",
  InProgress: "En cours",
  Review: "En révision",
  Done: "Terminé",
};

const priorityLabels: Record<Issue["priority"], string> = {
  Low: "Faible",
  Medium: "Moyenne",
  High: "Élevée",
  Critical: "Critique",
};

function App() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [session, setSession] = useState<Session | null>(null);
  const [projects, setProjects] = useState<Project[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [selectedProject, setSelectedProject] = useState<Project | null>(null);
  const [issuePage, setIssuePage] = useState<PagedResponse<Issue> | null>(null);

  async function handleLogin(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      const nextSession = await signIn(email.trim(), password);
      const nextProjects = await getProjects(nextSession.accessToken);
      setSession(nextSession);
      setProjects(nextProjects);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Erreur inattendue.");
    } finally {
      setBusy(false);
    }
  }

  async function refreshProjects() {
    if (!session) return;
    setBusy(true);
    setError("");

    try {
      setProjects(await getProjects(session.accessToken));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Erreur inattendue.");
    } finally {
      setBusy(false);
    }
  }

  async function loadIssuePage(project: Project, page: number) {
    if (!session) return;
    setBusy(true);
    setError("");
    try {
      setIssuePage(
        await getProjectIssues(session.accessToken, project.id, page),
      );
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Erreur inattendue.");
    } finally {
      setBusy(false);
    }
  }

  async function openProject(project: Project) {
    setSelectedProject(project);
    setIssuePage(null);
    await loadIssuePage(project, 1);
  }

  function closeProject() {
    setSelectedProject(null);
    setIssuePage(null);
    setError("");
  }

  function logout() {
    setSession(null);
    setProjects([]);
    setSelectedProject(null);
    setIssuePage(null);
    setError("");
  }

  if (!session) {
    return (
      <main className="login-page">
        <form className="login-form" onSubmit={handleLogin}>
          <strong className="brand">Trackr</strong>
          <h1>Connexion</h1>
          <label htmlFor="email">Adresse courriel</label>
          <input
            id="email"
            type="email"
            autoComplete="username"
            required
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />
          <label htmlFor="password">Mot de passe</label>
          <input
            id="password"
            type="password"
            autoComplete="current-password"
            required
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
          {error && (
            <p className="error" role="alert">
              {error}
            </p>
          )}
          <button type="submit" disabled={busy}>
            {busy ? "Connexion..." : "Se connecter"}
          </button>
        </form>
      </main>
    );
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <strong className="brand">Trackr</strong>
        <div className="top-actions">
          <span>{session.email}</span>
          <button
            className="secondary"
            type="button"
            onClick={() =>
              selectedProject
                ? loadIssuePage(selectedProject, issuePage?.page ?? 1)
                : refreshProjects()
            }
            disabled={busy}
          >
            Actualiser
          </button>
          <button
            className="secondary"
            type="button"
            onClick={logout}
            disabled={busy}
          >
            Déconnexion
          </button>
        </div>
      </header>
      <main className="content">
        {selectedProject ? (
          <>
            <button className="secondary back-link" type="button"
              onClick={closeProject} disabled={busy}>
                Retour aux projets
            </button>
            <div className="content-heading">
              <h1>{selectedProject.name}</h1>
              {issuePage && <span>{issuePage.totalCount} issue{issuePage.totalCount === 1 ? "" : "s"}</span>}
            </div>
            {error && <p className="error" role="alert">{error}</p>}
            {busy && <p className="empty" role="status">Chargement...</p>}
            {issuePage && (issuePage.items.length === 0 ? (
              <p className="empty">Aucune issue pour ce projet.</p>
            ) : (
              <ul className="project-list">
                {issuePage.items.map((issue) => (
                  <li className="project-row" key={issue.id}>
                    <div>
                      <h2>{issue.title}</h2>
                      <p>{issue.description || "Aucune description"}</p>
                    </div>
                    <span className="role">
                      {statusLabels[issue.status]} - {priorityLabels[issue.priority]}
                    </span>
                  </li>
                ))}
              </ul>
            ))}
            {issuePage && issuePage.totalPages > 1 && (
              <nav className="pagination" aria-label="Pagination des issues">
                <button className="secondary" type="button"
                  disabled={busy || issuePage.page <= 1}
                  onClick={() => loadIssuePage(selectedProject, issuePage.page - 1)}>
                  Précédente
                </button>
                <span>Page {issuePage.page} sur {issuePage.totalPages}</span>
                <button className="secondary" type="button"
                  disabled={busy || issuePage.page >= issuePage.totalPages}
                  onClick={() => loadIssuePage(selectedProject, issuePage.page + 1)}>
                  Suivante
                </button>
              </nav>
            )}
          </>
        ) : (
          <>
            <div className="content-heading">
              <h1>Projets</h1>
              <span>{projects.length} projet{projects.length === 1 ? "" : "s"}</span>
            </div>
            {error && <p className="error" role="alert">{error}</p>}
            {projects.length === 0 ? (
              <p className="empty">Aucun projet à afficher.</p>
            ) : (
              <ul className="project-list">
                {projects.map((project) => (
                  <li className="project-row" key={project.id}>
                    <div>
                      <h2>
                        <button className="project-link" type="button"
                          disabled={busy} onClick={() => openProject(project)}>
                          {project.name}
                        </button>
                      </h2>
                      <p>{project.description || "Aucune description"}</p>
                    </div>
                    <span className="role">
                      {project.isOwner ? "Propriétaire" : "Membre"}
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </>
        )}
      </main>
    </div>
  );
}

export default App;
