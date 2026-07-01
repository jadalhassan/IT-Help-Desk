# IT Help Desk Ticket Management System

A full-stack help desk application for creating, triaging, assigning, tracking, reporting, and resolving IT support tickets. The project uses React + Vite on the frontend and ASP.NET Core Web API on the backend, with JWT authentication, SignalR notifications, SQLite for local/demo use, and optional PostgreSQL for production.

## What it does

- Role-based workspaces for Admin, Agent, and User.
- Ticket lifecycle: create, view, edit, claim, assign, prioritize, categorize, comment, attach files, change status, inspect history, notify users, report, and export.
- Admin tools for broad visibility, assignment, deletion, dashboard analytics, and operational reports.
- Agent queue with assigned and unassigned tickets, claim workflow, internal/public comments, status changes, attachments, and AI assistance.
- User workspace for simple request submission, progress tracking, comments, and attachments.
- Dashboard KPIs, status charts, activity trends, recent activity, reports, PDF export, and Excel export.
- Optional AI assistance for categorization, priority recommendation, summaries, troubleshooting, and ticket-aware chat.

## Demo accounts

Demo accounts are seeded only when `DemoMode=true` or the Development settings are used.

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@helpdesk.local` | `Admin@123` |
| Agent | `agent@helpdesk.local` | `Agent@123` |
| User | `user@helpdesk.local` | `User@123` |

## Tech stack

| Layer | Technology |
| --- | --- |
| Frontend | React, Vite, React Query, Recharts, SignalR client |
| Backend | ASP.NET Core Web API, EF Core, SignalR |
| Auth | JWT bearer auth, role policies, login rate limiting |
| Database | SQLite by default, PostgreSQL optional |
| Deployment | GitHub Pages frontend, Docker/Railway/Render-capable backend |

## Repository structure

```text
IDS/
  backend/                 ASP.NET Core API
    Controllers/           HTTP endpoints
    Data/                  EF Core context, seeding, schema initialization
    Dtos/                  API request/response contracts
    Hubs/                  SignalR notification hub
    Models/                EF Core entities
    Services/              Auth, notifications, reports, AI, uploads, workflow
  frontend/                React/Vite client
    src/features/          Feature-oriented UI modules
  .github/workflows/       GitHub Pages deployment
  Dockerfile               Root backend Docker build for Railway
  railway.json             Railway service config
  render.yaml              Render service example
  DEPLOYMENT.md            Deployment checklist
  DEMO.md                  Presentation/demo script
```

Generated folders such as `node_modules`, `dist`, `bin`, and `obj` are intentionally ignored.

## Local setup

Prerequisites:

- .NET SDK 9
- Node.js 20+
- Optional: Docker
- Optional: PostgreSQL

Backend:

```powershell
dotnet restore backend
dotnet run --project backend --urls http://localhost:5088
```

Frontend:

```powershell
npm install --prefix frontend
$env:VITE_API_BASE="http://localhost:5088/api"
npm --prefix frontend run dev
```

Open `http://localhost:5173/IT-Help-Desk/` or the URL printed by Vite.

## Configuration

Important backend settings:

```text
ASPNETCORE_ENVIRONMENT=Production
DemoMode=false
DisableDemoAccounts=true
DatabaseProvider=Sqlite
ConnectionStrings__DefaultConnection=Data Source=/data/helpdesk.db
Jwt__Issuer=HelpDesk.Api
Jwt__Audience=HelpDesk.Frontend
Jwt__Secret=<unique secret at least 32 characters>
Cors__AllowedOrigins=https://your-frontend-origin.example
Uploads__MaxFileSizeMb=10
BootstrapAdmin__Email=admin@example.com
BootstrapAdmin__Password=<strong initial password, at least 12 chars>
BootstrapAdmin__FullName=System Administrator
```

Production startup warns when a placeholder JWT secret is used and falls back to an ephemeral startup secret so public demos do not crash; configure a stable `Jwt__Secret` for real deployments. Public demo accounts are enabled unless `DisableDemoAccounts=true`. For real production, set `DisableDemoAccounts=true`, `DemoMode=false`, and provide the `BootstrapAdmin__*` values to create the first admin safely.

Frontend:

```text
VITE_API_BASE=http://localhost:5088/api
```

## API overview

| Method | Endpoint | Description |
| --- | --- | --- |
| `POST` | `/api/auth/login` | Sign in and receive a JWT |
| `GET` | `/api/tickets?search=&status=&priority=&category=&page=&pageSize=` | List visible tickets |
| `GET` | `/api/tickets/{id}` | Get ticket details |
| `POST` | `/api/tickets` | Create a ticket |
| `PUT` | `/api/tickets/{id}` | Update ticket title/category/priority/status |
| `DELETE` | `/api/tickets/{id}` | Admin-only ticket deletion |
| `POST` | `/api/tickets/{id}/assign` | Admin assigns an agent |
| `POST` | `/api/tickets/{id}/claim` | Agent claims an unassigned ticket |
| `POST` | `/api/tickets/{id}/status` | Admin/Agent status transition |
| `POST` | `/api/tickets/{id}/comments` | Add public/internal comment |
| `POST` | `/api/attachments/upload` | Upload a validated ticket attachment |
| `GET` | `/api/dashboard/stats` | Dashboard KPI data |
| `GET` | `/api/reports/tickets` | Report data |
| `GET` | `/api/reports/tickets/export/pdf` | PDF export |
| `GET` | `/api/reports/tickets/export/excel` | Excel export |
| `GET` | `/api/ai/status` | AI provider status |
| `GET` | `/healthz` | JSON health and database connectivity check |

Valid priorities: `Low`, `Medium`, `High`, `Urgent`.

Valid statuses: `Open`, `Assigned`, `In Progress`, `Waiting for User`, `Resolved`, `Closed`.

## AI configuration

No AI key is exposed to the frontend. If no provider is configured, AI actions fail gracefully with a configuration message.

OpenAI:

```powershell
$env:AI_PROVIDER="openai"
$env:OPENAI_API_KEY="..."
$env:OPENAI_MODEL="gpt-4.1-mini"
dotnet run --project backend
```

Azure OpenAI and Ollama are also supported through the backend `AI_PROVIDER` settings.

## Verification

```powershell
dotnet restore backend
dotnet build backend --no-restore
dotnet test backend --no-restore
npm install --prefix frontend
npm --prefix frontend run lint
npm --prefix frontend run build
```

This repository currently has no backend test project, so `dotnet test backend --no-restore` is expected to report that no tests are available.

## Deployment

See [DEPLOYMENT.md](DEPLOYMENT.md). In short:

- Backend builds from the root `Dockerfile`.
- Railway uses `railway.json` and `/healthz`.
- GitHub Pages builds `frontend/dist`.
- `VITE_API_BASE` must point to the deployed backend `/api`.
- CORS must include the deployed frontend origin.
- Production must use a strong JWT secret and should not enable demo seeding.

## Design and QA artifacts

The repository includes ER/workflow diagrams, UI wireframes, screenshots, PDF, and spreadsheet QA artifacts. Treat the executable source code and this README as the current implementation source of truth; older diagrams are preserved as design/reference artifacts and may include future-state concepts that are not implemented one-for-one in EF Core.
