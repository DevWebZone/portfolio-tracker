# Portfolio Tracker Edge Cases and Correctness Report

## Summary

This report documents expected scenarios and expected behavior for the current staged MVP implementation. The app uses a fixed `demo-user`, SQLite persistence, simulated market prices, in-process realtime events, SignalR, and in-memory alert deduplication.

Overall correctness is solid for the core happy path: seeded portfolio load, buy/sell validation, average-cost updates, realized/unrealized P&L, exposure, market ticks, alert generation, and realtime UI updates. The main known limitations are local-MVP choices: no authentication, no transaction isolation around concurrent trades, alert deduplication resets after process restart, and frontend error state is not cleared after successful trades.

## Scenario Matrix

| Area | Scenario | Expected Behavior | Current Behavior | Status |
| --- | --- | --- | --- | --- |
| Startup | First backend start with empty SQLite DB | Create schema and seed AAPL, MSFT, BTC, ETH prices and demo positions. | `EnsureCreated` and `SeedData` perform this. | Correct |
| Startup | Restart backend with existing DB | Do not duplicate seeded assets, prices, or positions. | Seed checks prevent duplication by table/user existence. | Correct |
| User scope | `GET /api/portfolio/demo-user` | Return the demo portfolio snapshot. | Returns snapshot with holdings, totals, P&L, exposure, alerts. | Correct |
| User scope | Any non-demo user | Reject because auth is deferred. | Returns 404 for reads and 400 for trades. | Correct for MVP |
| Buy | Valid buy for existing symbol | Increase existing quantity, recalculate weighted average buy price, add buy transaction, return updated snapshot. | Implemented. | Correct |
| Buy | Valid buy for symbol not currently held but present in assets | Create new position and transaction. | Implemented. | Correct |
| Buy | Unknown symbol | Reject with clear error. | Rejects with `Unknown symbol {symbol}.` | Correct |
| Buy/Sell | Symbol entered in lowercase or with surrounding spaces | Normalize symbol to uppercase for storage and lookup. | Backend trims and uppercases. | Correct |
| Buy/Sell | Quantity is zero or negative | Reject. | Backend rejects. Frontend silently ignores submit. | Mostly correct |
| Buy/Sell | Price is zero or negative | Reject. | Backend rejects. Frontend silently ignores submit. | Mostly correct |
| Buy/Sell | Quantity or price is blank/NaN | Reject with useful feedback. | Frontend ignores if NaN comparison fails inconsistently; backend model binding may reject malformed JSON. | Needs hardening |
| Sell | Sell less than current quantity | Reduce quantity, record realized P&L using average cost, return updated snapshot. | Implemented. | Correct |
| Sell | Sell exact remaining quantity | Remove position, record realized P&L, return snapshot without that holding. | Implemented. | Correct |
| Sell | Sell more than current quantity | Reject and leave position unchanged. | Implemented and tested. | Correct |
| Sell | Sell symbol not held | Reject as oversell. | Returns `Sell quantity exceeds the current position.` | Correct |
| P&L | Cost value | `quantity * averageBuyPrice`. | Implemented. | Correct |
| P&L | Market value | `quantity * currentPrice`. | Implemented. | Correct |
| P&L | Unrealized P&L | `marketValue - costValue`. | Implemented. | Correct |
| P&L | Realized P&L | Sum realized P&L from sell transactions. | Implemented. | Correct |
| P&L | P&L % with zero cost | Return 0, avoid divide-by-zero. | Implemented. | Correct |
| Exposure | Normal portfolio with value > 0 | Each holding exposure equals holding market value divided by portfolio market value. | Implemented, rounded to 2 decimals. | Correct |
| Exposure | Empty portfolio | Return totals as 0 and no divide-by-zero. | Implemented by empty holdings and zero totals. | Correct |
| Exposure | Rounded exposure totals | Exposure may sum to 99.99 or 100.01 due to rounding. | Expected from per-row rounding. | Acceptable |
| Market simulator | Periodic price updates | Update each market price every 2 seconds using bounded random movement. | Implemented. | Correct |
| Market simulator | Price approaches zero | Prevent non-positive prices. | Uses minimum price `0.01`. | Correct |
| Market simulator | Opening price | Preserve opening price for move calculations. | Opening price remains seeded value. | Correct for MVP |
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
| Frontend trade | Trade succeeds after previous error | Clear previous error and show updated snapshot. | Successful trade updates snapshot but does not clear previous error. | Needs hardening |
| Frontend trade | Trade fails | Show backend error. | Error state is set from thunk rejection. | Correct |
| Frontend rendering | No alerts | Show empty alert message. | Implemented. | Correct |
| Frontend rendering | No holdings | Tables/charts should render empty state without crashing. | Metrics default to 0; table empty; charts get empty arrays. | Correct |
| Frontend rendering | Price object missing current price | Do not crash. | Reducer drops malformed realtime prices; `toFixed` guarded. | Correct |
| Data integrity | Concurrent buys/sells for same position | Apply atomically and avoid lost updates. | No explicit transaction/concurrency token. | Known gap |
| Data integrity | Duplicate asset symbols | Prevent duplicate symbols. | Unique index configured. | Correct |
| Data integrity | Duplicate user-position symbol | Prevent duplicate position row per user/symbol. | Unique index configured. | Correct |
| Data integrity | Decimal precision | Preserve reasonable financial precision. | Uses `decimal`; no explicit SQL precision in SQLite. | Acceptable for MVP |

## Correctness Rules

- Only `demo-user` is valid in the MVP.
- Trade symbols are canonicalized to uppercase on the backend.
- A buy increases position quantity and recalculates average buy price using weighted average cost.
- A sell calculates realized P&L as `(sell price - average buy price) * sold quantity`.
- A full sell removes the open position but keeps the sell transaction for realized P&L.
- Unrealized P&L is calculated only from currently open positions.
- Realized P&L is calculated from sell transactions and remains after positions are closed.
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

3. **Frontend trade validation**
   - Current: invalid values are mostly ignored silently.
   - Expected: disable submit and show field-level messages for blank, NaN, zero, negative, or unsupported precision.

4. **Frontend error clearing**
   - Current: an old trade error can remain visible after a later successful trade.
   - Expected: clear `error` when a trade is submitted and after a successful trade.

5. **Concurrent trade correctness**
   - Current: no explicit transaction isolation or concurrency token protects simultaneous updates.
   - Expected: wrap trade updates in a DB transaction and add optimistic concurrency for positions.

6. **Malformed API payload handling**
   - Current: minimal APIs rely mostly on model binding and service validation.
   - Expected: add request DTO validation and consistent `400` response bodies for malformed JSON, missing fields, NaN-like values, and overly large decimals.

7. **Alert volume in local simulator**
   - Current: random volatility can trigger many alerts quickly, and persisted alerts accumulate.
   - Expected: add trading-day cleanup, paging, and realistic simulator controls.

8. **Authentication and authorization**
   - Current: fixed demo user.
   - Expected: add real identity before multi-user or production use.

## Test Coverage Recommendations

Add or keep tests for these expected behaviors:

- Buy creates a new position for a held or unheld seeded asset.
- Buy rejects an unknown symbol.
- Buy rejects zero, negative, blank, or malformed quantity/price.
- Multiple buys produce correct weighted average price.
- Partial sell reduces quantity and records realized P&L.
- Full sell removes position and keeps realized P&L.
- Oversell rejects without mutating the database.
- Portfolio snapshot returns zero totals for no holdings.
- Exposure percentages are safe when total value is zero.
- Alerts fire at +5, +10, -5, and -10 thresholds.
- Alert dedupe prevents duplicate same-day symbol/threshold/direction alerts.
- Alert dedupe remains correct after process restart once persistent dedupe is implemented.
- SignalR `PriceUpdated` events are normalized correctly in the frontend.
- Frontend renders no holdings, no alerts, disconnected SignalR, and backend unavailable states.
- Frontend clears error after successful reload or trade.

## Acceptance Checklist

- Dashboard loads for `demo-user` and shows seeded holdings.
- Market ticker updates without polling.
- Buy and sell operations update holdings, totals, charts, and transaction-derived P&L.
- Oversell and invalid trades do not mutate positions.
- Alerts display with correct severity labels and do not crash the UI.
- CORS allows both `localhost:5173` and `127.0.0.1:5173`.
- REST and SignalR payloads use compatible shapes and string enums.
- Tests pass before release.
