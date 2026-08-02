# Monthly Stats — Backend Change Request

**Status:** required — the superadmin payments page now shows **monthly** KPI stats.

**Endpoint affected:** `GET /api/v1/admin/payments/stats` (currently deployed, see
`docs/superadmin-payments-frontend-guide.md` §4.3).

---

## What the frontend needs

The stats endpoint must accept two **optional** query parameters (same format as
the payments list endpoint):

| Param | Type | Notes |
|-------|------|-------|
| `FromDate` | date | `YYYY-MM-DD` — inclusive, filters by payment paid date |
| `ToDate` | date | `YYYY-MM-DD` — inclusive |

**Example call (August 2026):**

```
GET /api/v1/admin/payments/stats?FromDate=2026-08-01&ToDate=2026-08-31
```

## Required behavior

| Dates sent? | `todayRevenue` | `subscriptionsRevenue` / `appointmentsRevenue` / `adsRevenue` |
|-------------|----------------|----------------------------------------------------------------|
| **Yes** (FromDate + ToDate) | Total successful revenue **within the date range** (this becomes "إيرادات الشهر" in the UI) | Total successful revenue of that payment type **within the date range** |
| **No** (both omitted — backward compatible) | Payments paid **today** (status = ناجح) | All-time totals per type |

Rules that apply in both modes (unchanged):

- Revenue counts only payments with status **ناجح** (`1`) — refunds excluded.
- `pendingCount` includes pending + in-progress payments.
- `successCount` / `failedCount` / `refundedCount` — when dates are sent, count
  payments within the date range; otherwise keep the current behavior.
- `Type` param still applies: `Type=0/1/2` filters all values to one payment type.
- Response shape **must not change** — the DTO stays identical
  (`todayRevenue`, `subscriptionsRevenue`, `appointmentsRevenue`, `adsRevenue`,
  `pendingCount`, `successCount`, `failedCount`, `refundedCount`).
  The frontend reuses `todayRevenue` as the period total when dates are sent.

## Suggested SQL shape (for reference)

Filter the existing stats queries by the payment's paid date when both dates are
present, e.g. `CAST(PaidDate AS DATE) BETWEEN @FromDate AND @ToDate` (adjust to
the actual date column used by `todayRevenue`).

## Acceptance criteria

1. `GET /api/v1/admin/payments/stats?FromDate=2026-08-01&ToDate=2026-08-31`
   returns only August successful payments in all four revenue fields.
2. Calling the endpoint **without** dates returns the previous behavior
   (today + all-time) — nothing else on the dashboard breaks.
3. Invalid dates (`FromDate > ToDate`, malformed format) → `400` with a
   localized Arabic message.
4. Response envelope stays `ApiResponse<PaymentStatsDto>`.

## Frontend note (already implemented)

The dashboard sends `FromDate`/`ToDate` on every page load (defaults to the
current month) and offers a month selector (`<input type="month">`) above the
KPI cards. No frontend changes are needed once the backend ships this.
