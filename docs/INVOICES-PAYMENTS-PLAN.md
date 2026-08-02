# Invoices & Payments Feature — Implementation Plan

## Overview

Invoices & Payments module for clinic financial management. Handles invoice generation, payment collection (cash, card, insurance), and financial tracking per clinic.

---

## Business Constraints

### Invoices
- Each invoice belongs to exactly **one clinic** and can be linked to **one patient** (optional)
- Invoice statuses: `Draft` → `Issued` → `Paid` | `Cancelled` | `Refunded`
- A `Draft` invoice is editable; once `Issued` it locks line items and amounts
- An invoice can have multiple **line items** (service fees, medications, procedures)
- **Discount** can be a fixed amount or percentage, applied at invoice level
- **Tax** (e.g. VAT 15%) computed per line item or globally; configurable per clinic/clinic-type
- Partial payments allowed (e.g. deposit + final settlement) — tracked via `PaymentSettlements`
- Invoice number format: auto-generated, sequential per clinic (e.g. `INV-2026-0001`)
- Deleting/cancelling an invoice requires a reason; cancellation refunds any associated payments

### Insurance
- Each invoice can optionally attach to a **patient insurance policy**
- Insurance covers a percentage or fixed amount; remaining balance is patient responsibility
- Insurance claim status: `PendingReview` → `Approved` | `Rejected`
- Clinic must be registered with the insurance provider to accept claims

### Payments
- Payment methods: `Cash`, `Card` (Paymob), `BankTransfer`, `InsuranceClaim`
- Card payments go through **Paymob** integration (same flow as subscription payments)
- A single invoice can have **multiple payment settlements** (e.g. partial cash + card)
- Refund: full or partial; refunds always go back through the original payment method
- Payments are immutable once recorded (audit trail)
- Each payment has a unique **transaction reference** from the payment gateway

### Permissions & Plan Features
- `Permission.ManageBilling` (already exists) — grants access to the invoices/payments section
- `PlanFeature.ManageBilling` — **new** plan feature controlling availability
- Sidebar guard: `_user?.Has(Permission.ManageBilling) == true && _user?.HasFeature(PlanFeature.ManageBilling) == true`

---

## Backend API Endpoints Required

### Invoices

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/clinics/{clinicId}/invoices` | List invoices (paginated, filterable by date/status/patient) |
| `GET` | `/api/v1/clinics/{clinicId}/invoices/{invoiceId}` | Get single invoice with line items + payment settlements |
| `POST` | `/api/v1/clinics/{clinicId}/invoices` | Create draft invoice |
| `PUT` | `/api/v1/clinics/{clinicId}/invoices/{invoiceId}` | Update draft invoice (line items, discount, tax) |
| `POST` | `/api/v1/clinics/{clinicId}/invoices/{invoiceId}/issue` | Issue invoice (locks amounts, generates number) |
| `POST` | `/api/v1/clinics/{clinicId}/invoices/{invoiceId}/cancel` | Cancel invoice (requires reason; triggers refund if paid) |
| `GET` | `/api/v1/clinics/{clinicId}/invoices/stats` | Dashboard stats (today's revenue, paid/pending counts, insurance ratio) |

### Invoice Line Items

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/clinics/{clinicId}/invoices/{invoiceId}/items` | Add line item to draft invoice |
| `PUT` | `/api/v1/clinics/{clinicId}/invoices/{invoiceId}/items/{itemId}` | Update line item |
| `DELETE` | `/api/v1/clinics/{clinicId}/invoices/{invoiceId}/items/{itemId}` | Remove line item from draft |

### Payments

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/clinics/{clinicId}/payments` | Record a payment (cash/bank/insurance — no gateway redirect) |
| `POST` | `/api/v1/clinics/{clinicId}/payments/paymob-init` | Initiate Paymob card payment (returns redirect URL) |
| `POST` | `/api/v1/clinics/{clinicId}/payments/paymob-callback` | Paymob webhook/callback to confirm card payment |
| `POST` | `/api/v1/clinics/{clinicId}/payments/{paymentId}/refund` | Refund a payment (full or partial) |
| `GET` | `/api/v1/clinics/{clinicId}/payments` | List payments (paginated, filterable) |
| `GET` | `/api/v1/clinics/{clinicId}/payments/{paymentId}` | Get payment detail |

### Insurance Claims

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/clinics/{clinicId}/insurance/submit` | Submit insurance claim for invoice |
| `GET` | `/api/v1/clinics/{clinicId}/insurance/claims` | List insurance claims |
| `GET` | `/api/v1/clinics/{clinicId}/insurance/claims/{claimId}` | Get claim status and details |

### Reports / Financial

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/clinics/{clinicId}/reports/daily` | Daily revenue report (with breakdown by method) |
| `GET` | `/api/v1/clinics/{clinicId}/reports/monthly` | Monthly revenue report |
| `GET` | `/api/v1/clinics/{clinicId}/reports/insurance` | Insurance claims report |

---

## Frontend Pages / Views

| Route | View | Description |
|-------|------|-------------|
| `/Clinic/Billing` | `Billing.cshtml` | Dashboard with stats grid + invoices table (already scaffolded with mock data) |
| `/Clinic/Invoice/Create` | `InvoiceCreate.cshtml` | Create/edit invoice form (dynamic line items, discount, tax, insurance) |
| `/Clinic/Invoice/{id}` | `InvoiceDetail.cshtml` | Invoice detail with print, payment settlements, refund action |
| `/Clinic/Payments` | `Payments.cshtml` | Payments log with filters |
| `/Clinic/Payment/{id}` | `PaymentDetail.cshtml` | Single payment detail |
| `/Clinic/Insurance/Claims` | `InsuranceClaims.cshtml` | Insurance claims list + submission |

---

## Data Models

### InvoiceDto (response)
```
Id, InvoiceNumber, ClinicId, PatientId?, PatientName,
Status (Draft/Issued/Paid/Cancelled/Refunded),
SubTotal, DiscountType, DiscountValue, TaxRate, TaxAmount, Total,
LineItems[], PaymentSettlements[],
InsuranceProvider?, InsuranceClaimId?,
CreatedAt, IssuedAt?, PaidAt?, CancelledAt?
```

### InvoiceLineItemDto (response)
```
Id, Description, Quantity, UnitPrice, Discount, TaxRate, Total
```

### PaymentDto (response)
```
Id, InvoiceId, InvoiceNumber, Amount, Method (Cash/Card/BankTransfer/Insurance),
TransactionRef?, PaymobPaymentKey?,
Status (Pending/Completed/Failed/Refunded),
RefundedAmount?, RefundReason?,
PaidAt
```

### CreateInvoiceRequest
```
PatientId?, Items[{Description, Quantity, UnitPrice, Discount?}],
DiscountType?, DiscountValue?, TaxRate?,
InsuranceProviderId?, InsurancePolicyNumber?
```

### RecordPaymentRequest
```
InvoiceId, Amount, Method, TransactionRef? (for bank transfer),
Notes?
```

### InitiatePaymobPaymentRequest
```
InvoiceId, Amount, ReturnUrl
```

---

## Implementation Order

1. **Add `PlanFeature.ManageBilling`** to the enum in `Data/Roles.cs`
2. **Create DTOs** — `InvoiceDto`, `InvoiceLineItemDto`, `PaymentDto`, etc. in `Services/ReponseModels/`
3. **Create Request models** — `CreateInvoiceRequest`, `RecordPaymentRequest`, etc. in `Services/RequestModels/`
4. **Add API routes** — `InvoiceRoutes` + `PaymentRoutes` in `DoctoryRoutes.cs`
5. **Create service contract** — `IInvoiceService` in `Services/Contracts/`
6. **Create service implementation** — `InvoiceService` in `Services/Implementations/` following the existing HttpClient + BearerTokenHandler + ClinicHeaderHandler pattern
7. **Register in DI** — `services.AddHttpClient<IInvoiceService, InvoiceService>()` in `DependencyInjection.cs`
8. **Add frontend routes** — `Invoices()`, `Payments()` etc. in `ClinicRoutes.cs`
9. **Add controller actions** — in `ClinicController.cs` with ViewBag data passing
10. **Build views** — Update `Billing.cshtml` with real data binding, create `InvoiceDetail.cshtml` etc.
11. **Update sidebar** — Already exists (links to `Billing`) but update guard to include `PlanFeature.ManageBilling`
12. **Add mock data** — Expand `MockData.cs` with richer invoice/payment data for development

---

## Mock Data

Existing in `Data/MockData.cs`:
- `MockPayment` — Code, Payer, Type, Amount, Method, Status, Date
- `MockPaymentDetail` — with Timeline, Notes, ItemName, Quantity, UnitPrice
- `GetPayments()`, `GetPaymentDetail(id)`, `GetPaymentStats()`, `GetUserPayments(userId)`

Need to add:
- `MockInvoice` — with line items, status workflow
- `GetInvoices(clinicId, filters)` — paginated
- `GetInvoiceStats(clinicId)` — summary numbers for dashboard cards

---

## Payment Gateway (Paymob) Integration

Reuse the existing subscription payment flow pattern:
1. Frontend calls `/api/v1/clinics/{clinicId}/payments/paymob-init` with invoice ID + amount
2. Backend creates Paymob payment key, returns redirect URL
3. Frontend redirects user to Paymob checkout
4. Paymob calls back to `/api/v1/clinics/{clinicId}/payments/paymob-callback`
5. Backend confirms payment, updates invoice status to `Paid`, records payment

For the clinic dashboard, same flow already exists in `SubscriptionService.InitiatePaymentAsync` — the invoices feature can reuse the same underlying Paymob integration.
