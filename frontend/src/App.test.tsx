// @vitest-environment jsdom

import { afterEach, expect, it, vi } from "vitest";
import {
  cleanup,
  render,
  screen,
  within,
  fireEvent,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import App from "./App";

const session = {
  userId: "user-1",
  email: "alice@example.com",
  accessToken: "test-token",
};

const project = {
  id: 1,
  name: "Project Alpha",
  description: "Premier projet",
  createdAt: "2026-01-01T00:00:00Z",
  updatedAt: "2026-01-01T00:00:00Z",
  isOwner: true,
};

function jsonResponse(body: unknown) {
  return { ok: true, status: 200, json: async () => body };
}

async function submitLogin() {
  const user = userEvent.setup();
  render(<App />);
  await user.type(
    screen.getByLabelText("Adresse courriel"),
    "alice@example.com",
  );
  await user.type(screen.getByLabelText("Mot de passe"), "Password123!");
  await user.click(screen.getByRole("button", { name: "Se connecter" }));

  return user;
}

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

it("affiche les projets après la connexion avec le jeton JWT", async () => {
  const fetchMock = vi
    .fn()
    .mockResolvedValueOnce(jsonResponse(session))
    .mockResolvedValueOnce(jsonResponse([project]));
  vi.stubGlobal("fetch", fetchMock);

  await submitLogin();

  expect(await screen.findByText("Project Alpha")).not.toBeNull();
  expect(screen.getByText("Propriétaire")).not.toBeNull();
  expect(fetchMock).toHaveBeenNthCalledWith(1, "/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      email: "alice@example.com",
      password: "Password123!",
    }),
  });
  expect(fetchMock).toHaveBeenNthCalledWith(2, "/api/projects", {
    headers: { Authorization: "Bearer test-token" },
  });
});

it("affiche une erreur sans charger les projets si la connexion échoue", async () => {
  const fetchMock = vi.fn().mockResolvedValueOnce({ ok: false, status: 401 });
  vi.stubGlobal("fetch", fetchMock);

  await submitLogin();

  expect(await screen.findByRole("alert")).toHaveProperty(
    "textContent",
    "Adresse courriel ou mot de passe incorrect.",
  );
  expect(screen.getByRole("heading", { name: "Connexion" })).not.toBeNull();
  expect(fetchMock).toHaveBeenCalledTimes(1);
});

it("actualise les projets puis revient à la connexion après la déconnexion", async () => {
  const fetchMock = vi
    .fn()
    .mockResolvedValueOnce(jsonResponse(session))
    .mockResolvedValueOnce(jsonResponse([project]))
    .mockResolvedValueOnce(
      jsonResponse([
        { ...project, id: 2, name: "Project Beta", isOwner: false },
      ]),
    );
  vi.stubGlobal("fetch", fetchMock);

  const user = await submitLogin();
  await screen.findByText("Project Alpha");

  await user.click(screen.getByRole("button", { name: "Actualiser" }));
  expect(await screen.findByText("Project Beta")).not.toBeNull();
  expect(screen.queryByText("Project Alpha")).toBeNull();
  expect(screen.getByText("Membre")).not.toBeNull();

  await user.click(screen.getByRole("button", { name: "Déconnexion" }));
  expect(screen.getByRole("heading", { name: "Connexion" })).not.toBeNull();
  expect(screen.queryByText("Project Beta")).toBeNull();

  expect(screen.getByLabelText("Adresse courriel")).toHaveProperty("value", "");
  expect(screen.getByLabelText("Mot de passe")).toHaveProperty("value", "");
});

it("ouvre un projet et parcourt ses pages d’issues", async () => {
  const firstPage = {
    items: [
      {
        id: 1,
        title: "Première issue",
        description: "",
        status: "Todo",
        priority: "High",
      },
    ],
    page: 1,
    pageSize: 10,
    totalCount: 11,
    totalPages: 2,
  };
  const secondPage = {
    ...firstPage,
    items: [
      {
        id: 2,
        title: "Deuxième issue",
        description: "",
        status: "Done",
        priority: "Low",
      },
    ],
    page: 2,
  };
  const fetchMock = vi
    .fn()
    .mockResolvedValueOnce(jsonResponse(session))
    .mockResolvedValueOnce(jsonResponse([project]))
    .mockResolvedValueOnce(jsonResponse(firstPage))
    .mockResolvedValueOnce(jsonResponse(secondPage));
  vi.stubGlobal("fetch", fetchMock);

  const user = await submitLogin();
  await screen.findByText("Project Alpha");
  await user.click(screen.getByRole("button", { name: "Project Alpha" }));

  expect(await screen.findByText("Première issue")).not.toBeNull();
  expect(fetchMock).toHaveBeenNthCalledWith(
    3,
    "/api/projects/1/issues?page=1",
    {
      headers: { Authorization: "Bearer test-token" },
    },
  );

  await user.click(screen.getByRole("button", { name: "Suivante" }));
  expect(await screen.findByText("Deuxième issue")).not.toBeNull();
  expect(screen.queryByText("Première issue")).toBeNull();
  expect(fetchMock).toHaveBeenNthCalledWith(
    4,
    "/api/projects/1/issues?page=2",
    {
      headers: { Authorization: "Bearer test-token" },
    },
  );

  await user.click(screen.getByRole("button", { name: "Retour aux projets" }));
  expect(screen.getByRole("heading", { name: "Projets" })).not.toBeNull();
});

it("crée un compte puis propose la connexion sans connecter automatiquement", async () => {
  const fetchMock = vi.fn().mockResolvedValueOnce({
    ok: true,
    status: 201,
    json: async () => ({ id: "new-user", email: "alice@example.com" }),
  });
  vi.stubGlobal("fetch", fetchMock);
  const user = userEvent.setup();
  render(<App />);

  await user.click(screen.getByRole("button", { name: "Créer un compte" }));
  await user.type(
    screen.getByLabelText("Adresse courriel"),
    "alice@example.com",
  );
  await user.type(screen.getByLabelText("Mot de passe"), "Trackr123!");
  await user.type(
    screen.getByLabelText("Confirmer le mot de passe"),
    "Trackr123!",
  );
  await user.click(screen.getByRole("button", { name: "Créer mon compte" }));

  expect(await screen.findByRole("status")).toHaveProperty(
    "textContent",
    "Inscription réussie. Veuillez vous connecter.",
  );
  expect(screen.getByRole("heading", { name: "Connexion" })).not.toBeNull();
  expect(screen.getByLabelText("Adresse courriel")).toHaveProperty(
    "value",
    "alice@example.com",
  );
  expect(screen.getByLabelText("Mot de passe")).toHaveProperty("value", "");
  expect(fetchMock).toHaveBeenCalledExactlyOnceWith("/api/auth/register", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      email: "alice@example.com",
      password: "Trackr123!",
    }),
  });
});

it("ne contacte pas l’API si les mots de passe diffèrent", async () => {
  const fetchMock = vi.fn();
  vi.stubGlobal("fetch", fetchMock);
  const user = userEvent.setup();
  render(<App />);

  await user.click(screen.getByRole("button", { name: "Créer un compte" }));
  await user.type(
    screen.getByLabelText("Adresse courriel"),
    "alice@example.com",
  );
  await user.type(screen.getByLabelText("Mot de passe"), "Trackr123!");
  await user.type(
    screen.getByLabelText("Confirmer le mot de passe"),
    "Autre123!",
  );
  await user.click(screen.getByRole("button", { name: "Créer mon compte" }));

  expect(screen.getByRole("alert")).toHaveProperty(
    "textContent",
    "Les mots de passe ne correspondent pas.",
  );
  expect(fetchMock).not.toHaveBeenCalled();
});

it("explique le refus quand l’adresse est déjà utilisée", async () => {
  const fetchMock = vi.fn().mockResolvedValueOnce({
    ok: false,
    status: 400,
    json: async () => ({
      errors: { DuplicateUserName: ["Already taken."] },
    }),
  });
  vi.stubGlobal("fetch", fetchMock);
  const user = userEvent.setup();
  render(<App />);

  await user.click(screen.getByRole("button", { name: "Créer un compte" }));
  await user.type(
    screen.getByLabelText("Adresse courriel"),
    "alice@example.com",
  );
  await user.type(screen.getByLabelText("Mot de passe"), "Trackr123!");
  await user.type(
    screen.getByLabelText("Confirmer le mot de passe"),
    "Trackr123!",
  );
  await user.click(screen.getByRole("button", { name: "Créer mon compte" }));

  expect(await screen.findByRole("alert")).toHaveProperty(
    "textContent",
    "Cette adresse courriel est déjà utilisée.",
  );
  expect(
    screen.getByRole("heading", { name: "Créer un compte" }),
  ).not.toBeNull();
});

it("crée un projet et l’ajoute à la liste avec la réponse de l’API", async () => {
  const createdProject = {
    ...project,
    id: 2,
    name: "Nouveau projet",
    description: "Description",
  };
  const fetchMock = vi
    .fn()
    .mockResolvedValueOnce(jsonResponse(session))
    .mockResolvedValueOnce(jsonResponse([]))
    .mockResolvedValueOnce({
      ok: true,
      status: 201,
      json: async () => createdProject,
    });
  vi.stubGlobal("fetch", fetchMock);

  const user = await submitLogin();
  await screen.findByRole("heading", { name: "Projets" });
  await user.click(screen.getByRole("button", { name: "Nouveau projet" }));
  await user.type(screen.getByLabelText("Nom du projet"), "Nouveau projet");
  await user.type(screen.getByLabelText("Description"), "Description");
  await user.click(screen.getByRole("button", { name: "Créer le projet" }));

  const projectList = await screen.findByRole("list");
  expect(
    within(projectList).getByRole("button", { name: "Nouveau projet" }),
  ).not.toBeNull();
  expect(screen.getByText("Propriétaire")).not.toBeNull();
  expect(fetchMock).toHaveBeenNthCalledWith(3, "/api/projects", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: "Bearer test-token",
    },
    body: JSON.stringify({
      name: "Nouveau projet",
      description: "Description",
    }),
  });
});

it("refuse un nom composé uniquement d’espaces sans appeler l’API", async () => {
  const fetchMock = vi
    .fn()
    .mockResolvedValueOnce(jsonResponse(session))
    .mockResolvedValueOnce(jsonResponse([]));
  vi.stubGlobal("fetch", fetchMock);

  const user = await submitLogin();
  await screen.findByRole("heading", { name: "Projets" });
  await user.click(screen.getByRole("button", { name: "Nouveau projet" }));
  await user.type(screen.getByLabelText("Nom du projet"), "   ");
  await user.click(screen.getByRole("button", { name: "Créer le projet" }));

  expect(screen.getByRole("alert")).toHaveProperty(
    "textContent",
    "Le nom du projet est obligatoire.",
  );
  expect(fetchMock).toHaveBeenCalledTimes(2);
});

it("crée une issue et recharge la première page du projet", async () => {
  const emptyPage = {
    items: [],
    page: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  };
  const createdIssue = {
    id: 7,
    title: "Préparer la démo",
    description: "Vérifier le parcours",
    status: "Backlog",
    priority: "High",
    dueDate: "2026-11-15",
    createdAt: "2026-10-14T12:00:00Z",
    updatedAt: "2026-10-14T12:00:00Z",
    projectId: 1,
    assigneeId: null,
  };
  const fetchMock = vi
    .fn()
    .mockResolvedValueOnce(jsonResponse(session))
    .mockResolvedValueOnce(jsonResponse([project]))
    .mockResolvedValueOnce(jsonResponse(emptyPage))
    .mockResolvedValueOnce({
      ok: true,
      status: 201,
      json: async () => createdIssue,
    })
    .mockResolvedValueOnce(
      jsonResponse({
        ...emptyPage,
        items: [createdIssue],
        totalCount: 1,
        totalPages: 1,
      }),
    );
  vi.stubGlobal("fetch", fetchMock);

  const user = await submitLogin();
  await screen.findByText("Project Alpha");
  await user.click(screen.getByRole("button", { name: "Project Alpha" }));
  await screen.findByText("Aucune issue pour ce projet.");

  await user.click(screen.getByRole("button", { name: "Nouvelle issue" }));
  await user.type(screen.getByLabelText("Titre"), "Préparer la démo");
  await user.type(screen.getByLabelText("Description"), "Vérifier le parcours");
  await user.selectOptions(screen.getByLabelText("Priorité"), "High");
  fireEvent.change(screen.getByLabelText("Échéance"), {
    target: { value: "2026-11-15" },
  });
  await user.click(screen.getByRole("button", { name: "Créer l'issue" }));

  expect(
    await screen.findByRole("heading", {
      name: "Préparer la démo",
    }),
  ).not.toBeNull();
  expect(screen.getByText("Échéance : 2026-11-15")).not.toBeNull();
  expect(fetchMock).toHaveBeenNthCalledWith(4, "/api/projects/1/issues", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: "Bearer test-token",
    },
    body: JSON.stringify({
      title: "Préparer la démo",
      description: "Vérifier le parcours",
      priority: "High",
      dueDate: "2026-11-15",
    }),
  });
  expect(fetchMock).toHaveBeenNthCalledWith(
    5,
    "/api/projects/1/issues?page=1",
    { headers: { Authorization: "Bearer test-token" } },
  );
});

it("ne propose pas la création d'issue à un membre", async () => {
  const fetchMock = vi
    .fn()
    .mockResolvedValueOnce(jsonResponse(session))
    .mockResolvedValueOnce(jsonResponse([{ ...project, isOwner: false }]))
    .mockResolvedValueOnce(
      jsonResponse({
        items: [],
        page: 1,
        pageSize: 10,
        totalCount: 0,
        totalPages: 0,
      }),
    );
  vi.stubGlobal("fetch", fetchMock);

  const user = await submitLogin();
  await screen.findByText("Project Alpha");
  await user.click(screen.getByRole("button", { name: "Project Alpha" }));
  await screen.findByText("Aucune issue pour ce projet.");

  expect(screen.queryByRole("button", { name: "Nouvelle issue" })).toBeNull();
});
