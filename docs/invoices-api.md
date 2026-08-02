# Invoices & Payments API — Frontend Integration Guide

## Base URL

```
/api/v1/clinics/{clinicId}/invoices
/api/v1/clinics/{clinicId}/payments
```

All endpoints require:
- `[Authorize]` + `[RequirePlanPermission(ManageBilling)]` — the user must have the `ManageBilling` subscription permission
- The `Accept-Language` header controls response language (`ar` default, `en`).

---

## Response Envelope

Every response is wrapped in `ApiResponse<TData>`:

```json
{
  "success": true,
  "data": { ... },
  "message": "...",
  "errors": {},
  "statusCode": 200
}
```

On validation failure (HTTP 400):
```json
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": {
    "Items[0].Description": ["Description is required"]
  },
  "statusCode": 400
}
```

---

## Enums

### InvoiceStatus
| Value | Name |
|-------|------|
| `0` | Draft |
| `1` | Issued |
| `2` | Paid |
| `3` | Cancelled |
| `4` | Refunded |

### DiscountType
| Value | Name |
|-------|------|
| `0` | Percentage |
| `1` | Fixed |

### PaymentMethodType
| Value | Name |
|-------|------|
| `0` | Cash |
| `1` | Card |
| `2` | Wallet |

---

## Endpoints

### 1. List Invoices (Paginated)

```
GET  /api/v1/clinics/{clinicId}/invoices
```

**Query parameters:**

| Param | Type | Description |
|-------|------|-------------|
| `PageNumber` | int | Default `1` |
| `PageSize` | int | Default `10` |
| `Status` | int? | Filter by `InvoiceStatus` |
| `FromDate` | ISO8601? | Filter from (UTC) |
| `ToDate` | ISO8601? | Filter to (UTC) |
| `PatientId` | guid? | Filter by patient |

**Response:** `ApiResponse<PagginatedResult<InvoiceDto>>`

```json
{
  "data": {
    "items": [ /* InvoiceDto[] */ ],
    "totalCount": 42,
    "pageNumber": 1,
    "pageSize": 10
  }
}
```

---

### 2. Get Invoice By ID

```
GET  /api/v1/clinics/{clinicId}/invoices/{invoiceId}
```

**Response:** `ApiResponse<InvoiceDto>` — full invoice with line items and payment settlements.

---

### 3. Get Invoice Stats (Dashboard)

```
GET  /api/v1/clinics/{clinicId}/invoices/stats
```

**Response:** `ApiResponse<InvoiceStatsDto>`

```json
{
  "data": {
    "todayRevenue": 1250.00,
    "paidCount": 15,
    "pendingCount": 3,
    "draftCount": 7,
    "cancelledCount": 2,
    "insuranceRatio": 0.35
  }
}
```

---

### 4. Create Draft Invoice

```
POST  /api/v1/clinics/{clinicId}/invoices
```

**Request body:**

```json
{
  "patientId": "guid or null",
  "items": [
    {
      "description": "Consultation fee",
      "quantity": 1,
      "unitPrice": 100.00,
      "discount": 0
    }
  ],
  "discountType": 0,
  "discountValue": 10,
  "taxRate": 14
}
```

**Rules:**
- `PatientId` (optional) — must reference an existing `ApplicationUser` if provided
- `Items` must have at least 1 item; each item needs `description` (max 500), `quantity` (≥1), `unitPrice` (≥0)
- `Discount` per item is a **percentage** (0–100)
- `DiscountValue` depends on `DiscountType`: if Percentage → 0–100; if Fixed → ≥0
- `TaxRate` 0–100
- Invoice is created as **Draft** status

**Response:** `ApiResponse<InvoiceDto>` — HTTP 200

---

### 5. Update Draft Invoice

```
PUT  /api/v1/clinics/{clinicId}/invoices/{invoiceId}
```

**Request body:** Same shape as Create (with line item `id` fields for existing items).

```json
{
  "patientId": "guid or null",
  "items": [
    {
      "id": "guid of existing item, or null for new",
      "description": "Updated description",
      "quantity": 2,
      "unitPrice": 100.00,
      "discount": 5
    }
  ],
  "discountType": 1,
  "discountValue": 20,
  "taxRate": 14
}
```

**Rules:**
- Invoice must be in **Draft** status
- Items with an `id` that exists are updated; items without an `id` are created; items not in the array are deleted
- Same validation rules as Create

**Response:** `ApiResponse<InvoiceDto>`

---

### 6. Issue Invoice (Draft → Issued)

```
POST  /api/v1/clinics/{clinicId}/invoices/{invoiceId}/issue
```

**Rules:**
- Invoice must be in **Draft** status
- An `InvoiceNumber` is auto-generated: `INV-{Year}-{Seq:0004}` (e.g. `INV-2026-0001`)
- Sets `IssuedAt` to UTC now

**Response:** `ApiResponse<InvoiceDto>`

---

### 7. Cancel Invoice

```
POST  /api/v1/clinics/{clinicId}/invoices/{invoiceId}/cancel
```

**Request body:**

```json
{
  "reason": "Patient cancelled appointment"
}
```

**Rules:**
- Invoice can be cancelled from **Draft**, **Issued**, or **Paid** status
- `Reason` is optional, max 500 characters

**Response:** `ApiResponse<InvoiceDto>`

---

### 8. Record Payment

```
POST  /api/v1/clinics/{clinicId}/payments
```

**Request body:**

```json
{
  "invoiceId": "guid",
  "amount": 100.00,
  "method": 0,
  "transactionRef": "optional ref",
  "notes": "optional notes"
}
```

**Rules:**
- Invoice must be in **Issued** status
- `Amount` must be ≤ invoice `Total`
- If `method` is `Card` (1) or `Wallet` (2), `TransactionRef` is **required**
- If `method` is `Cash` (0), `TransactionRef` is optional (auto-generated if not provided)
- On success, invoice status changes to **Paid**, `PaidAt` is set

**Response:** `ApiResponse<guid>` — returns the Payment ID

---

## DTO Reference

### InvoiceDto

| Field | Type | Notes |
|-------|------|-------|
| `id` | guid | |
| `invoiceNumber` | string | `INV-2026-0001` — set after issuing |
| `clinicId` | guid | |
| `patientId` | guid? | nullable |
| `status` | int | `InvoiceStatus` enum |
| `subTotal` | decimal | Sum of line-item totals |
| `discountType` | int | `DiscountType` enum |
| `discountValue` | decimal | |
| `taxRate` | decimal | percentage |
| `taxAmount` | decimal | Computed: `(subTotal - discount) * taxRate / 100` |
| `total` | decimal | `subTotal - discount + taxAmount` |
| `lineItems` | `InvoiceLineItemDto[]` | |
| `paymentSettlements` | `PaymentSettlementDto[]` | |
| `cancellationReason` | string? | |
| `createdAt` | ISO8601 | |
| `issuedAt` | ISO8601? | |
| `paidAt` | ISO8601? | |
| `cancelledAt` | ISO8601? | |

### InvoiceLineItemDto

| Field | Type | Notes |
|-------|------|-------|
| `id` | guid | |
| `description` | string | |
| `quantity` | int | ≥1 |
| `unitPrice` | decimal | |
| `discount` | decimal | Percentage per item (0–100) |
| `taxRate` | decimal | Currently 0 (tax applied at invoice level) |
| `total` | decimal | Computed: `(qty * unitPrice) * (1 - discount/100) * (1 + taxRate/100)` |

### PaymentSettlementDto

| Field | Type | Notes |
|-------|------|-------|
| `id` | guid | |
| `invoiceId` | guid | |
| `amount` | decimal | |
| `method` | int | `PaymentMethodType` enum |
| `transactionRef` | string? | |
| `paymobPaymentKey` | string? | (future Paymob integration) |
| `status` | string | Always `"Completed"` |
| `refundedAmount` | decimal? | |
| `refundReason` | string? | |
| `paidAt` | ISO8601 | |

### InvoiceStatsDto

| Field | Type | Notes |
|-------|------|-------|
| `todayRevenue` | decimal | Sum of paid totals today |
| `paidCount` | int | |
| `pendingCount` | int | Issued but not paid |
| `draftCount` | int | |
| `cancelledCount` | int | |
| `insuranceRatio` | decimal | Ratio of insurance claims (future) |

---

## Frontend Workflow

```
[Draft] ──issue──→ [Issued] ──record payment──→ [Paid]
   │                    │                          │
   ├──update───────────┘                          │
   └──cancel────────────┴──────────────────────────┘ → [Cancelled]
                                                      [Refunded] ← future
```

1. **Create Draft** — build invoice with line items, discount, tax. Save as draft.
2. **Update Draft** — modify items, discount, tax while still in draft.
3. **Issue** — confirm the invoice. An invoice number is assigned. Invoice becomes immutable (except cancel).
4. **Record Payment** — accept cash/card/wallet against the issued invoice. Marks it paid.
5. **Cancel** — cancel any time (draft, issued, or paid).

---

## Localization Messages

All user-facing messages use localization keys. Default language is **Arabic** (changeable via `Accept-Language` header to `en`).

### InvoiceMessages Keys (for UI display)

| Key | English |
|-----|---------|
| `InvoiceCreated` | Invoice created successfully |
| `InvoiceUpdated` | Invoice updated successfully |
| `InvoiceIssued` | Invoice issued successfully |
| `InvoiceCancelled` | Invoice cancelled successfully |
| `InvoicePaid` | Invoice paid successfully |
| `NotFound` | Invoice not found |
| `InvalidStatus` | Invoice status does not allow this operation |
| `NoItems` | Invoice must have at least one item |
| `NoChanges` | No changes detected |
| `PaymentRecorded` | Payment recorded successfully |
| `PaymentNotFound` | Payment not found |
| `CantEditIssued` | Cannot edit an issued invoice |
| `AlreadyPaid` | Invoice is already paid |
| `PatientNotFound` | Patient not found |
| `ItemNotFound` | Invoice item not found |
