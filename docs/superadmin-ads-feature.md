# Ads Feature — Full Spec (الإعلانات)

**Status:** frontend implementation in progress — backend endpoints are **missing** and must be
built per `docs/ads-backend-contract.md`.

## Goal

Complete ads feature covering every scenario:

- **Clinic owner** (web dashboard `Clinic/Marketing`): sees "إعلاناتي" (my ads + statuses), buys
  new ads (package + duration → Paymob checkout), sees pricing, gets upsell when not eligible.
- **Superadmin** (web dashboard): manages all ads (list, filters, deactivate/moderation), manages
  ad packages (CRUD — replaces "seed via SQL"), records cash ad payments (manual payment type 2),
  creates ad orders from the Payments page (already exists).
- **Mobile app** (separate product): displays active ads via a **public API** — the web repo only
  specs the contract, it does not render ads (ads are viewed in mobile, NOT the website).

## Business rules

### Eligibility (subscription integration)

- Buying ads requires: **active subscription AND Advanced plan** (backend enforces → `403`
  `لا يمكن شراء الخمات الإعلانية إلا للباقات المتقدمة`).
- Frontend mirrors the gate for UX: `Clinic/Marketing` shows an **upsell card** (ترقية الباقة →
  `Clinic/MySubscription`) when the clinic lacks the feature/plan.
- Frontend proxy for "Advanced": `PlanFeature.MarketingTools` + `HasActivePlan` (same gate the
  sidebar uses today).
- Superadmin grants Advanced by creating/upgrading the clinic's subscription (see
  `docs/superadmin-create-subscription-api.md` — the create-subscription backend endpoint must
  exist for the manual grant path; the clinic buys the plan via `Clinic/Subscribe` → Paymob).

### Pricing

- **Proportional to duration:** `amount = package.Price × (durationDays / package.DurationDays)`.
- Duration must be a whole positive multiple of the package's `DurationDays` (validated
  client-side + backend).
- Example: package بانر رئيسي = 500 ج.م / 30 يوم → 60 يوم = 1,000 ج.م.

### Lifecycle & statuses

| Status | Meaning | Trigger |
|--------|---------|---------|
| `0` pending-payment (معلق الدفع) | Order created, not yet paid | `POST .../ads/orders` |
| `1` active (نشط) | Paid — visible in mobile app | Paymob webhook success OR admin manual payment (type 2) |
| `2` expired (منتهي) | `EndDate < now` | automatic by date |
| `3` deactivated (ملغي) | Taken down by admin | `POST /api/v1/admin/ads/{id}/deactivate` |

- **Instant activation after payment** (no admin approval): `StartDate = now`,
  `EndDate = now + durationDays` on payment success.
- Manual cash payment (`POST /api/v1/admin/payments/manual` with `type = 2`) **activates the
  clinic's most recent pending-payment ad** (spec'd in the backend contract).
- Deactivated ads are removed from the mobile display immediately and cannot be re-activated.
- **Expired ads keep their display slot removal automatic**; they remain in the clinic's history.

### Subscription interplay

- Ads that are already paid keep running until their `EndDate` even if the clinic's subscription
  lapses or is revoked (already-paid content).
- While ineligible (no active sub / not Advanced): no new purchases — buy button disabled with an
  upsell message; API still returns `403` as the hard gate.

## Mobile display contract (out of scope for the web repo)

- `GET /api/v1/public/ads/active` (public, no auth) → list of active ads (`EndDate ≥ today`,
  status = 1) with creative data: `clinicName`, `clinicLogoUrl`, `packageNameAr`, optional
  `title`, `startDate`, `endDate`.
- The mobile app renders/rotates these banners. The web frontend does NOT build this view.

## Frontend pages (this repo)

| Page | Route | Content |
|------|-------|---------|
| `Clinic/Marketing` (أدوات تسويقية) | `Clinic/Marketing` | Eligibility banner, إعلاناتي list (badges + dates + amounts), buy modal (package + duration + price preview + Paymob), empty states |
| `Admin/Ads` (إدارة الإعلانات) | `Admin/Ads` | Tab 1: all ads table (clinic, package, status, period, amount) + status filter + deactivate + manual cash payment modal; Tab 2: packages CRUD |
| `Admin/Payments` | existing | "طلب خدمة إعلانية" modal (create order → Paymob) — unchanged |

## Backend contract

All endpoints, DTO shapes, statuses and acceptance criteria: `docs/ads-backend-contract.md`.
