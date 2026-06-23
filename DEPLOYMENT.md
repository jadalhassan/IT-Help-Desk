# Deployment Guide

This project deploys as two services:

- Frontend: static React/Vite build on GitHub Pages.
- Backend: ASP.NET Core API hosted on Railway as a Docker service.

## 1. Backend Hosting: Railway

Production backend:

```text
https://helpdesk-api-production-5964.up.railway.app
```

The repository includes a root `Dockerfile` and `railway.json` so Railway builds the ASP.NET Core API instead of treating the repo as a Node app.

Railway config files:

```text
Dockerfile
railway.json
backend/Dockerfile
```

Required backend environment variables:

```text
ASPNETCORE_ENVIRONMENT=Production
DatabaseProvider=Sqlite
ConnectionStrings__DefaultConnection=Data Source=/data/helpdesk.db
Jwt__Issuer=HelpDesk.Api
Jwt__Audience=HelpDesk.Frontend
Jwt__Secret=<long random secret, at least 32 characters>
Cors__AllowedOrigins=https://jadalhassan.github.io
```

The Docker entrypoint binds ASP.NET Core to Railway's injected `PORT`, falling back to `8080` locally.

Health check:

```text
https://helpdesk-api-production-5964.up.railway.app/healthz
```

Railway should use:

```text
Builder: Dockerfile
Dockerfile Path: Dockerfile
Healthcheck Path: /healthz
```

If the frontend is served from a different origin, update `Cors__AllowedOrigins`. Do not include a trailing slash.

For PostgreSQL hosting, change:

```text
DatabaseProvider=Postgresql
ConnectionStrings__DefaultConnection=<postgres connection string>
```

## 2. Frontend Hosting

The workflow at `.github/workflows/deploy-frontend-pages.yml` builds and deploys `frontend/dist` to GitHub Pages.

Repository settings:

1. Enable GitHub Pages with source set to GitHub Actions.
2. Add a repository variable named `VITE_API_BASE`.
3. Set `VITE_API_BASE` to the deployed backend API URL:

```text
https://helpdesk-api-production-5964.up.railway.app/api
```

Push to `main` or run the workflow manually to deploy.

## 3. Local Production Checks

Run these before deployment:

```powershell
dotnet build backend
npm --prefix frontend run build
```

Optional Docker check:

```powershell
docker build -t helpdesk-api .
docker run --rm -p 8080:8080 `
  -e Jwt__Secret="replace-with-a-long-random-secret-123456" `
  -e Cors__AllowedOrigins="http://localhost:5173" `
  -e ConnectionStrings__DefaultConnection="Data Source=/data/helpdesk.db" `
  helpdesk-api
```

## 4. Demo Credentials

Seeded users:

- Admin: `admin@helpdesk.local` / `Admin@123`
- Agent: `agent@helpdesk.local` / `Agent@123`
- User: `user@helpdesk.local` / `User@123`

## 5. Deployment Checklist

- Backend service builds from the root `Dockerfile`.
- Railway deployment status is successful.
- Backend `/healthz` returns `{"status":"ok"}`.
- Backend `Jwt__Secret` is replaced with a real secret.
- Backend `Cors__AllowedOrigins` matches the GitHub Pages origin.
- Frontend repository variable `VITE_API_BASE` points to the hosted backend `/api`.
- GitHub Pages workflow completes successfully.
- Login, dashboard, tickets, reports, attachments, notifications, and AI status are smoke-tested.
