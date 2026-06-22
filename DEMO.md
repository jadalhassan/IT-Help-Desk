# Final Presentation and Demo Script

## Slide 1: IT Help Desk Ticket Management System

Claim: The system centralizes support requests, assignment, reporting, notifications, attachments, and AI assistance in one full-stack workflow.

Demo proof: Show the deployed app landing/login screen.

## Slide 2: Problem and Objective

Claim: Help desk teams need a faster way to capture, triage, track, and report support work.

Proof points:

- Users can create tickets with category, priority, and status.
- Agents can assign, comment, and update ticket progress.
- Admins can view broader operational reporting.

## Slide 3: Architecture

Claim: A React frontend communicates with an ASP.NET Core API backed by SQLite locally and optional PostgreSQL in production.

Proof object:

```text
React + Vite
    |
REST API + SignalR
    |
ASP.NET Core Controllers
    |
Entity Framework Core
    |
SQLite or PostgreSQL
```

## Slide 4: Core Ticket Workflow

Claim: The main workflow supports the full ticket lifecycle from request to resolution.

Demo steps:

1. Log in as `user@helpdesk.local`.
2. Create a new ticket.
3. Log in as `agent@helpdesk.local`.
4. Assign the ticket, update its status, and add a comment.
5. Show the updated ticket activity.

## Slide 5: Dashboard and Reporting

Claim: The reporting workspace turns ticket activity into operational insight.

Demo steps:

1. Open the dashboard.
2. Show KPI cards and charts.
3. Open Reports.
4. Filter ticket data.
5. Export PDF or Excel.

## Slide 6: Attachments and Notifications

Claim: The system keeps ticket evidence and status changes connected to the workflow.

Demo steps:

1. Upload an attachment to a ticket.
2. Show the attachment list and download action.
3. Trigger a ticket update.
4. Open the notification center.

## Slide 7: AI Assistance

Claim: AI support helps agents summarize, categorize, prioritize, and troubleshoot tickets when a provider is configured.

Demo steps:

1. Open the AI assistant panel.
2. Show provider status.
3. Run categorization or summary if API keys are configured.
4. If no provider is configured, show the clear configuration message.

## Slide 8: Deployment

Claim: The frontend is ready for GitHub Pages, and the backend is containerized for cloud hosting.

Proof points:

- `.github/workflows/deploy-frontend-pages.yml` deploys the frontend.
- `backend/Dockerfile` packages the API.
- `render.yaml` documents a backend hosting configuration.
- `VITE_API_BASE` connects the deployed frontend to the hosted API.

## Slide 9: Validation

Claim: The project is build-ready and demo-ready.

Checks completed:

- `dotnet build backend`
- `npm --prefix frontend run build`

Known deployment inputs:

- Hosted backend URL.
- Production JWT secret.
- GitHub repository variable `VITE_API_BASE`.
- Backend CORS origin.

## Slide 10: Closing

Claim: The application delivers a complete help desk workflow with a practical deployment path and a clear demo story.

Final demo path:

1. Login.
2. Create ticket.
3. Assign and update.
4. Review dashboard.
5. Export report.
6. Show notifications, attachments, and AI status.
