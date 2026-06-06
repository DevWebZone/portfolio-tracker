# API Contracts

## Portfolio

- `GET /api/portfolio/demo-user`

The first version exposes a read-only initialized portfolio. Buy and sell operations are intentionally out of scope for this version.

## Market

- `GET /api/market/prices`

## Alerts

- `GET /api/alerts/demo-user`

## SignalR

Hub: `/hubs/portfolio`

Events:

- `PriceUpdated`
- `PortfolioUpdated`
- `AlertGenerated`
