# IDS Starter (React + ASP.NET Core + SQLite/PostgreSQL)

This repo now includes:
- `frontend` (React + Vite)
- `backend` (ASP.NET Core Web API with JWT auth + role-based authorization)

## Architecture
- Frontend: login page + index page to test public/user/agent/admin endpoints.
- Backend: JWT authentication, role policies (`AdminOnly`, `AgentOrAdmin`), seeded users.
- DB: SQLite by default (fast local bootstrap), PostgreSQL optional via config.

## 1) Prerequisites
Install these first:
1. .NET SDK 9
2. (Optional) PostgreSQL 16+
3. Node.js 20+

## 2) Database setup
Default (already working): SQLite
- `backend/appsettings.json`
- `DatabaseProvider: Sqlite`
- `ConnectionStrings:DefaultConnection = Data Source=helpdesk.db`

Optional PostgreSQL:
1. Install PostgreSQL and create `helpdesk_db`.
2. Set in `backend/appsettings.json`:
   - `DatabaseProvider: PostgreSQL`
   - `ConnectionStrings:DefaultConnection = Host=localhost;Port=5432;Database=helpdesk_db;Username=postgres;Password=postgres`

## 3) Backend setup
1. Open terminal in `backend`
2. Restore and run:
```powershell
dotnet restore
dotnet run
```
3. API base URL should be shown (example: `https://localhost:7243`)

Notes:
- JWT config is in `backend/appsettings.json` under `Jwt`.
- Replace `Jwt:Secret` with a long random value before production.

## 4) Frontend setup
1. Open terminal in `frontend`
2. Install + run:
```powershell
npm install
npm run dev
```
3. Open `http://localhost:5173`

If backend URL differs from `https://localhost:7243`, edit:
- `frontend/src/api.js` (`API_BASE`)

## 5) Seed credentials
- Admin: `admin@helpdesk.local` / `Admin@123`
- Agent: `agent@helpdesk.local` / `Agent@123`
- User: `user@helpdesk.local` / `User@123`

## 6) Endpoints
- `POST /api/auth/login`
- `GET /api/tickets/public` (anonymous)
- `GET /api/tickets/user` (authenticated)
- `GET /api/tickets/agent` (Agent or Admin)
- `GET /api/tickets/admin` (Admin only)

## Learning resources
- React + ASP.NET Core JWT Authentication (official guidance):
  - https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-9.0
- ASP.NET Core Authentication Tutorial:
  - https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-9.0
- JWT authentication server tutorial (official patterns):
  - https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-9.0

## Suggested GitHub repos
- RBAC React + ASP.NET Core Example (search):
  - https://github.com/search?q=react+asp.net+core+jwt+rbac&type=repositories
- ASP.NET Core Developer Roadmap:
  - https://github.com/saifaustcse/aspdotnet-developer-roadmap
