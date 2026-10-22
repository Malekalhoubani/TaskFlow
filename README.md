# MyProject

ASP.NET Core 9 solution organized into clear layers:

- `MyProject.Api` — HTTP API host
- `MyProject.Service` — business services
- `MyProject.Repo` — repositories and EF Core persistence
- `MyProject.Entities` — domain entities
- `MyProject.Infrastructure` — external integrations

## Build

```powershell
dotnet restore
dotnet build
```
