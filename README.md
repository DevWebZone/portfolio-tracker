# Portfolio Tracker

A real-time portfolio tracking application with simulated market prices, demo holdings, P&L calculations, exposure charts, intraday alerts, and live dashboard updates through SignalR.

This repository contains the first version of the application: one modular .NET backend and one React frontend. The current implementation is intentionally local-first and runnable without container infrastructure. Future versions can evolve toward PostgreSQL, Redis, RabbitMQ, and service separation as scale and deployment needs grow.

## Tech Stack

- Backend: .NET 10 Web API, C#, SignalR, EF Core SQLite
- Frontend: React, TypeScript, Vite, Redux Toolkit, Material UI, Recharts
- Tests: xUnit, EF Core SQLite in-memory tests
- Current user model: fixed `demo-user`

## Prerequisites

Install these before running locally:

- .NET SDK 10.x
- Node.js 24.x or compatible current Node version
- npm 11.x or compatible npm version
- Git

Docker is not required for the current first version. PostgreSQL, Redis, and RabbitMQ are possible future upgrades, not prerequisites for local development.

On Windows PowerShell, use `npm.cmd` if script execution blocks `npm.ps1`.

## Run Locally

From the repository root:

```powershell
cd c:\Users\asus\Downloads\GitHub\portfolio-tracker
```

### 1. Restore and Run the Backend

```powershell
dotnet restore PortfolioTracker.slnx
dotnet run --project src/backend/PortfolioTracker.Api/PortfolioTracker.Api.csproj --launch-profile http
```

The backend runs at:

```text
http://localhost:5273
```

Useful endpoints:

- `GET http://localhost:5273/api/portfolio/demo-user`
- `GET http://localhost:5273/api/market/prices`
- `GET http://localhost:5273/api/alerts/demo-user`
- SignalR hub: `http://localhost:5273/hubs/portfolio`

The backend creates a local SQLite database automatically:

```text
src/backend/PortfolioTracker.Api/portfolio-tracker.db
```

This file is ignored by Git.

### 2. Install and Run the Frontend

Open a second terminal:

```powershell
cd src/frontend
npm.cmd install
npm.cmd run dev -- --host 127.0.0.1
```

Open the app:

```text
http://127.0.0.1:5173
```

The frontend defaults to the backend URL:

```text
http://localhost:5273
```

To override it, create a local `.env` file in `src/frontend`:

```text
VITE_API_BASE_URL=http://localhost:5273
```

## Verification Commands

Run backend tests:

```powershell
dotnet test PortfolioTracker.slnx
```

Build the frontend:

```powershell
cd src/frontend
npm.cmd run build
```

## Application Behavior

- Loads a seeded demo portfolio for `demo-user`.
- Simulates live prices for AAPL, MSFT, BTC, and ETH.
- Calculates total portfolio value, cost value, unrealized P&L, P&L percentage, and asset exposure.
- Shows realized P&L as 0 in this read-only first version.
- Presents a static initialized portfolio for the first version.
- Generates alerts for price moves at `+5%`, `+10%`, `-5%`, and `-10%`.
- Updates the dashboard through SignalR without polling.

## Correctness of the Application
- Added unit test cases and ensured they all passed
- Performed Manual validation of the applicaton functionality and happy path.
- Generated report for edge cases correctness [Edge cases and correctness report](docs/edge-cases-correctness-report.md) and test cases [Test cases report](docs/test-cases-report.md)
- Asked AI agent to perform smoke test and integration tests to ensure APIs and SignalR connection are working

## Supporting Documents

- [System design document](<Portfolio Tracker System Design.docx>)
- [Architecture overview](docs/architecture.md)
- [API contracts](docs/api-contracts.md)
- [Edge cases and correctness report](docs/edge-cases-correctness-report.md)
- [Test cases report](docs/test-cases-report.md)
- [Prompt and AI artifact log](PROMPTS.md)

## Repository Notes

The root `.gitignore` excludes generated and local-only files such as:

- `node_modules`
- `dist`
- `bin`
- `obj`
- local SQLite databases
- logs
- local .NET/NuGet caches
- Word temporary lock files

Commit source, docs, tests, configs, solution files, and package lockfiles only.
