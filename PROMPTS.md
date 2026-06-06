# AI Tools and Artifacts

## AI Tools Used

- **OpenAI ChatGPT**
  - Used for planning and architecture design for this application 
  - generated a final design document: `Portfolio Tracker System Design.docx`.
  - Used local terminal commands, file inspection, and patch-based edits to implement and verify the application.

- **OpenAI Codex coding agent**
  - Used as the primary AI coding assistant for reviewing the system design document, producing an implementation plan, scaffolding the application, writing backend/frontend code, debugging runtime issues, and generating documentation.
  - Operated inside the local workspace
  - Used local terminal commands, file inspection, and patch-based edits to implement and verify the application.

- **Codex planning workflow**
  - Used to review `Portfolio Tracker System Design.docx`.
  - Produced the implementation plan that guided the first version.
  - Chose a local-first v1 approach with .NET 10, React, SQLite/local fallbacks, fixed `demo-user`, SignalR, and possible future upgrade paths for PostgreSQL, Redis, RabbitMQ, Docker, and service separation.

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

## Prompt Logs

### 1. System Design Review Prompt

```text
Review the Portfolio Tracker System Design document and extract the functional requirements, non-functional requirements, domain model, API contracts, realtime events, storage choices, and first-version implementation path.

Create a practical implementation plan for version 1 of the application. Prefer an approach that delivers a runnable local application while leaving room for future PostgreSQL, Redis, RabbitMQ, and service-separation upgrades. Call out design gaps, assumptions, and risks.
```

Expected AI output:

- Summarize the document into buildable milestones.
- Identify the need for a `Transaction` model to support realized P&L.
- Recommend a fixed `demo-user` for v1 because authentication is not specified.
- Recommend local-first infrastructure because PostgreSQL, Redis, and RabbitMQ are future upgrade paths, not v1 prerequisites.
- Recommend .NET 10 because the local SDK is .NET 10.

### 2. Implementation Planning Prompt

```text
Create an implementation plan for the first version of the Portfolio Tracker application.

The plan should include backend, frontend, realtime updates, market simulation, alerts, persistence, tests, documentation, and prompt tracking. Include milestones in execution order and make sure every milestone has clear acceptance criteria.
```

Expected AI output:

- Produce the v1 implementation plan used as the implementation blueprint.
- Define the required APIs:
  - `GET /api/portfolio/demo-user`
  - `POST /api/portfolio/buy`
  - `POST /api/portfolio/sell`
  - `GET /api/market/prices`
  - `GET /api/alerts/demo-user`
- Define SignalR events:
  - `PriceUpdated`
  - `PortfolioUpdated`
  - `AlertGenerated`
- Define the prompt log requirement for `PROMPTS.md`.

### 3. Project Scaffolding Prompt

```text
Scaffold the application using a .NET 10 Web API backend and a React TypeScript frontend.

Create a clean repository structure:
- `src/backend/PortfolioTracker.Api`
- `src/frontend`
- `tests/PortfolioTracker.Tests`
- `docs`

Add initial documentation for architecture and API contracts. Do not commit generated build outputs, dependency folders, local caches, logs, or databases.
```

### 4. Backend Domain and Persistence Prompt

```text
Implement the backend domain model for version 1 of the Portfolio Tracker application.

Use EF Core SQLite for local persistence. Add entities for Asset, Position, MarketPrice, Alert, and Transaction. Use `demo-user` as the only supported user. Seed AAPL, MSFT, BTC, and ETH with prices and demo positions.

Make the code modular enough that persistence, cache, messaging, and event delivery can later be upgraded to PostgreSQL, Redis, RabbitMQ, and separate services if needed.
```

### 5. Portfolio API Prompt

```text
Implement the portfolio REST APIs.

Buy behavior:
- Validate user, symbol, quantity, and price.
- Normalize symbols to uppercase.
- Create a position when none exists.
- Increase quantity and recalculate weighted average buy price when a position exists.
- Record a buy transaction.

Sell behavior:
- Reject oversells.
- Reduce quantity.
- Remove the position when quantity reaches zero.
- Calculate realized P&L using average cost.
- Record a sell transaction.

The portfolio response must include holdings, market value, cost value, unrealized P&L, realized P&L, P&L percent, exposure, latest alerts, and updated timestamp.
```

### 6. Market Simulator Prompt

```text
Implement a market simulator as a hosted backend service.

Every few seconds, update seeded market prices with bounded random movement. Use higher volatility for crypto than equities. Preserve opening price for intraday move calculations. Publish in-process `PriceUpdated` events for the current version.
```

### 7. Alert Engine Prompt

```text
Implement alert generation for intraday price moves.

Generate alerts when price moves:
- +5% Warning
- +10% Critical
- -5% Warning
- -10% Critical

Deduplicate alerts by trading day, symbol, direction, and threshold. Persist generated alerts for `demo-user`. Use in-memory deduplication for the first version, with Redis as a possible future upgrade.
```

### 8. SignalR Realtime Prompt

```text
Add realtime updates using SignalR.

Create a `PortfolioHub` at `/hubs/portfolio`. Broadcast price updates, portfolio updates, and generated alerts to connected clients. Configure CORS for local frontend origins. Keep payloads compatible with the frontend and serialize enums as strings.
```

### 9. Frontend Dashboard Prompt

```text
Build the actual portfolio dashboard, not a landing page.

Use React, TypeScript, Redux Toolkit, Material UI, Recharts, SignalR, and lucide icons. The first screen should show the working dashboard with:
- summary metrics
- holdings table
- market ticker
- exposure chart
- P&L chart
- alerts panel
- notification/connection state
- buy/sell trade ticket

The UI should be compact, operational, responsive, and suitable for repeated portfolio monitoring.
```

### 10. Frontend Runtime Debugging Prompt: CORS

```text
The browser reports a CORS failure for:
`http://localhost:5273/hubs/portfolio/negotiate?negotiateVersion=1`
from origin:
`http://127.0.0.1:5173`

Fix the backend CORS policy so SignalR negotiation works for both localhost and 127.0.0.1 development origins.
```

### 11. Frontend Runtime Debugging Prompt: Alert Severity

```text
The frontend crashes with:
`alert.severity.toLowerCase is not a function`

Find the payload mismatch and fix it so alert severity is safe for both REST and SignalR events.
```

### 12. Frontend Runtime Debugging Prompt: Market Price Payload

```text
The frontend crashes with:
`Cannot read properties of undefined (reading 'toFixed')`

Find the market price payload mismatch and fix it so realtime `PriceUpdated` events cannot crash the trade ticket or ticker.
```

### 13. Test Coverage Prompt

```text
Add focused tests for portfolio correctness.

Cover:
- buy creates or increases a position
- sell reduces quantity
- realized P&L calculation
- oversell rejection
- unrealized P&L and exposure calculation
- alert threshold deduplication

Use local SQLite/in-memory test setup so tests do not need PostgreSQL, Redis, RabbitMQ, or Docker.
```

### 14. Edge Case Report Prompt

```text
Review the implemented application for correctness and edge cases.

Generate a report documenting expected scenarios, expected behavior, current behavior, known gaps, recommended fixes, and test coverage recommendations.
```

### 15. AI Usage Audit Prompt

```text
Update the prompt log to document what AI tools were used, how they were used, and what AI artifacts were produced.

Include planning, code generation, debugging, documentation, verification, and prompt tracking. Mention any tools that were not used, such as image generation.
```


### 16. Git Publishing Prompt

```text
publish this code to git repo only pushing the required files further updating the git ignore file
```

### Implementation Notes


- **Prompt log**
  - This file records implementation prompts and AI-assisted changes.
  - User-reported errors were added as prompt entries before fixes were made.

## Prompt Coverage

The following prompts are tracked in this document:

- Initial implementation request for the first version of the application.
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



### Implementation Notes

- Update `.gitignore`, stage only required source/docs/config files, commit, and push to the configured Git remote if available.

## 2026-06-06 00:00 IST - User

### Prompt

```text
generate a readme file containing instructions to run the app locally and required prequisties. also add a brief about the application and supporting documents
```

### Implementation Notes

- Add a root README with application overview, prerequisites, local run instructions, verification commands, and supporting document links.
