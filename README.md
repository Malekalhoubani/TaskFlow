# TaskFlow

TaskFlow is a full-stack task and project management platform for organizing work, assigning responsibilities, tracking progress, and keeping teams aligned.

## What we will build

- Secure user registration and authentication.
- Workspaces and projects for organizing team activity.
- Tasks with assignees, priorities, due dates, labels, and status tracking.
- Kanban-style task boards and searchable task lists.
- Comments and activity history for collaboration and accountability.
- Dashboard summaries for overdue, active, and completed work.
- A responsive Angular frontend connected to an ASP.NET Core Web API.

## Architecture

- `TaskFlow.Api` — ASP.NET Core API host and HTTP endpoints.
- `TaskFlow.Service` — application services and business workflows.
- `TaskFlow.Repo` — repositories, EF Core, and SQL Server persistence.
- `TaskFlow.Entities` — domain entities and core business models.
- `TaskFlow.Infrastructure` — external integrations and system services.
- `client` — standalone Angular frontend.

## Technology

- .NET 9 / ASP.NET Core Web API
- Entity Framework Core 9 with SQL Server
- Angular 22, TypeScript, SCSS, and RxJS
- OpenAPI for API documentation

## Run locally

Backend:

```powershell
dotnet restore TaskFlow.sln
dotnet run --project src/TaskFlow.Api
```

Frontend:

```powershell
cd client
npm install
npm start
```

The active development branch is `Development`; `master` remains the stable default branch.
