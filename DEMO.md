# Final Presentation and Demo Script

Use this script to present the project as a polished help desk product rather than a code walkthrough.

## 1. Opening

Claim: This is a full-stack IT Help Desk Ticket Management System that centralizes requests, triage, assignment, collaboration, notifications, reporting, exports, attachments, and optional AI assistance.

Show: Login screen, polished layout, and role shortcuts.

## 2. Architecture

```text
React + Vite frontend
    |
JWT-secured REST API + SignalR
    |
ASP.NET Core controllers and services
    |
Entity Framework Core
    |
SQLite locally / PostgreSQL optionally in production
```

Mention:

- JWT authentication with Admin, Agent, and User roles.
- SignalR for real-time notification refresh.
- SQLite auto-setup for demos.
- PostgreSQL-ready configuration for production.
- AI provider keys stay server-side.

## 3. User workflow

Login as:

```text
user@helpdesk.local / User@123
```

Demo:

1. Create a ticket with title, description, category, and priority.
2. Show that the user can track status and add a public comment.
3. Upload a safe attachment if available.
4. Explain that users see their own tickets, not the whole organization.

## 4. Agent workflow

Login as:

```text
agent@helpdesk.local / Agent@123
```

Demo:

1. Show the agent queue: assigned tickets plus unassigned work.
2. Open the new unassigned ticket.
3. Click **Claim Ticket**.
4. Move status through `Assigned`, `In Progress`, `Waiting for User`, or `Resolved`.
5. Add an internal note and a public comment.
6. Show status timeline and audit trail.

Key line: Agents do not need admin power to take ownership of unassigned work.

## 5. Admin workflow

Login as:

```text
admin@helpdesk.local / Admin@123
```

Demo:

1. Show broad ticket visibility.
2. Assign or reassign a ticket to an agent.
3. Edit ticket metadata if needed.
4. Delete only if you want to demonstrate administrative control.
5. Show notifications after workflow changes.

## 6. Dashboard and reports

Demo:

1. Open Dashboard.
2. Show KPI cards, status distribution, trend chart, and recent activity.
3. Open Reports.
4. Filter by status, priority, category, date, or agent.
5. Export PDF and Excel.

Key line: Reports are not decorative; they answer operational questions such as backlog, priority pressure, resolution speed, and agent workload.

## 7. Attachments and AI

Demo:

1. Upload a permitted file type: PNG, JPG, WEBP, PDF, DOCX, XLSX, or TXT.
2. Show download and delete permissions.
3. Open the AI assistant panel.
4. If an AI provider is configured, run summarize or troubleshooting.
5. If no provider is configured, show the graceful configuration message.

## 8. Deployment readiness

Mention:

- Backend Docker build is available from the root `Dockerfile`.
- Railway config uses `/healthz`.
- Render config is included as an alternative host reference.
- GitHub Pages workflow builds the frontend.
- `VITE_API_BASE` connects the deployed frontend to the backend API.
- Production rejects placeholder JWT secrets.
- Demo seeding can be disabled with `DemoMode=false`.

## 9. Validation commands

Run before presenting:

```powershell
dotnet restore backend
dotnet build backend --no-restore
npm install --prefix frontend
npm --prefix frontend run lint
npm --prefix frontend run build
```

Smoke-test:

```powershell
dotnet run --project backend --no-build --urls http://127.0.0.1:5088
Invoke-RestMethod http://127.0.0.1:5088/healthz
```

## 10. Closing path

Recommended live demo order:

1. User creates a request.
2. Agent claims it and starts work.
3. Agent comments, attaches evidence, and updates status.
4. Admin reviews dashboard and reports.
5. Export PDF/Excel.
6. Show notifications and AI status.

Keep the story simple: request comes in, work is owned, progress is visible, history is auditable, reporting is exportable.
