# IT Help Desk Ticket Management System

A polished full-stack IT help desk application for submitting, triaging, assigning, tracking, resolving, reporting, and exporting support tickets. The system is built with a React + Vite frontend and an ASP.NET Core Web API backend using JWT authentication, role-based access control, SignalR notifications, SQLite by default, and optional PostgreSQL configuration.

Live demo:

- Frontend: `https://jadalhassan.github.io/IT-Help-Desk/`
- Backend API: `https://helpdesk-api-production-5964.up.railway.app`
- Health check: `https://helpdesk-api-production-5964.up.railway.app/healthz`

## Feature overview

- Role-based workspaces for Admin, Agent, and User.
- Ticket lifecycle support: create, view, edit, assign, claim, prioritize, categorize, comment, attach files, update status, audit history, notify users, report, and export.
- Admin controls for full ticket visibility, assignment, deletion, dashboard analytics, and reports.
- Agent queue for assigned and unassigned tickets, with a claim workflow for unassigned work.
- User workspace for simple request submission, progress tracking, comments, and attachments.
- Dashboard KPIs, ticket status charts, activity trends, and recent activity.
- Report workspace with filters, summaries, PDF export, and Excel export.
- SignalR-backed notification updates.
- Optional AI assistance for categorization, priority recommendation, summaries, troubleshooting guidance, ticket-aware chat, and knowledge-base questions.
- Safe attachment handling for common document/image formats.

## Tech stack

| Layer | Technology |
| --- | --- |
| Frontend | React 19, Vite, React Query, Recharts, React Hook Form, SignalR client |
| Backend | ASP.NET Core / .NET 9 Web API, EF Core, SignalR |
| Auth | JWT bearer authentication, role policies, login rate limiting |
| Database | SQLite by default, PostgreSQL optional through EF Core provider |
| Exports | PDF and Excel report generation |
| Deployment | GitHub Pages frontend, Dockerized backend for Railway or Render |

## Architecture summary

```text
React + Vite frontend
  |
  | REST API + SignalR
  v
ASP.NET Core Web API
  |
  | Controllers -> Services -> EF Core DbContext
  v
SQLite locally / Railway volume by default
PostgreSQL optionally by configuration
```

The frontend stores the JWT session client-side for demo simplicity and sends it as a bearer token to protected API endpoints. The backend enforces role-based access for ticket visibility, admin assignment, agent claiming, reporting, uploads, notifications, and AI operations.

## Project structure

```text
IDS/
  backend/
    Controllers/          API endpoints for auth, tickets, reports, dashboard, AI, notifications, attachments
    Data/                 EF Core context, SQLite schema helpers, seed data
    Dtos/                 Request/response contracts
    Hubs/                 SignalR notification hub
    Models/               User, Ticket, Comment, Attachment, Notification, ActivityLog entities
    Services/             JWT, workflow, uploads, notifications, reports, AI
    Dockerfile            Backend-only Dockerfile
  frontend/
    public/               Static frontend assets
    src/
      features/           Feature modules for AI, attachments, dashboard, notifications, reports
      App.jsx             Main app shell and ticket workspace
      api.js              API client
      index.css           Application styling
    vite.config.js        GitHub Pages base path
  .github/workflows/
    deploy-frontend-pages.yml
  Dockerfile              Root Dockerfile used by Railway
  railway.json            Railway deployment config
  render.yaml             Render deployment example
  DEPLOYMENT.md           Deployment guide
  DEMO.md                 Presentation script
  .env.example            Backend environment example
```

Generated/dependency folders such as `.git`, `node_modules`, `bin`, `obj`, `dist`, and `build` should not be committed.

## Demo accounts

The backend currently ensures these public demo accounts on startup unless `DisableDemoAccounts=true` is configured.

| Role | Email | Password | Purpose |
| --- | --- | --- | --- |
| Admin | `admin@helpdesk.local` | `Admin@123` | Full ticket visibility, assignments, dashboard, reports, deletion |
| Agent | `agent@helpdesk.local` | `Agent@123` | Assigned/unassigned ticket queue, claim workflow, status updates, internal notes |
| User | `user@helpdesk.local` | `User@123` | Submit requests, view own tickets, comment, attach files |

## Local setup

Prerequisites:

- .NET SDK 9
- Node.js 20 or newer
- npm
- Optional: Docker
- Optional: PostgreSQL

Clone and install:

```powershell
git clone https://github.com/jadalhassan/IT-Help-Desk.git
cd IT-Help-Desk
dotnet restore backend
npm install --prefix frontend
```

## Backend setup

Run the API locally:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project backend --urls http://localhost:5088
```

Verify health:

```powershell
Invoke-RestMethod http://localhost:5088/healthz
```

Development settings use SQLite and enable the documented demo accounts. The local database is created automatically.

## Frontend setup

Run the React/Vite app:

```powershell
$env:VITE_API_BASE="http://localhost:5088/api"
npm --prefix frontend run dev
```

Open the Vite URL printed in the terminal. Because the app is configured for GitHub Pages, the expected local path is usually:

```text
http://localhost:5173/IT-Help-Desk/
```

## Environment variables

Backend production-style variables:

```text
ASPNETCORE_ENVIRONMENT=Production
DatabaseProvider=Sqlite
ConnectionStrings__DefaultConnection=Data Source=/data/helpdesk.db
Jwt__Issuer=HelpDesk.Api
Jwt__Audience=HelpDesk.Frontend
Jwt__Secret=<unique secret at least 32 characters>
Cors__AllowedOrigins=https://your-frontend-origin.example
Uploads__MaxFileSizeMb=10
DisableDemoAccounts=true
BootstrapAdmin__Email=admin@example.com
BootstrapAdmin__Password=<strong initial password at least 12 characters>
BootstrapAdmin__FullName=System Administrator
```

Frontend:

```text
VITE_API_BASE=https://your-backend.example.com/api
```

Notes:

- `DisableDemoAccounts=true` disables public demo-account seeding for real production.
- If demo accounts are disabled and no users exist, configure `BootstrapAdmin__Email` and `BootstrapAdmin__Password` to create the first admin.
- A stable `Jwt__Secret` should be configured for real deployments. If the placeholder secret is used in production, the app starts with an ephemeral secret so public demos do not crash, but all sessions are invalidated on restart.
- `DatabaseProvider=Postgresql` switches the backend to PostgreSQL when paired with a PostgreSQL connection string.

## API overview

| Method | Endpoint | Description |
| --- | --- | --- |
| `POST` | `/api/auth/login` | Authenticate and return JWT session data |
| `GET` | `/api/tickets` | List visible tickets with search/filter/pagination |
| `GET` | `/api/tickets/{id}` | Get ticket details, comments, history, activity |
| `POST` | `/api/tickets` | Create a ticket |
| `PUT` | `/api/tickets/{id}` | Update ticket metadata |
| `DELETE` | `/api/tickets/{id}` | Admin-only ticket deletion |
| `POST` | `/api/tickets/{id}/assign` | Admin assigns a ticket to an agent |
| `POST` | `/api/tickets/{id}/claim` | Agent claims an unassigned ticket |
| `POST` | `/api/tickets/{id}/status` | Admin/Agent status transition |
| `POST` | `/api/tickets/{id}/comments` | Add public/internal comment |
| `GET` | `/api/categories` | List ticket categories |
| `GET` | `/api/statuses` | List ticket statuses |
| `GET` | `/api/users/agents` | Admin agent list |
| `GET` | `/api/dashboard/stats` | Dashboard KPIs |
| `GET` | `/api/dashboard/charts/tasks-by-status` | Status chart data |
| `GET` | `/api/dashboard/charts/activity-trends` | Activity trend chart data |
| `GET` | `/api/dashboard/recent-activity` | Recent activity list |
| `GET` | `/api/reports/tickets` | Filtered report data |
| `GET` | `/api/reports/filters` | Report filter options |
| `GET` | `/api/reports/tickets/export/pdf` | Export filtered report as PDF |
| `GET` | `/api/reports/tickets/export/excel` | Export filtered report as Excel |
| `GET` | `/api/attachments` | List attachments for an entity |
| `POST` | `/api/attachments/upload` | Upload validated attachment |
| `GET` | `/api/attachments/{id}/download` | Download attachment |
| `GET` | `/api/notifications` | List notifications |
| `PATCH` | `/api/notifications/{id}/read` | Mark notification read |
| `PATCH` | `/api/notifications/read-all` | Mark all notifications read |
| `GET` | `/api/ai/status` | AI provider status |
| `POST` | `/api/ai/tickets/{id}/categorize` | AI category suggestion |
| `POST` | `/api/ai/tickets/{id}/recommend-priority` | AI priority suggestion |
| `POST` | `/api/ai/tickets/{id}/summarize` | AI ticket summary |
| `POST` | `/api/ai/tickets/{id}/troubleshooting` | AI troubleshooting steps |
| `POST` | `/api/ai/chat` | Ticket-aware AI chat |
| `GET` | `/healthz` | API/database health check |

## Ticket workflow

Valid categories:

```text
Bug, Feature Request, Support, Billing, General
```

Valid priorities:

```text
Low, Medium, High, Urgent
```

Valid statuses:

```text
Open, Assigned, In Progress, Waiting for User, Resolved, Closed
```

Supported transitions:

- `Open` -> `Assigned`, `In Progress`, `Closed`
- `Assigned` -> `In Progress`, `Waiting for User`, `Resolved`, `Closed`
- `In Progress` -> `Waiting for User`, `Resolved`, `Closed`
- `Waiting for User` -> `In Progress`, `Resolved`, `Closed`
- `Resolved` -> `In Progress`, `Closed`
- `Closed` -> `Open`

Assignments, claims, comments, status changes, uploads, and ticket changes are recorded through activity/status history where implemented.

## Roles and permissions

| Role | Capabilities |
| --- | --- |
| Admin | View all tickets, create/edit/delete tickets, assign agents, update statuses, comment, upload/manage attachments, view dashboard/reports, export reports |
| Agent | View assigned and unassigned tickets, claim unassigned tickets, update statuses, add public/internal comments, upload attachments, use AI assistance |
| User | Create tickets, view own tickets, add public comments, upload attachments, track status/history for their own requests |

## AI assistant configuration

AI features are backend-only. Provider keys are never exposed to the frontend.

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

If no provider is configured, the AI panel remains available but returns a clear configuration message.

## Reports and exports

The Reports workspace supports filtering by available ticket dimensions and exporting the filtered result set.

- PDF export: `/api/reports/tickets/export/pdf`
- Excel export: `/api/reports/tickets/export/excel`
- Dashboard charts: status distribution, activity trends, KPIs, recent activity

## Attachments and notifications

Attachments:

- Allowed file types: PNG, JPG/JPEG, WEBP, PDF, DOCX, XLSX, TXT.
- Configurable max size: `Uploads__MaxFileSizeMb`, clamped between 1 MB and 25 MB.
- Backend validates both MIME type and file content signatures where practical.

Notifications:

- Notifications are stored in the backend.
- The frontend uses SignalR to refresh notification state in real time.
- Users can mark individual notifications or all notifications as read.

## Testing and verification

Backend:

```powershell
dotnet restore backend
dotnet build backend --no-restore
dotnet test backend --no-restore
```

Frontend:

```powershell
npm install --prefix frontend
npm --prefix frontend run lint
npm --prefix frontend run build
npm --prefix frontend run preview -- --host 127.0.0.1 --port 4181 --strictPort
```

Smoke checks:

```powershell
Invoke-RestMethod http://localhost:5088/healthz
```

```powershell
$body = @{ email="admin@helpdesk.local"; password="Admin@123" } | ConvertTo-Json
Invoke-RestMethod http://localhost:5088/api/auth/login -Method Post -Body $body -ContentType "application/json"
```

At the time of writing, this repository does not include a dedicated backend test project, so `dotnet test backend --no-restore` exits successfully without executing tests.

## Deployment

### Frontend: GitHub Pages

The workflow `.github/workflows/deploy-frontend-pages.yml` builds `frontend/dist` and deploys it to GitHub Pages.

Required repository variable:

```text
VITE_API_BASE=https://your-backend.example.com/api
```

The Vite base path is configured as:

```text
/IT-Help-Desk/
```

### Backend: Railway

Railway uses:

```text
Dockerfile
railway.json
```

Expected Railway settings:

```text
Builder: Dockerfile
Dockerfile Path: Dockerfile
Healthcheck Path: /healthz
```

For durable SQLite, use a volume and set:

```text
ConnectionStrings__DefaultConnection=Data Source=/data/helpdesk.db
```

### Backend: Render

`render.yaml` provides a Render Docker service example with a persistent disk path under `/var/data`.

For a real production deployment, prefer:

```text
DisableDemoAccounts=true
Jwt__Secret=<stable secret>
Cors__AllowedOrigins=<frontend origin>
```

## Security notes

- JWT bearer authentication is used for protected API endpoints.
- Role policies protect Admin-only and Agent/Admin workflows.
- Login endpoint has fixed-window rate limiting.
- Uploads are restricted by type, size, and content signature checks.
- AI provider keys belong on the backend only.
- A stable production `Jwt__Secret` is required for persistent sessions across restarts.
- Demo accounts are useful for public presentation but should be disabled for real production with `DisableDemoAccounts=true`.
- The frontend stores JWTs in `localStorage`, which is acceptable for this demo but not the strongest production browser-session model.

## Known limitations

- No dedicated backend test project is currently included.
- EF Core migrations are not implemented; local/demo setup uses `EnsureCreated` and SQLite schema helpers.
- The normalized PostgreSQL SQL file and diagrams are reference/future-state artifacts, not the exact EF Core runtime schema.
- GitHub Actions currently shows a Node action deprecation warning, but the Pages deployment succeeds.
- Docker build depends on Docker being installed locally; it was not available in the current Windows environment during verification.
- Production file storage is local/container-volume based; object storage would be better for a larger SaaS deployment.

## Future improvements

- Add backend integration tests using `WebApplicationFactory`.
- Add frontend end-to-end tests using Playwright.
- Replace `EnsureCreated` with formal EF Core migrations.
- Add SLA/escalation rules and richer assignment workflows.
- Move production auth to a more secure cookie/session strategy.
- Store attachments in S3/Azure Blob/R2 for production durability.
- Add CI checks for backend restore/build/test and frontend lint/build.
- Add audit-log filtering and admin user-management screens.

## Demo checklist

Before presenting:

1. Open the live frontend or start frontend/backend locally.
2. Sign in as User and create a ticket.
3. Sign in as Agent and claim the unassigned ticket.
4. Add a public comment and an internal note.
5. Upload a permitted attachment.
6. Move the ticket through the status workflow.
7. Sign in as Admin and show assignment/admin controls.
8. Open Dashboard and explain KPIs/charts.
9. Open Reports, filter data, export PDF/Excel.
10. Open Notifications.
11. Open AI assistant and show either configured AI behavior or graceful not-configured messaging.
12. Mention deployment path: GitHub Pages frontend, Docker/Railway backend, optional Render/PostgreSQL.

## Design and QA artifacts

This repository includes diagrams, wireframes, screenshots, a QA PDF, and a QA spreadsheet. The executable source code and this README are the current implementation source of truth. Some diagrams document broader or future-state ideas that are preserved for presentation/reference rather than implemented one-for-one.
