# Trackr

Trackr is an issue tracking application built as a full-stack software development project.

The project currently provides a REST API built with ASP.NET Core, Entity Framework Core, and PostgreSQL. A React frontend is planned for the next stage of development.

## Features

The API currently supports:

* Project creation, retrieval, update, and deletion
* Issue creation, retrieval, update, and deletion
* Issues associated with projects
* Issue statuses:

  * Backlog
  * Todo
  * In Progress
  * Review
  * Done
* Issue priorities:

  * Low
  * Medium
  * High
  * Critical
* Issue filtering by status and priority
* Text search across issue titles and descriptions
* Configurable sorting
* Pagination with page metadata
* Creation and update timestamps
* Request validation
* RESTful HTTP responses and resource locations

## Tech Stack

### Backend

* C#
* .NET 10
* ASP.NET Core
* Entity Framework Core
* LINQ
```markdown
- xUnit

### Database

* PostgreSQL
* Npgsql

### Frontend

* React — planned

## Architecture

The backend separates API, business logic, persistence models, and data transfer objects.

```text
HTTP Request
     |
     v
Controller
     |
     v
Service
     |
     v
Entity Framework Core
     |
     v
PostgreSQL
```

The API uses DTOs to separate its public HTTP contract from Entity Framework Core entities.

## API

### Projects

```http
GET    /api/projects
GET    /api/projects/{id}
POST   /api/projects
PUT    /api/projects/{id}
DELETE /api/projects/{id}
```

### Issues

Issues are scoped to a project.

```http
GET    /api/projects/{projectId}/issues
GET    /api/projects/{projectId}/issues/{id}
POST   /api/projects/{projectId}/issues
PUT    /api/projects/{projectId}/issues/{id}
DELETE /api/projects/{projectId}/issues/{id}
```

The issue collection endpoint supports filtering, searching, sorting, and pagination.

Example:

```http
GET /api/projects/1/issues?status=InProgress&priority=High&search=authentication&sortBy=UpdatedAt&sortDirection=Desc&page=1&pageSize=10
```

## Getting Started

### Prerequisites

Install:

* .NET 10 SDK
* PostgreSQL

### Clone the repository

```bash
git clone <repository-url>
cd Trackr
```

### Configure the database

Trackr uses .NET User Secrets for the local PostgreSQL connection string.

From the API project directory:

```bash
cd src/Trackr.Api
dotnet user-secrets set "ConnectionStrings:TrackrDatabase" "Host=localhost;Port=5432;Database=trackr;Username=postgres;Password=YOUR_PASSWORD"
```

Do not commit database passwords or other secrets to the repository.

### Apply database migrations

```bash
dotnet ef database update
```

### Run the API

```bash
dotnet watch
```

The local API address is displayed in the terminal when the application starts.

## Testing

The backend includes automated tests built with xUnit and Entity Framework Core's InMemory provider.

Run the test suite from the repository root:

```bash
dotnet test

## Project Structure

```text
Trackr/
├── src/
│   └── Trackr.Api/
│       ├── Controllers/
│       ├── Data/
│       │   └── Configurations/
│       ├── Dtos/
│       ├── Migrations/
│       ├── Models/
│       ├── Services/
│       └── Program.cs
├── Trackr.slnx
└── README.md
```

## Roadmap

Planned improvements include:

* React frontend
* Additional issue management features
* Improved API error handling
* Expand automated test coverage
* Authentication and user management
* Deployment

## Purpose

Trackr is being developed as a learning and portfolio project focused on modern backend and full-stack development practices with the .NET ecosystem.
