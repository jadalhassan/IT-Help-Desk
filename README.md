# IT Help Desk Ticket Management System

A full-stack help desk application for creating, assigning, tracking, reporting, and resolving IT support tickets. The frontend is built with React and Vite, and the backend is an ASP.NET Core Web API with JWT authentication and SQLite by default.

## Live Demo

- Website: `https://jadalhassan.github.io/IT-Help-Desk/`
- API: `https://helpdesk-api-production-5964.up.railway.app`
- Health check: `https://helpdesk-api-production-5964.up.railway.app/healthz`

Demo accounts:

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@helpdesk.local` | `Admin@123` |
| Agent | `agent@helpdesk.local` | `Agent@123` |
| User | `user@helpdesk.local` | `User@123` |

## Features

- Ticket creation, editing, deletion, assignment, status updates, and comments.
- Role-based workflows for admins, agents, and standard users.
- Dashboard KPIs, status charts, activity trends, and recent activity.
- Report workspace with filters, summary cards, charts, PDF export, and Excel export.
- Attachments for ticket-related files.
- Notifications with SignalR support.
- Optional AI assistance for categorization, priority recommendations, summaries, troubleshooting steps, and ticket-aware chat.
- Seeded demo users and starter tickets for local testing.

## Tech Stack

| Layer | Technology |
| --- | --- |
| Frontend | React, Vite, React Query, Recharts |
| Backend | ASP.NET Core Web API, SignalR |
| Auth | JWT bearer authentication, role policies |
| Database | SQLite by default, PostgreSQL optional |
| Hosting | GitHub Pages frontend, Railway backend |

## Project Structure

```text
IDS/
  backend/
    Controllers/
    Data/
    Dtos/
    Hubs/
    Models/
    Services/
    Dockerfile
  frontend/
    public/
    src/
      features/
      App.jsx
      api.js
      index.css
  .github/workflows/deploy-frontend-pages.yml
  Dockerfile
  railway.json
  DEPLOYMENT.md
```

## Prerequisites

- .NET SDK 9
- Node.js 20 or newer
- Optional: Docker
- Optional: PostgreSQL 16 or newer

## Local Setup

Run the backend:

```powershell
dotnet restore backend
dotnet run --project backend --urls http://localhost:5000
```

Run the frontend from the project root:

```powershell
npm install --prefix frontend
$env:VITE_API_BASE="http://localhost:5000/api"
npm --prefix frontend run dev
```

Open:

```text
http://localhost:5173/IT-Help-Desk/
```

## Build Checks

Backend:

```powershell
dotnet build backend
```

Frontend:

```powershell
npm --prefix frontend run lint
npm --prefix frontend run build
```

## API Overview

Main endpoints:

| Method | Endpoint | Description |
| --- | --- | --- |
| `POST` | `/api/auth/login` | Sign in and receive a JWT |
| `GET` | `/api/tickets` | List visible tickets |
| `GET` | `/api/tickets/{id}` | Get ticket details |
| `POST` | `/api/tickets` | Create a ticket |
| `PUT` | `/api/tickets/{id}` | Update a ticket |
| `DELETE` | `/api/tickets/{id}` | Delete a ticket |
| `POST` | `/api/tickets/{id}/assign` | Assign an agent |
| `POST` | `/api/tickets/{id}/status` | Update ticket status |
| `GET` | `/api/reports/tickets` | Get report data |
| `GET` | `/api/reports/tickets/export/pdf` | Export report as PDF |
| `GET` | `/api/reports/tickets/export/excel` | Export report as Excel |
| `GET` | `/api/ai/status` | Check AI provider status |

Valid priorities:

```text
Low, Medium, High, Urgent
```

Valid statuses:

```text
Open, In Progress, Resolved, Closed
```

## AI Configuration

AI features are configured only on the backend. No provider keys are exposed to the frontend.

OpenAI:

```powershell
$env:AI_PROVIDER="openai"
$env:OPENAI_API_KEY="..."
$env:OPENAI_MODEL="gpt-4.1-mini"
dotnet run --project backend
```

Azure OpenAI:

```powershell
$env:AI_PROVIDER="azure"
$env:AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com"
$env:AZURE_OPENAI_API_KEY="..."
$env:AZURE_OPENAI_DEPLOYMENT="your-deployment"
$env:AZURE_OPENAI_API_VERSION="2024-10-21"
dotnet run --project backend
```

Ollama:

```powershell
$env:AI_PROVIDER="ollama"
$env:OLLAMA_BASE_URL="http://localhost:11434"
$env:OLLAMA_MODEL="llama3.1"
dotnet run --project backend
```

If no AI provider is configured, the AI panel remains visible but requests return a configuration message.

## Deployment

The app is deployed as two services:

- Frontend: GitHub Pages
- Backend: Railway Docker service

GitHub Pages builds `frontend/dist` using:

```text
.github/workflows/deploy-frontend-pages.yml
```

The repository variable `VITE_API_BASE` must point to the hosted backend API:

```text
https://helpdesk-api-production-5964.up.railway.app/api
```

Railway uses:

```text
Dockerfile
railway.json
```

Required Railway variables:

```text
ASPNETCORE_ENVIRONMENT=Production
DatabaseProvider=Sqlite
ConnectionStrings__DefaultConnection=Data Source=/data/helpdesk.db
Jwt__Issuer=HelpDesk.Api
Jwt__Audience=HelpDesk.Frontend
Jwt__Secret=<long random secret, at least 32 characters>
Cors__AllowedOrigins=https://jadalhassan.github.io
```

See [DEPLOYMENT.md](DEPLOYMENT.md) for the full deployment checklist.

## Notes

- SQLite is created automatically and seeded on startup.
- For production persistence on Railway, keep the database path under `/data`.
- PostgreSQL can be used by setting `DatabaseProvider=Postgresql` and replacing `ConnectionStrings__DefaultConnection`.
- The deployed frontend must be rebuilt after changing `VITE_API_BASE`.
