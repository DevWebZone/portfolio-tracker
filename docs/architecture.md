# Portfolio Tracker Architecture

The first implementation is a staged MVP that preserves the design document's service boundaries inside one runnable backend. The backend exposes REST APIs, a SignalR hub, local persistence, a market simulator, portfolio calculations, and alert generation. Later milestones can swap the internal event bus for RabbitMQ, alert deduplication for Redis, and local persistence for PostgreSQL.

## Current Runtime

- Frontend: React, TypeScript, Redux Toolkit, Material UI, Recharts.
- Backend: .NET 10 Web API, SignalR, EF Core SQLite.
- Identity: fixed `demo-user`.
- Realtime: SignalR events for prices, portfolio snapshots, and alerts.

## Upgrade Path

1. Replace in-process market events with RabbitMQ adapters.
2. Replace in-memory alert deduplication with Redis.
3. Move persistence from SQLite to PostgreSQL.
4. Split modules into Market, Portfolio, Alert, and Realtime Gateway services.
