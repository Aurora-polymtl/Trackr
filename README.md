# Trackr

Trackr is a full-stack issue tracker and a hands-on .NET and React learning project. It combines an ASP.NET Core REST API backed by PostgreSQL with a React frontend.

## Current Features

### React Frontend

The frontend currently supports:

- Account registration and sign-in
- Project creation and a list of accessible projects
- Viewing a project's issues with status, priority, and pagination
- Refreshing the current list and signing out
- Creating unassigned issues in owned projects
- Changing issue status as a project owner or assigned member

Registration creates an account but does not sign the user in automatically. The frontend keeps the JWT in React state, so reloading the page currently requires signing in again.

Issue editing, assignment, project membership management, and advanced issue filters are available through the API but are not yet exposed in the React interface.

### REST API

The API supports:

- Registration, login, current-user lookup, and password changes with ASP.NET Core Identity
- JWT authentication and ownership-based authorization
- Project CRUD, project members, and issue-status summaries
- Issue CRUD, status changes, priority, optional due dates, and assignment
- Issue filtering, search, sorting, and pagination
- An endpoint for issues assigned to the current user
- Issue comments
- DTO-based responses and structured `ProblemDetails` errors

Project owners manage their projects and members. Members can access shared projects and issues. Full issue edits remain owner-controlled; an assigned member can update an issue's status. Comment authors can edit or delete their own comments.

## Tech Stack

- **Backend:** C#, .NET 10, ASP.NET Core Web API, Entity Framework Core, ASP.NET Core Identity
- **Database:** PostgreSQL with Npgsql and EF Core migrations
- **Frontend:** React, TypeScript, Vite
- **Tests:** xUnit, `WebApplicationFactory`, Vitest, React Testing Library

## Project Structure

```text
Trackr/
├── frontend/
│   └── src/
│       ├── App.tsx
│       ├── App.test.tsx
│       └── api.ts
├── src/
│   └── Trackr.Api/
│       ├── Controllers/
│       ├── Data/Configurations/
│       ├── Dtos/
│       ├── Exceptions/
│       ├── Migrations/
│       ├── Models/
│       ├── Services/
│       ├── Program.cs
│       └── Trackr.Api.http
├── tests/
│   └── Trackr.Api.Tests/
│       ├── Integration/
│       └── Services/
├── Trackr.slnx
└── README.md
```

The API follows a controller → service → Entity Framework Core structure. Controllers handle HTTP concerns; services contain application logic. API contracts use DTOs rather than returning EF Core entity graphs.

## Local Setup

### Prerequisites

- .NET 10 SDK
- PostgreSQL
- Node.js and npm
- The `dotnet-ef` CLI for applying migrations

Run the commands below from the repository root unless stated otherwise.

### Configure Local Secrets

Trackr reads the PostgreSQL connection string and JWT settings from .NET configuration. For local development, set them with User Secrets. Replace the placeholders with your own values:

```powershell
dotnet user-secrets set "ConnectionStrings:TrackrDatabase" "Host=localhost;Port=5432;Database=trackr;Username=postgres;Password=YOUR_LOCAL_PASSWORD" --project src/Trackr.Api
dotnet user-secrets set "Jwt:Key" "YOUR_LONG_RANDOM_SECRET_AT_LEAST_32_BYTES" --project src/Trackr.Api
dotnet user-secrets set "Jwt:Issuer" "Trackr.Api" --project src/Trackr.Api
dotnet user-secrets set "Jwt:Audience" "Trackr.Frontend" --project src/Trackr.Api
```

Do not commit database passwords or JWT signing keys.

### Apply Migrations

Make sure PostgreSQL is running, then apply the existing EF Core migrations:

```powershell
dotnet ef database update --project src/Trackr.Api --startup-project src/Trackr.Api
```

### Start the API

In one terminal:

```powershell
dotnet run --project src/Trackr.Api --launch-profile http
```

The `http` launch profile listens on `http://localhost:5257`. In Development, the generated OpenAPI document is available at `http://localhost:5257/openapi/v1.json`.

### Start the Frontend

In another terminal:

```powershell
cd frontend
npm ci
npm run dev
```

Open the local URL printed by Vite, usually `http://localhost:5173`. Vite proxies `/api` requests to the API on port `5257`, so the backend must be running for registration, login, and project data to work.

## API Overview

Authentication:

```http
POST /api/auth/register
POST /api/auth/login
GET  /api/auth/me
POST /api/auth/change-password
```

Projects and membership:

```http
GET    /api/projects
GET    /api/projects/{id}
POST   /api/projects
PUT    /api/projects/{id}
DELETE /api/projects/{id}
GET    /api/projects/{id}/summary
GET    /api/projects/{id}/members
GET    /api/projects/{id}/members/{memberId}
POST   /api/projects/{id}/members
DELETE /api/projects/{id}/members/{memberId}
```

Issues and comments:

```http
GET    /api/projects/{projectId}/issues
GET    /api/projects/{projectId}/issues/{id}
POST   /api/projects/{projectId}/issues
PUT    /api/projects/{projectId}/issues/{id}
PUT    /api/projects/{projectId}/issues/{id}/status
DELETE /api/projects/{projectId}/issues/{id}
GET    /api/issues/assigned-to-me
GET    /api/projects/{projectId}/issues/{issueId}/comments
GET    /api/projects/{projectId}/issues/{issueId}/comments/{commentId}
POST   /api/projects/{projectId}/issues/{issueId}/comments
PUT    /api/projects/{projectId}/issues/{issueId}/comments/{commentId}
DELETE /api/projects/{projectId}/issues/{issueId}/comments/{commentId}
```

Protected endpoints require `Authorization: Bearer <token>`. Private project resources return `404` when the authenticated user cannot access them.

Project issues can be filtered by status, priority, due date, and assignee; searched by text; sorted; and paginated. For example:

```http
GET /api/projects/1/issues?status=InProgress&priority=High&sortBy=DueDate&sortDirection=Asc&page=1&pageSize=10
```

`src/Trackr.Api/Trackr.Api.http` contains additional requests for the VS Code REST Client.

## Tests

Run the backend tests from the repository root:

```powershell
dotnet test tests/Trackr.Api.Tests/Trackr.Api.Tests.csproj
```

Service tests and HTTP integration tests use EF Core InMemory. They validate application behavior but do not reproduce every PostgreSQL constraint or SQL translation rule.

Run the frontend checks from `frontend`:

```powershell
npm test
npm run build
npm run lint
```

Vitest and React Testing Library test the UI with simulated network responses. They do not replace the API integration tests.

## Next Steps

- Add editing to the React interface
- Expose more of the API's filters and membership workflows in React
- Improve session handling and error feedback
- Add PostgreSQL-backed integration tests for relational behavior
- Prepare deployment

## Purpose

Trackr is a public portfolio project developed incrementally to practice .NET backend development, API design, persistence, security, automated testing, and React.