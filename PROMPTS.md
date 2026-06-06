# Prompt Log

This file is append-only for implementation prompts. Before starting future implementation work, add a new entry with timestamp, role, full prompt text, and optional implementation notes.

## 2026-06-05 20:37 IST - User

### Prompt

```text
PLEASE IMPLEMENT THIS PLAN:
# Portfolio Tracker Implementation Plan

## Summary

Build a staged MVP from the reviewed system design: first a runnable portfolio tracker with simulated market data, demo user holdings, P&L, exposure, alerts, and SignalR updates; then evolve it toward the document’s event-driven microservice architecture.

Key review notes from the design document:
- The app needs a `Transaction`/trade history model to calculate realized P&L correctly; positions alone are not enough.
- Auth is not specified, so v1 will use a fixed demo user.
- Docker is not installed locally, so early milestones will use local fallbacks before PostgreSQL, Redis, and RabbitMQ.
- The local SDK is .NET 10, so implementation will target .NET 10 rather than .NET 9.

## Milestones

1. **Project Setup and Prompt Log**
   - Create `PROMPTS.md` at repo root.
   - Add this current prompt as the first entry with timestamp, role, and full prompt text.
   - Define the rule that every future implementation prompt gets appended before work starts.
   - Scaffold:
     - `src/backend` for .NET 10 Web API.
     - `src/frontend` for React + TypeScript.
     - Shared docs folder for architecture notes and API contracts.

2. **Backend Core Domain**
   - Implement domain models: `Asset`, `Position`, `MarketPrice`, `Alert`, and `Transaction`.
   - Use a fixed `demo-user` identity for all portfolio APIs.
   - Add EF Core persistence with SQLite/local fallback for v1.
   - Seed sample assets such as AAPL, MSFT, BTC, ETH.
   - Add repositories/services for assets, positions, transactions, market prices, alerts, and portfolio calculations.

3. **Portfolio APIs**
   - Implement:
     - `GET /api/portfolio/demo-user`
     - `POST /api/portfolio/buy`
     - `POST /api/portfolio/sell`
     - `GET /api/market/prices`
     - `GET /api/alerts/demo-user`
   - Buy flow updates or creates a position and records a transaction.
   - Sell flow reduces quantity, records realized P&L, and rejects oversells.
   - Portfolio response includes holdings, total market value, cost value, unrealized P&L, realized P&L, P&L %, and exposure.

4. **Market Simulator**
   - Add a hosted background service that updates seeded asset prices on an interval.
   - Maintain opening prices for alert calculations.
   - Generate realistic bounded volatility.
   - Publish internal in-process price update events for the first implementation stage.

5. **SignalR Realtime Gateway**
   - Add `PortfolioHub`.
   - Broadcast:
     - `PortfolioUpdated`
     - `PriceUpdated`
     - `AlertGenerated`
   - Frontend connects once and updates without polling.
   - Keep event payloads close to the design document while including enough data for dashboard refreshes.

6. **Alert Engine**
   - Detect price moves from opening price at `+5%`, `+10%`, `-5%`, and `-10%`.
   - Severity:
     - `Warning` for 5% moves.
     - `Critical` for 10% moves.
   - Add in-memory deduplication for v1 with keys including symbol, threshold, direction, and trading day.
   - Persist generated alerts for the demo user.

7. **Frontend Dashboard**
   - Build the actual app screen, not a landing page.
   - Use React, TypeScript, Redux Toolkit, Material UI, and Recharts.
   - Components:
     - `PortfolioSummaryCard`
     - `HoldingsTable`
     - `ExposureChart`
     - `PnLChart`
     - `MarketTicker`
     - `AlertPanel`
     - `NotificationCenter`
     - Buy/sell position form
   - Use `npm.cmd` for Windows commands because PowerShell blocks `npm.ps1`.

8. **Integration Upgrade Path**
   - Add adapter interfaces for market events, cache, and messaging.
   - Replace in-process pub-sub with RabbitMQ adapters.
   - Replace in-memory alert deduplication with Redis.
   - Replace SQLite/local fallback with PostgreSQL.
   - Add Docker Compose once Docker is available locally.

9. **Microservice Separation**
   - Split responsibilities into services only after the modular backend is stable:
     - Market Service
     - Portfolio Service
     - Alert Service
     - Realtime Gateway
   - Keep shared event contracts versioned.
   - Preserve the same API and SignalR behavior for the frontend.

10. **Production Readiness**
   - Add structured logging, health checks, metrics, and configuration by environment.
   - Add Kubernetes manifests only after Docker Compose is working.
   - Add load/performance tests for price propagation and portfolio recalculation.
   - Document deployment steps for local, staging, and cloud environments.

## Public Interfaces and Types

- Add `Transaction` as a required domain model:
  - `Id`, `UserId`, `Symbol`, `Side`, `Quantity`, `Price`, `RealizedPnL`, `ExecutedAt`.
- Portfolio API response should include:
  - holdings, prices, total value, cost value, unrealized P&L, realized P&L, P&L %, exposure by asset, and latest alerts.
- SignalR events:
  - `PriceUpdated`
  - `PortfolioUpdated`
  - `AlertGenerated`
- Prompt tracking file:
  - `PROMPTS.md`
  - Append-only format with timestamp, source/user role, prompt text, and optional implementation notes.

## Test Plan

- Backend unit tests:
  - Buy creates/increases positions.
  - Sell reduces positions and calculates realized P&L.
  - Oversell is rejected.
  - Unrealized P&L and exposure calculations are correct.
  - Alert thresholds trigger once per symbol/threshold/day.
- Backend integration tests:
  - Portfolio API returns seeded demo data.
  - Market simulator updates prices.
  - SignalR emits expected events.
- Frontend tests:
  - Dashboard renders holdings, charts, alerts, and ticker.
  - Buy/sell form validates input.
  - SignalR updates change visible dashboard values.
- End-to-end smoke test:
  - Start backend and frontend.
  - Confirm demo portfolio loads.
  - Confirm prices update live.
  - Confirm alerts appear after threshold movement.

## Assumptions

- Implementation targets .NET 10 because it is installed locally.
- v1 uses a fixed `demo-user`; authentication is deferred.
- v1 uses SQLite/local fallbacks; PostgreSQL, Redis, RabbitMQ, Docker, and Kubernetes are later milestones.
- The design document remains the source architecture, but implementation is staged to produce a working app early.
- `PROMPTS.md` must be created before code scaffolding and updated for every future prompt.
```

### Implementation Notes

- First implementation prompt for the repository.

## 2026-06-05 21:17 IST - User

### Prompt

```text
Access to fetch at 'http://localhost:5273/hubs/portfolio/negotiate?negotiateVersion=1' from origin 'http://127.0.0.1:5173' has been blocked by CORS policy: Response to preflight request doesn't pass access control check: No 'Access-Control-Allow-Origin' header is present on the requested resource.
```

### Implementation Notes

- Fix local development CORS to allow both `localhost` and `127.0.0.1` frontend origins.

## 2026-06-05 21:20 IST - User

### Prompt

```text
App.tsx:274 Uncaught TypeError: alert.severity.toLowerCase is not a function
    at App.tsx:274:86
    at Array.map (<anonymous>)
    at App (App.tsx:273:44)
```

### Implementation Notes

- Fix enum serialization/normalization so alert severity is handled safely in the frontend.

## 2026-06-05 21:24 IST - User

### Prompt

```text
App.tsx:84 Uncaught TypeError: Cannot read properties of undefined (reading 'toFixed')
```

### Implementation Notes

- Normalize realtime market price payloads so `currentPrice` is always populated before rendering.

## 2026-06-06 00:00 IST - User

### Prompt

```text
Identify the edge cases and correctness of the application and generate a report for all the expected scenario and expected behaviour 
```

### Implementation Notes

- Review application behavior and create an edge-case/correctness report with expected scenarios and expected behavior.

## 2026-06-06 00:00 IST - User

### Prompt

```text
update the prompts document to include what AI tools were used for developing this application, and how they were used, including any AI artifacts, prompts etc
```

### Implementation Notes

- Add an audit section documenting AI tools, AI-assisted workflows, generated artifacts, and prompt-tracking coverage.

# AI Tools and Artifacts

## AI Tools Used

- **OpenAI Codex coding agent**
  - Used as the primary AI coding assistant for reviewing the system design document, producing an implementation plan, scaffolding the application, writing backend/frontend code, debugging runtime issues, and generating documentation.
  - Operated inside the local workspace at `c:\Users\asus\Downloads\GitHub\portfolio-tracker`.
  - Used local terminal commands, file inspection, and patch-based edits to implement and verify the application.

- **Codex planning workflow**
  - Used to review `Portfolio Tracker System Design.docx`.
  - Produced the staged implementation plan that guided the MVP build.
  - Chose a staged MVP approach with .NET 10, React, SQLite/local fallbacks, fixed `demo-user`, SignalR, and later upgrade paths for PostgreSQL, Redis, RabbitMQ, Docker, and microservices.

- **Codex code generation and debugging workflow**
  - Used to generate backend API code, frontend dashboard code, tests, and documentation.
  - Used to diagnose and fix runtime issues reported by the user, including CORS, enum serialization, and realtime price payload normalization.

## AI-Assisted Development Activities

- Reviewed the system design document and extracted requirements.
- Generated the implementation plan and milestones.
- Created project documentation:
  - `docs/architecture.md`
  - `docs/api-contracts.md`
  - `docs/edge-cases-correctness-report.md`
- Created and maintained this prompt log:
  - `PROMPTS.md`
- Implemented the backend:
  - .NET 10 Web API
  - EF Core SQLite persistence
  - seeded demo portfolio
  - buy/sell APIs
  - portfolio calculations
  - market simulator
  - alert engine
  - SignalR hub and realtime broadcasts
- Implemented the frontend:
  - React + TypeScript + Vite
  - Redux Toolkit state
  - Material UI dashboard
  - Recharts visualizations
  - SignalR client integration
  - buy/sell trade ticket
- Implemented and ran backend tests for core portfolio and alert behavior.
- Ran frontend production builds to verify TypeScript and bundling correctness.

## AI Artifacts Generated

- **Implementation plan**
  - Generated from the reviewed Word system design document and captured in the first prompt entry above.

- **Application source code**
  - Backend source under `src/backend/PortfolioTracker.Api`.
  - Frontend source under `src/frontend`.
  - Test source under `tests/PortfolioTracker.Tests`.

- **Documentation**
  - Architecture overview in `docs/architecture.md`.
  - API contract notes in `docs/api-contracts.md`.
  - Edge case and correctness report in `docs/edge-cases-correctness-report.md`.

- **Prompt log**
  - This file records implementation prompts and AI-assisted changes.
  - User-reported errors were added as prompt entries before fixes were made.

## Prompt Coverage

The following prompts are tracked in this document:

- Initial implementation request for the staged MVP.
- CORS failure for SignalR negotiate from `127.0.0.1:5173`.
- Frontend crash caused by numeric alert severity.
- Frontend crash caused by missing `currentPrice` in realtime price events.
- Request for edge case and correctness report.
- Request to document AI tools, usage, artifacts, and prompts.

## Notes

- No AI image-generation tools were used.
- No external web research was used for implementation.
- Framework generators and package managers were used as development tools:
  - `dotnet new`, `dotnet build`, `dotnet test`
  - `npm.cmd create vite`, `npm.cmd install`, `npm.cmd run build`
- Generated source code and documentation were reviewed and verified through local builds/tests where possible.

## 2026-06-06 00:00 IST - User

### Prompt

```text
publish this code to git repo only pushing the required files further updating the git ignore file
```

### Implementation Notes

- Update `.gitignore`, stage only required source/docs/config files, commit, and push to the configured Git remote if available.
