# Deployment Guide

The application deploys as two services:

- Frontend: static React/Vite build, usually GitHub Pages.
- Backend: ASP.NET Core API container, Railway or Render.

## Backend: Railway

Railway should build from the root `Dockerfile`.

Required settings:

```text
Builder: Dockerfile
Dockerfile Path: Dockerfile
Healthcheck Path: /healthz
```

Required environment variables:

```text
ASPNETCORE_ENVIRONMENT=Production
DemoMode=false
DatabaseProvider=Sqlite
ConnectionStrings__DefaultConnection=Data Source=/data/helpdesk.db
Jwt__Issuer=HelpDesk.Api
Jwt__Audience=HelpDesk.Frontend
Jwt__Secret=<unique random secret, at least 32 characters>
Cors__AllowedOrigins=https://your-github-user.github.io
Uploads__MaxFileSizeMb=10
BootstrapAdmin__Email=admin@example.com
BootstrapAdmin__Password=<strong password at least 12 characters>
BootstrapAdmin__FullName=System Administrator
```

Notes:

- Keep SQLite under `/data` on Railway so it survives restarts when a volume is mounted.
- Do not set `DemoMode=true` in production unless the deployment is intentionally public/demo-only.
- Production startup fails if `Jwt__Secret` is missing, too short, or still uses the placeholder.
- If the frontend origin changes, update `Cors__AllowedOrigins` without a trailing slash.
- `/healthz` returns JSON including API status and database connectivity.

PostgreSQL option:

```text
DatabaseProvider=Postgresql
ConnectionStrings__DefaultConnection=<postgres connection string>
```

## Backend: Render

`render.yaml` is included as an example service definition. If using Render:

1. Create a Web Service from the repository.
2. Use Docker as the environment.
3. Point the health check to `/healthz`.
4. Set the same production variables listed above.
5. Use PostgreSQL for durable production storage unless you have configured persistent disk for SQLite.

## Frontend: GitHub Pages

The workflow at `.github/workflows/deploy-frontend-pages.yml` builds `frontend/dist`.

Repository settings:

1. Enable GitHub Pages.
2. Set Pages source to GitHub Actions.
3. Add repository variable `VITE_API_BASE`.
4. Set it to the deployed backend API URL, including `/api`:

```text
https://your-backend.example.com/api
```

Push to `main` or run the workflow manually.

## Local deployment checks

Backend:

```powershell
dotnet restore backend
dotnet build backend --no-restore
dotnet run --project backend --no-build --urls http://127.0.0.1:5088
Invoke-RestMethod http://127.0.0.1:5088/healthz
```

Frontend:

```powershell
npm install --prefix frontend
npm --prefix frontend run lint
npm --prefix frontend run build
npm --prefix frontend run preview
```

Docker:

```powershell
docker build -t helpdesk-api .
docker run --rm -p 8080:8080 `
  -e ASPNETCORE_ENVIRONMENT=Production `
  -e DemoMode=false `
  -e Jwt__Secret="replace-with-a-long-random-secret-123456" `
  -e Cors__AllowedOrigins="http://localhost:5173" `
  -e ConnectionStrings__DefaultConnection="Data Source=/data/helpdesk.db" `
  -e BootstrapAdmin__Email="admin@example.com" `
  -e BootstrapAdmin__Password="ChangeMe-Strong-12345" `
  helpdesk-api
```

## Release checklist

- Backend restore/build succeeds.
- Frontend lint/build succeeds.
- `/healthz` returns `status: ok`.
- Production JWT secret is unique and at least 32 characters.
- `DemoMode=false` for real production.
- Bootstrap admin values are provided for first production startup.
- CORS origin exactly matches the frontend origin.
- `VITE_API_BASE` points to the deployed backend `/api`.
- Login works for the intended account.
- Tickets can be created, claimed/assigned, commented, updated, and exported.
- Attachments upload/download works with permitted file types.
- Notifications load and SignalR connection does not show console errors.
- AI status gracefully reports configured or not configured.
