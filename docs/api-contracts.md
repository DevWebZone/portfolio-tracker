# API Contracts

## Portfolio

- `GET /api/portfolio/demo-user`
- `POST /api/portfolio/buy`
- `POST /api/portfolio/sell`

Trade request body:

```json
{
  "userId": "demo-user",
  "symbol": "AAPL",
  "quantity": 5,
  "price": 105.42
}
```

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
