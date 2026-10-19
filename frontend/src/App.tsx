import { useState } from "react";
import {
  getProjectIssues,
  getProjects,
  createProject,
  createIssue,
  updateIssueStatus,
  type IssueFilters,
  type CreateIssueInput,
  type Issue,
  type PagedResponse,
  type Project,
  type Session,
} from "./api";
import { AuthScreen } from "./components/AuthScreen";
import { ProjectForm } from "./components/ProjectForm";
import { IssueForm } from "./components/IssueForm";
import { IssueFiltersForm } from "./components/IssueFiltersForm";

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

const emptyIssueFilters: IssueFilters = { search: "", priority: "" };

function App() {
  const [session, setSession] = useState<Session | null>(null);
  const [projects, setProjects] = useState<Project[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [selectedProject, setSelectedProject] = useState<Project | null>(null);
  const [issuePage, setIssuePage] = useState<PagedResponse<Issue> | null>(null);
  const [showProjectForm, setShowProjectForm] = useState(false);
  const [showIssueForm, setShowIssueForm] = useState(false);
  const [issueFilters, setIssueFilters] =
    useState<IssueFilters>(emptyIssueFilters);

  function handleAuthenticated(nextSession: Session, nextProjects: Project[]) {
    setSession(nextSession);
    setProjects(nextProjects);
  }

  function toggleProjectForm() {
    setShowProjectForm((current) => !current);
    setError("");
  }

  async function handleCreateProject(name: string, description: string) {
    if (!session) return;

    const trimmedName = name.trim();
    if (!trimmedName) {
      setError("Le nom du projet est obligatoire.");
      return;
    }

    setBusy(true);
    setError("");
    try {
      const created = await createProject(
        session.accessToken,
        trimmedName,
        description.trim(),
      );
      setProjects((current) => [...current, created]);
      setShowProjectForm(false);
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

  async function loadIssuePage(
    project: Project,
    page: number,
    filters: IssueFilters = issueFilters,
  ) {
    if (!session) return;
    setBusy(true);
    setError("");
    try {
      setIssuePage(
        await getProjectIssues(session.accessToken, project.id, page, filters),
      );
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Erreur inattendue.");
    } finally {
      setBusy(false);
    }
  }

  async function handleApplyIssueFilters(filters: IssueFilters) {
    if (!selectedProject || busy) return;

    const nextFilters = { ...filters, search: filters.search.trim() };
    setIssueFilters(nextFilters);
    setIssuePage(null);
    await loadIssuePage(selectedProject, 1, nextFilters);
  }

  function toggleIssueForm() {
    setShowIssueForm((current) => !current);
    setError("");
  }

  async function handleCreateIssue(input: CreateIssueInput) {
    if (!session || !selectedProject || !selectedProject.isOwner) return;

    const title = input.title.trim();
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
        description: input.description.trim(),
        priority: input.priority,
        dueDate: input.dueDate,
      });

      created = true;
      setShowIssueForm(false);
      setIssuePage(null);
      setIssuePage(
        await getProjectIssues(
          session.accessToken,
          selectedProject.id,
          1,
          issueFilters,
        ),
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

  async function handleUpdateIssueStatus(
    issue: Issue,
    status: Issue["status"],
  ) {
    if (
      !session ||
      !selectedProject ||
      busy ||
      (!selectedProject.isOwner && issue.assigneeId !== session.userId) ||
      status === issue.status
    ) {
      return;
    }

    setBusy(true);
    setError("");
    try {
      await updateIssueStatus(
        session.accessToken,
        selectedProject.id,
        issue.id,
        status,
      );
      setIssuePage((current) =>
        current
          ? {
              ...current,
              items: current.items.map((item) =>
                item.id === issue.id ? { ...item, status } : item,
              ),
            }
          : current,
      );
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Erreur inattendue.");
    } finally {
      setBusy(false);
    }
  }

  async function openProject(project: Project) {
    setShowIssueForm(false);
    setShowProjectForm(false);
    setSelectedProject(project);
    setIssuePage(null);
    setIssueFilters(emptyIssueFilters);
    await loadIssuePage(project, 1, emptyIssueFilters);
  }

  function closeProject() {
    setShowIssueForm(false);
    setSelectedProject(null);
    setIssuePage(null);
    setIssueFilters(emptyIssueFilters);
    setError("");
  }

  function logout() {
    setShowIssueForm(false);
    setSession(null);
    setProjects([]);
    setSelectedProject(null);
    setIssuePage(null);
    setIssueFilters(emptyIssueFilters);
    setShowProjectForm(false);
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
            <IssueFiltersForm
              busy={busy}
              appliedFilters={issueFilters}
              priorityLabels={priorityLabels}
              onApply={handleApplyIssueFilters}
            />
            {error && (
              <p className="error" role="alert">
                {error}
              </p>
            )}
            {selectedProject.isOwner && showIssueForm && (
              <IssueForm
                busy={busy}
                priorityLabels={priorityLabels}
                onCreate={handleCreateIssue}
              />
            )}
            {busy && (
              <p className="empty" role="status">
                Chargement...
              </p>
            )}
            {issuePage &&
              (issuePage.items.length === 0 ? (
                <p className="empty">
                  {issueFilters.search || issueFilters.priority
                    ? "Aucune issue pour ces critères."
                    : "Aucune issue pour ce projet."}
                </p>
              ) : (
                <ul className="project-list">
                  {issuePage.items.map((issue) => (
                    <li className="project-row" key={issue.id}>
                      <div>
                        <h2>{issue.title}</h2>
                        <p>{issue.description || "Aucune description"}</p>
                        {issue.dueDate && <p>Échéance : {issue.dueDate}</p>}
                      </div>
                      <div className="issue-meta">
                        {selectedProject.isOwner ||
                        issue.assigneeId === session.userId ? (
                          <select
                            aria-label={`Statut de ${issue.title}`}
                            value={issue.status}
                            disabled={busy}
                            onChange={(event) =>
                              handleUpdateIssueStatus(
                                issue,
                                event.target.value as Issue["status"],
                              )
                            }
                          >
                            <option value="Backlog">
                              {statusLabels.Backlog}
                            </option>
                            <option value="Todo">{statusLabels.Todo}</option>
                            <option value="InProgress">
                              {statusLabels.InProgress}
                            </option>
                            <option value="Review">
                              {statusLabels.Review}
                            </option>
                            <option value="Done">{statusLabels.Done}</option>
                          </select>
                        ) : (
                          <span>{statusLabels[issue.status]}</span>
                        )}
                        <span>{priorityLabels[issue.priority]}</span>
                      </div>
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
              <ProjectForm busy={busy} onCreate={handleCreateProject} />
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
