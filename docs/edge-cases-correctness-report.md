# Portfolio Tracker Edge Cases and Correctness Report

## Summary

This report documents expected scenarios and expected behavior for the first version of the Portfolio Tracker application. The app uses a fixed `demo-user`, SQLite persistence, simulated market prices, in-process realtime events, SignalR, and in-memory alert deduplication.

Overall correctness is solid for the core happy path: seeded portfolio load, initialized holdings, unrealized P&L, exposure, market ticks, alert generation, and realtime UI updates. The main known limitations are first-version choices: no authentication, no user-editable positions, alert deduplication resets after process restart, and no trading workflow.

## Scenario Matrix

| Area | Scenario | Expected Behavior | Current Behavior | Status |
| --- | --- | --- | --- | --- |
| Startup | First backend start with empty SQLite DB | Create schema and seed AAPL, MSFT, BTC, ETH prices and demo positions. | `EnsureCreated` and `SeedData` perform this. | Correct |
| Startup | Restart backend with existing DB | Do not duplicate seeded assets, prices, or positions. | Seed checks prevent duplication by table/user existence. | Correct |
| User scope | `GET /api/portfolio/demo-user` | Return the demo portfolio snapshot. | Returns snapshot with holdings, totals, P&L, exposure, alerts. | Correct |
| User scope | Any non-demo user | Reject because auth is deferred. | Returns 404 for reads. | Correct for v1 |
| Static portfolio | Seeded initialized holdings | Return the initialized AAPL, MSFT, BTC, and ETH positions. | Implemented. | Correct |
| Static portfolio | User attempts to trade | Trading is unavailable in version 1. | No buy/sell UI or API endpoints are exposed. | Correct |
| P&L | Cost value | `quantity * averageBuyPrice`. | Implemented. | Correct |
| P&L | Market value | `quantity * currentPrice`. | Implemented. | Correct |
| P&L | Unrealized P&L | `marketValue - costValue`. | Implemented. | Correct |
| P&L | Realized P&L | Return 0 because trading is out of scope for version 1. | Implemented. | Correct |
| P&L | P&L % with zero cost | Return 0, avoid divide-by-zero. | Implemented. | Correct |
| Exposure | Normal portfolio with value > 0 | Each holding exposure equals holding market value divided by portfolio market value. | Implemented, rounded to 2 decimals. | Correct |
| Exposure | Empty portfolio | Return totals as 0 and no divide-by-zero. | Implemented by empty holdings and zero totals. | Correct |
| Exposure | Rounded exposure totals | Exposure may sum to 99.99 or 100.01 due to rounding. | Expected from per-row rounding. | Acceptable |
| Market simulator | Periodic price updates | Update each market price every 2 seconds using bounded random movement. | Implemented. | Correct |
| Market simulator | Price approaches zero | Prevent non-positive prices. | Uses minimum price `0.01`. | Correct |
| Market simulator | Opening price | Preserve opening price for move calculations. | Opening price remains seeded value. | Correct for v1 |
| Market simulator | New trading day | Opening price should reset for a new trading day. | Not implemented. | Known gap |
| Alerts | Move reaches +5% or -5% | Generate Warning alert. | Implemented. | Correct |
| Alerts | Move reaches +10% or -10% | Generate Critical alert. | Implemented. | Correct |
| Alerts | Move reaches +10% | Generate both 5% and 10% threshold alerts if not already generated. | Implemented by evaluating both thresholds. | Correct |
| Alerts | Duplicate same symbol/threshold/direction/day | Prevent duplicate alert. | Prevented only while process memory remains alive. | Partially correct |
| Alerts | Backend restart after alert already persisted | Should still avoid duplicate persisted alert for same day. | In-memory dedupe resets, so duplicates can occur. | Known gap |
| Alerts | Alerts API | Return recent alerts newest first. | Returns latest 50. Portfolio snapshot embeds latest 10. | Correct |
| SignalR | Frontend connects from localhost or 127.0.0.1 | CORS should allow negotiate and websocket. | CORS allows both origins. | Correct |
| SignalR | `PriceUpdated` payload shape | UI should handle realtime payloads safely. | Frontend normalizes `price` and `currentPrice`. | Correct |
| SignalR | Enum payloads | UI should handle string enums and old numeric values safely. | Backend serializes strings; UI normalizes severity. | Correct |
| SignalR | Connection lost | UI should show disconnected/reconnecting state and continue REST refresh. | Connection chip updates; refresh still available. | Correct |
| Frontend load | Backend unavailable | Show error instead of crashing. | `loadDashboard` sets error state. | Correct |
| Frontend rendering | No alerts | Show empty alert message. | Implemented. | Correct |
| Frontend rendering | No holdings | Tables/charts should render empty state without crashing. | Metrics default to 0; table empty; charts get empty arrays. | Correct |
| Frontend rendering | Price object missing current price | Do not crash. | Reducer drops malformed realtime prices; `toFixed` guarded. | Correct |
| Data integrity | Duplicate asset symbols | Prevent duplicate symbols. | Unique index configured. | Correct |
| Data integrity | Duplicate user-position symbol | Prevent duplicate position row per user/symbol. | Unique index configured. | Correct |
| Data integrity | Decimal precision | Preserve reasonable financial precision. | Uses `decimal`; no explicit SQL precision in SQLite. | Acceptable for v1 |

## Correctness Rules

- Only `demo-user` is valid in the first version.
- The initialized portfolio is read-only in version 1.
- Buy and sell operations are intentionally out of scope.
- Unrealized P&L is calculated only from currently open positions.
- Realized P&L is 0 in version 1.
- Portfolio exposure is based on current market value, not cost value.
- Market prices never go below `0.01`.
- Alerts are based on move from opening price, not previous tick.
- SignalR should be treated as eventually consistent; REST refresh remains the recovery path.

## Known Gaps and Recommended Fixes

1. **Persistent alert deduplication**
   - Current: dedupe is in memory, so duplicates can be generated after backend restart.
   - Expected long-term: enforce a DB unique key or Redis key on trading day, symbol, direction, and threshold.

2. **Trading day/opening price reset**
   - Current: opening prices stay at seeded values forever.
   - Expected long-term: reset opening prices at the start of each trading day/session.

3. **Alert volume in local simulator**
   - Current: random volatility can trigger many alerts quickly, and persisted alerts accumulate.
   - Expected: add trading-day cleanup, paging, and realistic simulator controls.

4. **Authentication and authorization**
   - Current: fixed demo user.
   - Expected: add real identity before multi-user or production use.

5. **Editable portfolio workflows**
   - Current: buy/sell and position editing are intentionally excluded from version 1.
   - Expected long-term: add explicit requirements, validation, transaction history, and tests before introducing user-editable positions.

## Test Coverage Recommendations

Add or keep tests for these expected behaviors:

- Static portfolio returns the expected initialized holdings and quantities.
- Static portfolio returns realized P&L as 0.
- Portfolio snapshot returns zero totals for no holdings.
- Exposure percentages are safe when total value is zero.
- Alerts fire at +5, +10, -5, and -10 thresholds.
- Alert dedupe prevents duplicate same-day symbol/threshold/direction alerts.
- Alert dedupe remains correct after process restart once persistent dedupe is implemented.
- SignalR `PriceUpdated` events are normalized correctly in the frontend.
- Frontend renders no holdings, no alerts, disconnected SignalR, and backend unavailable states.
- Frontend renders the static portfolio without any trading controls.

## Acceptance Checklist

- Dashboard loads for `demo-user` and shows seeded holdings.
- Market ticker updates without polling.
- Static initialized holdings load and update market values from simulated prices.
- Alerts display with correct severity labels and do not crash the UI.
- CORS allows both `localhost:5173` and `127.0.0.1:5173`.
- REST and SignalR payloads use compatible shapes and string enums.
- Tests pass before release.
