# IT Help Desk Ticket Management System

Full-stack ticket management system built with React, Vite, ASP.NET Core Web API, and SQLite by default.

## Features

- Create, view, edit, update, and delete help desk tickets.
- Assign tickets to categories: Bug, Feature Request, Support, Billing, and General.
- Track priority, status, created date, and updated date.
- Filter tickets by category.
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

The frontend is configured for GitHub Pages with base path `/IT-Help-Desk/`.

Workflow:

```text
.github/workflows/deploy-frontend-pages.yml
```

Set a repository variable named `VITE_API_BASE` in GitHub Actions if the deployed frontend should connect to a hosted backend.
