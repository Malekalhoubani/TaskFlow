# TaskFlow

TaskFlow is a modern, full-stack task and project management platform designed to help teams plan work, assign responsibilities, monitor progress, automate recurring operations, and make better decisions through real-time dashboards and reports.

The platform is being built with a microservices-oriented, clean architecture. Each major business capability will have a clear boundary so it can evolve into an independently deployable service as the product grows, while the current solution remains simple to develop and maintain.

## Product vision

TaskFlow will provide one secure workspace for managing the complete lifecycle of team projects—from planning and task assignment to execution, reporting, notifications, and operational automation.

### Core capabilities

- Secure registration, login, user management, and role-based authorization using ASP.NET Core Identity.
- Workspaces, teams, projects, and membership management.
- Tasks with assignees, priorities, due dates, labels, dependencies, and status workflows.
- Kanban boards, searchable task lists, comments, attachments, and activity history.
- Interactive dashboards for workload, productivity, overdue work, project health, and completion trends.
- Detailed operational and management reports with filtering and export support.
- Scheduled and background processing with Hangfire for reminders, recurring tasks, report generation, notifications, and maintenance jobs.
- Real-time and asynchronous notifications for important project and task events.
- A responsive Angular frontend connected to documented ASP.NET Core APIs.

## Architecture direction

TaskFlow follows Clean Architecture and is designed around microservice-ready business boundaries:

- **Identity** — users, roles, authentication, authorization, and access control.
- **Projects** — workspaces, teams, projects, and memberships.
- **Tasks** — task workflows, assignments, priorities, labels, comments, and activity tracking.
- **Notifications** — email, in-app notifications, reminders, and event-driven communication.
- **Reporting** — dashboard metrics, analytics, scheduled reports, and exports.
- **Background Jobs** — reliable recurring and long-running operations powered by Hangfire.

As the system grows, these boundaries can be extracted into independent services communicating through well-defined APIs and asynchronous events.

## Current solution structure

- `TaskFlow.Api` — ASP.NET Core API host, Swagger documentation, and HTTP endpoints.
- `TaskFlow.Service` — application services, use cases, validation, and business workflows.
- `TaskFlow.Repo` — repositories, Entity Framework Core, Identity persistence, and SQL Server access.
- `TaskFlow.Entities` — domain entities and core business models.
- `TaskFlow.Infrastructure` — external integrations, notifications, storage, and system services.
- `client` — standalone Angular application for dashboards and user workflows.

## Technology stack

- .NET 9 and ASP.NET Core Web API
- ASP.NET Core Identity
- Entity Framework Core 9 and SQL Server
- Swagger / OpenAPI documentation
- Angular 22, TypeScript, SCSS, and RxJS
- Hangfire for scheduled and background jobs
- Clean Architecture, repository, and service patterns
- GitHub with a `Development`-first branching workflow

## Planned quality and operations

- Unit, integration, and API tests.
- Centralized validation, error handling, and structured logging.
- Health checks, monitoring, and audit trails.
- Docker-based development and deployment.
- CI/CD pipelines for build, test, and release automation.
- Secure configuration with no secrets committed to source control.

## Run locally

Backend:

```powershell
dotnet restore TaskFlow.sln
dotnet run --project src/TaskFlow.Api
```

Swagger opens automatically in Development and is available at:

```text
https://localhost:7001/swagger
```

Frontend:

```powershell
cd client
npm install
npm start
```

## Development workflow

The active integration branch is `Development`. New feature branches should be created from `Development`, reviewed through pull requests, and merged back into it. The `master` branch remains the stable release branch.
