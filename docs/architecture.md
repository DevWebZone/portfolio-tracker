# Portfolio Tracker Architecture

This repository contains the first version of the Portfolio Tracker application. It keeps the design document's main responsibilities inside one runnable backend while maintaining clear module boundaries. The backend exposes REST APIs, a SignalR hub, local persistence, a market simulator, portfolio calculations, and alert generation.

The current version is designed to run locally without container infrastructure. Future versions can upgrade persistence to PostgreSQL, alert/cache concerns to Redis, event delivery to RabbitMQ, and eventually split modules into separate services if deployment and scale requirements justify it.

## Current Runtime

- Frontend: React, TypeScript, Redux Toolkit, Material UI, Recharts.
- Backend: .NET 10 Web API, SignalR, EF Core SQLite.
- Identity: fixed `demo-user`.
- Realtime: SignalR events for prices, portfolio snapshots, and alerts.

## Possible Future Upgrade Paths

These are not required for the first version, but the code is organized so they remain reasonable next steps:

1. Move persistence from SQLite to PostgreSQL.
2. Replace in-memory alert deduplication with Redis.
3. Replace in-process market events with RabbitMQ adapters.
4. Split modules into Market, Portfolio, Alert, and Realtime Gateway services.
