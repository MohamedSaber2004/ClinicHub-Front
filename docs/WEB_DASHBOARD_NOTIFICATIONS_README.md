# 🖥️ ClinicHub Web Dashboard — Push Notification Scenarios (SuperAdmin / ClinicOwner / Doctor / Staff)

**Date:** 2026-08-08
**Audience:** web dashboard frontend team

This document lists **every scenario** where the server sends a push notification to a
**dashboard user** (superadmin, clinic owner, doctor, or staff), the **exact `data` payload**
attached to each push (used for navigation), and the two REST endpoints that power the
in-app notification bell.

> Patient-facing pushes are documented in `docs/NOTIFICATIONS_README.md`. This file covers
> only dashboard roles.

---

## 1. Notification REST Endpoints (the bell)

Base route: `api/v1` — both endpoints require `Authorization: Bearer <accessToken>`
(any authenticated role: superadmin, clinic owner, doctor, staff).

### 1.1 Get unread count (badge)

```
GET api/v1/notifications/count
```

**Response (`ApiResponse<int>`):**
```json
{
  "success": true,
  "errors": null,
  "message": null,
  "statusCode": 200,
  "data": 3
}
```

`data` = number of **unread** notifications for the logged-in user.

### 1.2 Get notifications (list)

```
GET api/v1/notifications/pagginated?pageNumber=1&pageSize=20
```

| Param | Type | Default | Validation |
|---|---|---|---|
| `pageNumber` | int | 1 | ≥ 1 |
| `pageSize` | int | 20 | 1 – 100 |

**Response (`ApiResponse<PagginatedResult<NotificationDto>>`):**
```json
{
  "success": true,
  "errors": null,
  "message": null,
  "statusCode": 200,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "userId": "0d8f…",
        "senderUserId": null,
        "titleEn": "",
        "titleAr": "حجز جديد",
        "bodyEn": "",
        "bodyAr": "قام أحمد محمد بحجز موعد في عيادة الأمل بتاريخ 2026-08-10 الساعة 10:00 - 10:30",
        "isRead": true,
        "clinicId": null,
        "createdAt": "2026-08-08T10:30:00Z",
        "type": 12
      }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 1,
    "totalCount": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
}
```

| Field | Type | Meaning |
|---|---|---|
| `id` | Guid | notification id |
| `userId` | Guid | recipient (always the logged-in user) |
| `senderUserId` | Guid? | acting user (chat sender, etc.) |
| `titleAr` / `bodyAr` | string | **Arabic** text — `titleEn`/`bodyEn` are stored empty today |
| `isRead` | bool | read flag |
| `clinicId` | Guid? | related clinic (tenancy) |
| `createdAt` | DateTime | sort newest-first |
| `type` | int | `NotificationType` enum value (see §3) |

> ⚠️ **Read-marking behavior:** the list endpoint **marks every returned item as read**
> server-side. Recommended bell flow: fetch `/count` → show badge → user opens bell →
> fetch the list → refresh the badge.
> ⚠️ **Error responses:** `401` (no/invalid token), `400` (validation, in `errors` as
> `{ "PageNumber": ["…"] }`).

---

## 2. How pushes reach the dashboard

```
Handler / background job
   └─ IFcmService.SendToUserAsync(userId, type, parameters)
        ├─ NotificationBuilderService.BuildAsync → saves dbo.Notifications row + builds data payload
        └─ FcmService.SendToUserAsync → one FCM message per registered device token of the user
```

Every push message contains:

```
notification: { title, body }                     ← Arabic, hardcoded server-side
data: {
  "type": "NewBookingRequest",                    ← NotificationType name (see §3)
  "link": "https://your-frontend/appointments/3fa8…",  ← deep link ("" if none)
  …type-specific keys (ids used for navigation)
}
```

**Navigation rule for the dashboard:** on push click / foreground message, read `data.link`
(or use `data.type` + the ids inside `data` to build your own route). The ids present in the
payload (`appointmentId`, `clinicId`, `ticketId`, `conversationId`) are the same ids the REST
endpoints use.

The deep links are configured in `ClinicHub.Application/Common/DeepLinkRoutes.cs`
(frontend base = `EmailSettings.FrontendUrl`):

| Route | Path |
|---|---|
| Appointments list | `/appointments` |
| Appointment details | `/appointments/{appointmentId}` |
| Chat | `/chat/{conversationId}` |
| Clinics list | `/clinics` |
| Clinic details | `/clinics/{clinicId}` |
| Support ticket | `/support-tickets/{ticketId}` |
| Notifications | `/notifications` |

---

## 3. Scenario catalogue (per role) — with `data` payloads

### 3.1 SuperAdmin

| # | Scenario | Type (value) | Trigger | `data` keys | Deep link | Code |
|---|---|---|---|---|---|---|
| S1 | **New clinic registered (pending approval)** | `ClinicRegistered` (13) | A clinic owner completes `POST /clinics/register` | `clinicName, clinicId, ownerName` | `/clinics/{clinicId}` | `Features/Clinics/Commands/RegisterClinic/RegisterClinicCommandHandler.cs` |
| S2 | **Appointment paid — revenue increased** | `RevenueIncreased` (18) | Paymob webhook confirms an appointment payment | `amount, clinicName, totalRevenue, appointmentId` | `/appointments/{appointmentId}` | `Features/Payment/Commands/ConfirmPaymentWebhook/ConfirmPaymentWebhookCommandHandler.cs` |

> S1–S2 were **added** in this update — previously the superadmin received nothing when a new
> clinic registered or when revenue grew. S2 recipients: all users with role `SuperAdmin`.
> `totalRevenue` = running sum of all paid appointment payments.

### 3.2 ClinicOwner

| # | Scenario | Type (value) | Trigger | `data` keys | Deep link | Code |
|---|---|---|---|---|---|---|
| O1 | **New booking request received** | `NewBookingRequest` (12) | A patient creates an appointment (`POST /appointments`) | `patientName, clinicName, doctorName, date, time, appointmentId` | `/appointments/{appointmentId}` | `Features/Appointments/Commands/CreateAppointment/CreateAppointmentCommandHandler.cs` |
| O2 | **Clinic approved** | `ClinicApproved` (14) | SuperAdmin approves the clinic | `clinicName, clinicId` | `/clinics/{clinicId}` | `Features/Admin/Commands/ApproveClinic/ApproveClinicCommandHandler.cs` |
| O3 | **Clinic rejected** | `ClinicRejected` (15) | SuperAdmin rejects the clinic | `clinicName, reason` | `/clinics` | `Features/Admin/Commands/RejectClinic/RejectClinicCommandHandler.cs` |
| O4 | **Appointment outside clinic working hours** | `AppointmentOutsideWorkingHours` (11) | Hourly validation job | `clinicName, date, time, appointmentId` | `/appointments/{appointmentId}` | `Infrastructure/Services/BackgroundJobs/ClinicWorkingHoursValidationJob.cs` |
| O5 | **Appointment outside doctor availability** | `AppointmentOutsideAvailability` (10) | Hourly validation job | `clinicName, date, time, appointmentId` | `/appointments/{appointmentId}` | `Infrastructure/Services/BackgroundJobs/DoctorAvailabilityValidationJob.cs` |
| O6 | **Subscription expiring** | `SubscriptionExpiring` (7) | Daily job (3-day & 1-day windows) | `clinicName, date, period` | `/notifications` | `Infrastructure/Services/BackgroundJobs/ExpiryReminderJob.cs` |
| O7 | **Ad expiring** | `AdExpiring` (9) | Daily job (3-day & 1-day windows) | `clinicName, date, period` | `/notifications` | `Infrastructure/Services/BackgroundJobs/ExpiryReminderJob.cs` |
| O8 | **Ticket status updated** | `SupportTicketUpdate` (16) | SuperAdmin updates a support ticket | `ticketId, subject, status` | `/support-tickets/{ticketId}` | `Features/Admin/Commands/UpdateSupportTicketStatus/UpdateSupportTicketStatusCommandHandler.cs` |
| O9 | **Appointment paid (revenue received)** | `PaymentReceived` (17) | Paymob webhook confirms an appointment payment | `amount, patientName, clinicName, appointmentId` | `/appointments/{appointmentId}` | `Features/Payment/Commands/ConfirmPaymentWebhook/ConfirmPaymentWebhookCommandHandler.cs` |
| O10 | **New chat message** | `NewMessage` (1) | A message is sent in a conversation you belong to | `senderName, conversationId` | `/chat/{conversationId}` | `Features/Conversations/Commands/SendMessage/SendMessageCommandHandler.cs` |
| O11 | **Booking accepted (shared acceptance event)** | `AppointmentAccepted` (19) | Clinic admin/staff/doctor accepts the booking request | `patientName, clinicName, doctorName, date, time, appointmentId` | `/appointments/{appointmentId}` | `Application/Common/Services/AppointmentAcceptanceService.cs` |

> O1–O3, O8 and O9 were **added** in this update — previously the owner was never told about
> new bookings, approval/rejection outcomes, ticket updates, or received payments.
> O11 fires from the same acceptance event as the patient's `AppointmentConfirmation` (3).

### 3.3 Doctor

| # | Scenario | Type (value) | Trigger | `data` keys | Deep link | Code |
|---|---|---|---|---|---|---|
| D1 | **New booking request for this doctor** | `NewBookingRequest` (12) | A patient books with this doctor | `patientName, clinicName, doctorName, date, time, appointmentId` | `/appointments/{appointmentId}` | `CreateAppointmentCommandHandler.cs` |
| D2 | **Appointment outside doctor availability** | `AppointmentOutsideAvailability` (10) | Hourly validation job | `clinicName, date, time, appointmentId` | `/appointments/{appointmentId}` | `DoctorAvailabilityValidationJob.cs` |
| D3 | **New chat message** | `NewMessage` (1) | Chat message | `senderName, conversationId` | `/chat/{conversationId}` | `SendMessageCommandHandler.cs` |
| D4 | **Booking accepted (shared acceptance event)** | `AppointmentAccepted` (19) | Clinic admin/staff/doctor accepts the booking request | `patientName, clinicName, doctorName, date, time, appointmentId` | `/appointments/{appointmentId}` | `AppointmentAcceptanceService.cs` |

### 3.4 Staff

| # | Scenario | Type (value) | Trigger | `data` keys | Deep link | Code |
|---|---|---|---|---|---|---|
| ST1 | **New booking request in the clinic** | `NewBookingRequest` (12) | A patient books at the staff member's clinic | `patientName, clinicName, doctorName, date, time, appointmentId` | `/appointments/{appointmentId}` | `CreateAppointmentCommandHandler.cs` |
| ST2 | **New chat message** | `NewMessage` (1) | Chat message | `senderName, conversationId` | `/chat/{conversationId}` | `SendMessageCommandHandler.cs` |

> ST1 was **added** in this update — staff previously received nothing when a patient booked.

### 3.5 Payload details per new type

**`NewBookingRequest` (12)** — recipients: appointment's doctor + clinic owner (`ClinicAdminId`) + all staff users whose `ClinicId` = the clinic.
```json
{
  "type": "NewBookingRequest",
  "patientName": "أحمد محمد",
  "clinicName": "عيادة الأمل",
  "doctorName": "د. سارة أحمد",
  "date": "2026-08-10",
  "time": "10:00 - 10:30",
  "appointmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "link": "https://your-frontend/appointments/3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```
Title: `حجز جديد` — Body: `قام {patientName} بحجز موعد في {clinicName} بتاريخ {date} الساعة {time}`

**`ClinicRegistered` (13)** — recipients: all superadmins.
```json
{
  "type": "ClinicRegistered",
  "clinicName": "عيادة الأمل",
  "clinicId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "ownerName": "أحمد محمد",
  "link": "https://your-frontend/clinics/3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```
Title: `تسجيل عيادة جديدة` — Body: `تسجيل عيادة جديدة بانتظار الموافقة: {clinicName}`

**`ClinicApproved` (14)** — recipient: clinic owner.
```json
{
  "type": "ClinicApproved",
  "clinicName": "عيادة الأمل",
  "clinicId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "link": "https://your-frontend/clinics/3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```
Title: `تمت الموافقة على العيادة` — Body: `تمت الموافقة على عيادة {clinicName} — يمكنك الآن بدء العمل`

**`ClinicRejected` (15)** — recipient: clinic owner.
```json
{
  "type": "ClinicRejected",
  "clinicName": "عيادة الأمل",
  "reason": "المستندات غير مكتملة",
  "link": "https://your-frontend/clinics"
}
```
Title: `تم رفض العيادة` — Body: `عذرًا، تم رفض تسجيل عيادة {clinicName}: {reason}`

**`SupportTicketUpdate` (16)** — recipient: ticket owner (`SupportTicket.UserId`).
```json
{
  "type": "SupportTicketUpdate",
  "ticketId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "subject": "مشكلة في الدفع",
  "status": "InProgress",
  "link": "https://your-frontend/support-tickets/3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```
Title: `تحديث تذكرة الدعم` — Body: `تم تحديث حالة تذكرتك «{subject}» إلى {status}`
(`status` values: `Open`, `InProgress`, `Resolved`, `Closed`).

**`PaymentReceived` (17)** — recipient: clinic owner (`Clinic.ClinicAdminId`).
```json
{
  "type": "PaymentReceived",
  "amount": "250.00 EGP",
  "patientName": "أحمد محمد",
  "clinicName": "عيادة الأمل",
  "appointmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "link": "https://your-frontend/appointments/3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```
Title: `تم استلام الدفع` — Body: `تم استلام دفعة بقيمة {amount} لحجز {patientName} في عيادتك {clinicName}`

**`RevenueIncreased` (18)** — recipients: all superadmins. Sent on every paid **appointment** payment.
```json
{
  "type": "RevenueIncreased",
  "amount": "250.00 EGP",
  "clinicName": "عيادة الأمل",
  "totalRevenue": "12850.00 EGP",
  "appointmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "link": "https://your-frontend/appointments/3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```
Title: `زيادة الإيرادات` — Body: `تم دفع {amount} لحجز في عيادة {clinicName} — إجمالي الإيرادات الآن {totalRevenue}`
(`totalRevenue` = running sum of **all** paid appointment payments at that moment).

---

## 4. What each dashboard role receives (summary)

| Role | Types |
|---|---|
| **SuperAdmin** | `ClinicRegistered`, `RevenueIncreased` |
| **ClinicOwner** | `NewBookingRequest`, `AppointmentAccepted`, `ClinicApproved`, `ClinicRejected`, `PaymentReceived`, `AppointmentOutsideWorkingHours`, `AppointmentOutsideAvailability`, `SubscriptionExpiring`, `AdExpiring`, `SupportTicketUpdate`, `NewMessage` |
| **Doctor** | `NewBookingRequest`, `AppointmentAccepted`, `AppointmentOutsideAvailability`, `NewMessage` |
| **Staff** | `NewBookingRequest`, `NewMessage` |
| **Everyone** | `NewMessage` (chat) |

**Background-job cadence** (bursts possible — debounce badge refresh):
- Hourly: subscriptions-expiration, ads-expiration, doctor-availability-validation, clinic-working-hours-validation.
- Daily: expiry-reminders.
- On demand: booking created, clinic registered/approved/rejected, ticket updated, chat message.

---

## 5. Dashboard integration checklist

- [ ] On load: `GET api/v1/notifications/count` → badge; on bell open: `GET api/v1/notifications/pagginated?pageNumber=1&pageSize=20` → list (infinite scroll until `hasNextPage=false`).
- [ ] The list endpoint marks items read — fetch list **before** refreshing the badge.
- [ ] Foreground push (`onMessage`): toast + navigate via `data.link` (fallback: build route from `data.type` + ids).
- [ ] Background push: service worker `notificationclick` → `clients.openWindow(event.notification.data.link)`.
- [ ] Display `titleAr`/`bodyAr`; ignore `titleEn`/`bodyEn` (stored empty).
- [ ] Register FCM token at every login: `POST api/v1/auth/login-web` with `fcmToken` (non-empty string) + `devicePlatform: 0` (number).
- [ ] **Clinic registration** (`POST /clinics/register`) also accepts `fcmToken` + `devicePlatform` — the owner is blocked from logging in until approval (`AccountPendingApproval`), so the signup device must send its token **at registration time** or `ClinicApproved`/`ClinicRejected` pushes can't be delivered.
