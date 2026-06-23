# IT Help Desk Ticket Management System

Full-stack ticket management system built with React, Vite, ASP.NET Core Web API, and SQLite by default.

## Features

- Create, view, edit, update, and delete help desk tickets.
- Assign tickets to categories: Bug, Feature Request, Support, Billing, and General.
- Track priority, status, created date, and updated date.
- Filter tickets by category.
- View a reports workspace with ticket KPIs, breakdown charts, filterable tables, and PDF/Excel exports.
- Use AI assistance for ticket categorization, priority recommendations, summaries, troubleshooting suggestions, and ticket-aware chat.
- Validate required fields before saving.
- Connect the React frontend directly to backend REST APIs.
- Seed sample users and starter ticket data for local testing.

## Tech Stack

- Frontend: React + Vite
- Backend: ASP.NET Core Web API
- Database: SQLite by default, PostgreSQL optional
- Auth foundation: JWT authentication and seeded roles remain available

## Project Structure

```text
IDS/
  backend/
    Controllers/
    Data/
    Dtos/
    Models/
    Services/
  frontend/
    src/
      App.jsx
      api.js
      index.css
```

## Prerequisites

- .NET SDK 9
- Node.js 20+
- Optional: PostgreSQL 16+

## Backend Setup

From the `backend` folder:

```powershell
dotnet restore
dotnet run
```

The API runs on the URL shown in the terminal. For a predictable local URL, run:

```powershell
dotnet run --urls http://localhost:5000
```

SQLite is configured in `backend/appsettings.json`:

```json
"DatabaseProvider": "Sqlite",
"ConnectionStrings": {
  "DefaultConnection": "Data Source=helpdesk.db"
}
```

## Frontend Setup

From the project root:

```powershell
npm install --prefix frontend
$env:VITE_API_BASE="http://localhost:5000/api"
npm --prefix frontend run dev
```

Open:

```text
http://localhost:5173/IT-Help-Desk/
```

If your backend uses the default HTTPS URL, set `VITE_API_BASE` to that value instead, for example:

```powershell
$env:VITE_API_BASE="https://localhost:7243/api"
```

## API Endpoints

- `GET /api/tickets` - list tickets
- `GET /api/tickets?category=Bug` - list tickets by category
- `GET /api/tickets/{id}` - get one ticket
- `POST /api/tickets` - create ticket
- `PUT /api/tickets/{id}` - update ticket
- `DELETE /api/tickets/{id}` - delete ticket
- `GET /api/categories` - list ticket categories
- `POST /api/auth/login` - existing login endpoint
- `GET /api/reports/tickets` - filterable ticket report data
- `GET /api/reports/tickets/export/pdf` - export the filtered ticket report as PDF
- `GET /api/reports/tickets/export/excel` - export the filtered ticket report as Excel `.xlsx`
- `GET /api/reports/filters` - report filter options scoped to the current user
- `GET /api/ai/status` - configured AI provider status
- `POST /api/ai/tickets/{id}/categorize` - suggest a ticket category
- `POST /api/ai/tickets/{id}/recommend-priority` - recommend ticket priority
- `POST /api/ai/tickets/{id}/summarize` - summarize ticket details and comments
- `POST /api/ai/tickets/{id}/troubleshooting` - suggest troubleshooting steps
- `POST /api/ai/chat` - ticket-aware AI assistant chat

Report endpoints follow the same visibility rules as tickets: admins can report across all tickets, agents see assigned tickets, and users see their own tickets.

## AI Configuration

AI features use backend environment variables only. No API keys are exposed to the frontend.

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

If the provider is not configured, the AI panel remains visible but requests return a clear configuration error.

## Ticket Payload

```json
{
  "title": "Cannot access email",
  "description": "User receives an invalid credentials message.",
  "category": "Support",
  "priority": "High",
  "status": "Open"
}
```

Valid priorities:

```text
Low, Medium, High, Urgent
```

Valid statuses:

```text
Open, In Progress, Resolved, Closed
```

## Seed Data

The backend creates the SQLite database automatically and seeds starter tickets.

Seed users:

- Admin: `admin@helpdesk.local` / `Admin@123`
- Agent: `agent@helpdesk.local` / `Agent@123`
- User: `user@helpdesk.local` / `User@123`

## Build

Backend:

```powershell
dotnet build backend
```

Frontend:

```powershell
npm --prefix frontend run build
```

## Deployment

The production app is deployed as two services:

- Frontend: GitHub Pages at `https://jadalhassan.github.io/IT-Help-Desk/`
- Backend: Railway at `https://helpdesk-api-production-5964.up.railway.app`

The frontend is configured for GitHub Pages with base path `/IT-Help-Desk/`. The backend is container-ready with the root `Dockerfile`, `backend/Dockerfile`, and `railway.json`.

Frontend workflow:

```text
.github/workflows/deploy-frontend-pages.yml
```

Deployment files:

```text
DEPLOYMENT.md
DEMO.md
Dockerfile
railway.json
backend/Dockerfile
render.yaml
```

The GitHub Actions repository variable `VITE_API_BASE` points the frontend at the Railway API:

```text
https://helpdesk-api-production-5964.up.railway.app/api
```

Railway should include these backend environment variables:

```text
ASPNETCORE_ENVIRONMENT=Production
DatabaseProvider=Sqlite
ConnectionStrings__DefaultConnection=Data Source=/data/helpdesk.db
Jwt__Issuer=HelpDesk.Api
Jwt__Audience=HelpDesk.Frontend
Jwt__Secret=<long random secret, at least 32 characters>
Cors__AllowedOrigins=https://jadalhassan.github.io
```

Backend health check:

```text
https://helpdesk-api-production-5964.up.railway.app/healthz
```

See `DEPLOYMENT.md` for the full hosting checklist and `DEMO.md` for the final presentation/demo script.
