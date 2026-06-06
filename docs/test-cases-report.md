# Portfolio Tracker Test Cases Report

## Summary

This report documents the automated test cases currently added for the Portfolio Tracker application. The first version uses a static initialized portfolio, so tests focus on read-only portfolio correctness, market-derived calculations, and alerts.

Test command run:

```powershell
dotnet test tests/PortfolioTracker.Tests/PortfolioTracker.Tests.csproj --no-build --verbosity minimal
```

Current result:

```text
Passed: 8
Failed: 0
Skipped: 0
Total: 8
```

## Test Environment

- Test project: `tests/PortfolioTracker.Tests`
- Test framework: xUnit
- Target framework: .NET 10
- Persistence used in tests: SQLite in-memory database
- Services covered:
  - `PortfolioService`
  - `AlertService`
  - `InMemoryAlertDeduplicationStore`

## Test Cases

| ID | Test Case | Area | Scenario | Expected Result | Status |
| --- | --- | --- | --- | --- | --- |
| TC-001 | `Snapshot_returns_seeded_static_holdings` | Static portfolio snapshot | Load the seeded portfolio snapshot for `demo-user`. | Snapshot contains AAPL, MSFT, BTC, and ETH with initialized quantities. | Passed |
| TC-002 | `Snapshot_has_zero_realized_pnl_for_static_portfolio` | Portfolio calculations | Load the seeded portfolio snapshot for `demo-user`. | Realized P&L is 0 because buy/sell functionality is out of scope for this version. | Passed |
| TC-003 | `Snapshot_calculates_unrealized_pnl_and_exposure` | Portfolio calculations | Load the seeded portfolio snapshot for `demo-user`. | Total value is greater than 0, unrealized P&L is greater than 0, and exposure totals approximately 100%. | Passed |
| TC-004 | `Alert_service_triggers_threshold_once_per_day` | Alert generation and deduplication | Evaluate an AAPL price move of 5.79% twice on the same day. | First evaluation creates one Warning alert; second evaluation creates no duplicate alert. | Passed |
| TC-005 | `Snapshot_returns_zero_totals_for_empty_portfolio` | Portfolio calculations | Remove all demo-user positions and load the portfolio snapshot. | Holdings are empty and total value, cost value, unrealized P&L, realized P&L, and P&L percent are all 0. | Passed |
| TC-006 | `Alert_service_generates_expected_threshold_alerts` | Alert generation | Evaluate a +10.25% MSFT move. | Generates +5% Warning and +10% Critical alerts. | Passed |
| TC-007 | `Alert_service_generates_expected_threshold_alerts` | Alert generation | Evaluate a -5.25% MSFT move. | Generates one -5% Warning alert. | Passed |
| TC-008 | `Alert_service_generates_expected_threshold_alerts` | Alert generation | Evaluate a -10.25% MSFT move. | Generates -5% Warning and -10% Critical alerts. | Passed |

## Coverage Notes

The current tests cover the most important service-level correctness paths:

- Seeded static holdings.
- Zero realized P&L for the read-only first version.
- Portfolio snapshot total and exposure calculation.
- Empty portfolio zero-total behavior.
- Alert threshold detection at +5%, +10%, -5%, and -10%.
- Same-day alert deduplication while the in-memory dedupe store is active.

## Not Yet Covered

Recommended future automated tests:

- Empty portfolio snapshot returns zero totals safely.
- Add DB/Redis-backed persistent alert deduplication tests after that feature is implemented.
- API endpoint integration tests.
- SignalR event integration tests.
- Frontend rendering tests for static holdings, charts, alerts, and connection status.

## Current Status

The added automated tests are passing and validate the main backend service correctness paths for version 1 of the application.
