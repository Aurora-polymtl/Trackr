import { useState, type SubmitEvent } from "react";
import {
  getProjectIssues,
  getProjects,
  createProject,
  createIssue,
  type Issue,
  type PagedResponse,
  type Project,
  type Session,
} from "./api";
import { AuthScreen } from "./components/AuthScreen";

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
  const [session, setSession] = useState<Session | null>(null);
  const [projects, setProjects] = useState<Project[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [selectedProject, setSelectedProject] = useState<Project | null>(null);
  const [issuePage, setIssuePage] = useState<PagedResponse<Issue> | null>(null);
  const [showProjectForm, setShowProjectForm] = useState(false);
  const [projectName, setProjectName] = useState("");
  const [projectDescription, setProjectDescription] = useState("");
  const [showIssueForm, setShowIssueForm] = useState(false);
  const [issueTitle, setIssueTitle] = useState("");
  const [issueDescription, setIssueDescription] = useState("");
  const [issuePriority, setIssuePriority] =
    useState<Issue["priority"]>("Medium");
  const [issueDuDate, setIssueDueDate] = useState("");

  function handleAuthenticated(nextSession: Session, nextProjects: Project[]){
    setSession(nextSession);
    setProjects(nextProjects);
  }

  function toggleProjectForm() {
    setShowProjectForm(!showProjectForm);
    setProjectName("");
    setProjectDescription("");
    setError("");
  }

  async function handleCreateProject(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!session) return;

    const name = projectName.trim();
    if (!name) {
      setError("Le nom du projet est obligatoire.");
      return;
    }

    setBusy(true);
    setError("");
    try {
      const created = await createProject(
        session.accessToken,
        name,
        projectDescription.trim(),
      );
      setProjects((current) => [...current, created]);
      setShowProjectForm(false);
      setProjectName("");
      setProjectDescription("");
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

  function resetIssueForm() {
    setShowIssueForm(false);
    setIssueTitle("");
    setIssueDescription("");
    setIssuePriority("Medium");
    setIssueDueDate("");
  }

  function toggleIssueForm() {
    if (showIssueForm) resetIssueForm();
    else setShowIssueForm(true);
    setError("");
  }

  async function handleCreateIssue(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!session || !selectedProject || !selectedProject.isOwner) return;

    const title = issueTitle.trim();
    if (!title) {
      setError("Le titre de l'issue est obligatoire.");
      return;
    }

    setBusy(true);
    setError("");
    let created = false;

    try {
      await createIssue(session.accessToken, selectedProject.id, {
        title,
        description: issueDescription.trim(),
        priority: issuePriority,
        dueDate: issueDuDate || null,
      });

      created = true;
      resetIssueForm();
      setIssuePage(null);
      setIssuePage(
        await getProjectIssues(session.accessToken, selectedProject.id, 1),
      );
    } catch (cause) {
      if (created) {
        setError("Issue créée, mais la liste n'a pas pu être actualisée.");
      } else {
        setError(cause instanceof Error ? cause.message : "Erreur inattendue.");
      }
    } finally {
      setBusy(false);
    }
  }

  async function openProject(project: Project) {
    resetIssueForm();
    setShowProjectForm(false);
    setProjectName("");
    setProjectDescription("");
    setSelectedProject(project);
    setIssuePage(null);
    await loadIssuePage(project, 1);
  }

  function closeProject() {
    resetIssueForm();
    setSelectedProject(null);
    setIssuePage(null);
    setError("");
  }

  function logout() {
    resetIssueForm();
    setSession(null);
    setProjects([]);
    setSelectedProject(null);
    setIssuePage(null);
    setShowProjectForm(false);
    setProjectName("");
    setProjectDescription("");
    setError("");
  }

  if (!session) {
    return <AuthScreen onAuthenticated={handleAuthenticated} />;
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
            <button
              className="secondary back-link"
              type="button"
              onClick={closeProject}
              disabled={busy}
            >
              Retour aux projets
            </button>
            <div className="content-heading">
              <h1>{selectedProject.name}</h1>
              <div className="project-heading-actions">
                {issuePage && (
                  <span>
                    {issuePage.totalCount} issue
                    {issuePage.totalCount === 1 ? "" : "s"}
                  </span>
                )}
                {selectedProject.isOwner && (
                  <button
                    className={showIssueForm ? "secondary" : undefined}
                    type="button"
                    onClick={toggleIssueForm}
                    disabled={busy}
                  >
                    {showIssueForm ? "Annuler" : "Nouvelle issue"}
                  </button>
                )}
              </div>
            </div>
            {error && (
              <p className="error" role="alert">
                {error}
              </p>
            )}
            {selectedProject.isOwner && showIssueForm && (
              <form className="editor-form" onSubmit={handleCreateIssue}>
                <h2>Nouvelle issue</h2>
                <label htmlFor="issue-title">Titre</label>
                <input
                  id="issue-title"
                  required
                  maxLength={150}
                  value={issueTitle}
                  onChange={(event) => setIssueTitle(event.target.value)}
                />
                <label htmlFor="issue-description">Description</label>
                <textarea
                  id="issue-description"
                  rows={3}
                  maxLength={2000}
                  value={issueDescription}
                  onChange={(event) => setIssueDescription(event.target.value)}
                />
                <label htmlFor="issue-priority">Priorité</label>
                <select
                  id="issue-priority"
                  value={issuePriority}
                  onChange={(event) =>
                    setIssuePriority(event.target.value as Issue["priority"])
                  }
                >
                  <option value="Low">{priorityLabels.Low}</option>
                  <option value="Medium">{priorityLabels.Medium}</option>
                  <option value="High">{priorityLabels.High}</option>
                  <option value="Critical">{priorityLabels.Critical}</option>
                </select>
                <label htmlFor="issue-due-date">Échéance</label>
                <input
                  id="issue-due-date"
                  type="date"
                  value={issueDuDate}
                  onChange={(event) => setIssueDueDate(event.target.value)}
                />
                <button type="submit" disabled={busy}>
                  {busy ? "Création..." : "Créer l'issue"}
                </button>
              </form>
            )}
            {busy && (
              <p className="empty" role="status">
                Chargement...
              </p>
            )}
            {issuePage &&
              (issuePage.items.length === 0 ? (
                <p className="empty">Aucune issue pour ce projet.</p>
              ) : (
                <ul className="project-list">
                  {issuePage.items.map((issue) => (
                    <li className="project-row" key={issue.id}>
                      <div>
                        <h2>{issue.title}</h2>
                        <p>{issue.description || "Aucune description"}</p>
                        {issue.dueDate && <p>Échéance : {issue.dueDate}</p>}
                      </div>
                      <span className="role">
                        {statusLabels[issue.status]} -{" "}
                        {priorityLabels[issue.priority]}
                      </span>
                    </li>
                  ))}
                </ul>
              ))}
            {issuePage && issuePage.totalPages > 1 && (
              <nav className="pagination" aria-label="Pagination des issues">
                <button
                  className="secondary"
                  type="button"
                  disabled={busy || issuePage.page <= 1}
                  onClick={() =>
                    loadIssuePage(selectedProject, issuePage.page - 1)
                  }
                >
                  Précédente
                </button>
                <span>
                  Page {issuePage.page} sur {issuePage.totalPages}
                </span>
                <button
                  className="secondary"
                  type="button"
                  disabled={busy || issuePage.page >= issuePage.totalPages}
                  onClick={() =>
                    loadIssuePage(selectedProject, issuePage.page + 1)
                  }
                >
                  Suivante
                </button>
              </nav>
            )}
          </>
        ) : (
          <>
            <div className="content-heading">
              <h1>Projets</h1>
              <div className="project-heading-actions">
                <span>
                  {projects.length} projet{projects.length === 1 ? "" : "s"}
                </span>
                <button
                  className={showProjectForm ? "secondary" : undefined}
                  type="button"
                  onClick={toggleProjectForm}
                  disabled={busy}
                >
                  {showProjectForm ? "Annuler" : "Nouveau projet"}
                </button>
              </div>
            </div>
            {error && (
              <p className="error" role="alert">
                {error}
              </p>
            )}
            {showProjectForm && (
              <form className="editor-form" onSubmit={handleCreateProject}>
                <h2>Nouveau projet</h2>
                <label htmlFor="project-name">Nom du projet</label>
                <input
                  id="project-name"
                  required
                  maxLength={100}
                  value={projectName}
                  onChange={(event) => setProjectName(event.target.value)}
                />
                <label htmlFor="project-description">Description</label>
                <textarea
                  id="project-description"
                  rows={3}
                  maxLength={500}
                  value={projectDescription}
                  onChange={(event) =>
                    setProjectDescription(event.target.value)
                  }
                />
                <button type="submit" disabled={busy}>
                  {busy ? "Création..." : "Créer le projet"}
                </button>
              </form>
            )}
            {projects.length === 0 ? (
              <p className="empty">Aucun projet à afficher.</p>
            ) : (
              <ul className="project-list">
                {projects.map((project) => (
                  <li className="project-row" key={project.id}>
                    <div>
                      <h2>
                        <button
                          className="project-link"
                          type="button"
                          disabled={busy}
                          onClick={() => openProject(project)}
                        >
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
