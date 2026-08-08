# 🔔 ClinicHub Notification Types — Complete Catalogue

Every notification in ClinicHub is both **persisted** (`dbo.Notifications` → in-app bell) and **pushed via FCM** to the user's registered devices (web/mobile). This file explains each `NotificationType` (0–19): what triggers it, who receives it, the `data` payload keys, and the deep link.

> Default culture is Arabic (`Accept-Language` header) — titles/bodies below are the Arabic strings produced by `NotificationBuilderService`.
> `NotificationType` is stored as an int in `dbo.Notifications`; values are appended, never renumbered — no DB migration is needed when adding new types.

---

## 1. Booking lifecycle (patient ↔ clinic)

| # | Type | Recipients | Trigger | `data` keys | Deep link |
|---|------|-----------|---------|-------------|-----------|
| 3 | `AppointmentConfirmation` | Patient (`BookedByUserId`) | Clinic admin/staff/doctor **accepts** the booking request | `clinicName, date, appointmentId, paymentUrl` | `/appointments/{appointmentId}` |
| 19 | `AppointmentAccepted` | Appointment's **doctor** + **clinic owner** | Same acceptance event (shared `AppointmentAcceptanceService`) | `patientName, clinicName, doctorName, date, time, appointmentId` | `/appointments/{appointmentId}` |
| 12 | `NewBookingRequest` | Doctor + clinic owner + all clinic staff | Patient **creates** an appointment | `patientName, clinicName, doctorName, date, time, appointmentId` | `/appointments/{appointmentId}` |
| 4 | `AppointmentCancellation` | Patient | Appointment cancelled (by clinic, or reservation auto-expired, or abandoned payment) | `clinicName, reason, appointmentId` | `/appointments` |
| 0 | `AppointmentReminder` | Patient | Scheduled reminder before the visit | `clinicName, time` | `/appointments/{appointmentId}` |
| 6 | `CancellationWindowClosed` | Patient | Cancellation/refund window closed before the visit | `clinicName, appointmentId` | `/appointments/{appointmentId}` |
| 10 | `AppointmentOutsideAvailability` | Doctor + clinic owner | Hourly job: booking falls outside doctor availability | `clinicName, date, time, appointmentId` | `/appointments/{appointmentId}` |
| 11 | `AppointmentOutsideWorkingHours` | Clinic owner | Hourly job: booking outside clinic working hours | `clinicName, date, time, appointmentId` | `/appointments/{appointmentId}` |

**Flow summary:** patient books → `NewBookingRequest` (12) to doctor+owner+staff → clinic approves → patient gets `AppointmentConfirmation` (3) with payment link, doctor+owner get `AppointmentAccepted` (19) → payment completes → payment notifications (see §3).

## 2. Clinic registration & admin

| # | Type | Recipients | Trigger | `data` keys | Deep link |
|---|------|-----------|---------|-------------|-----------|
| 13 | `ClinicRegistered` | All superadmins | Clinic owner registers a new clinic (pending approval) | `clinicName, clinicId, ownerName` | `/clinics/{clinicId}` |
| 14 | `ClinicApproved` | Clinic owner | Superadmin approves the clinic | `clinicName, clinicId` | `/clinics/{clinicId}` |
| 15 | `ClinicRejected` | Clinic owner | Superadmin rejects the clinic | `clinicName, reason` | `/clinics` |
| 16 | `SupportTicketUpdate` | Ticket owner (`SupportTicket.UserId`) | Superadmin changes ticket status | `ticketId, subject, status` | `/support-tickets/{ticketId}` |

## 3. Payments & revenue

| # | Type | Recipients | Trigger | `data` keys | Deep link |
|---|------|-----------|---------|-------------|-----------|
| 2 | `PaymentConfirmation` | Patient | Paymob webhook confirms the payment | `amount, appointmentId` | `/appointments/{appointmentId}` |
| 17 | `PaymentReceived` | Clinic owner (`Clinic.ClinicAdminId`) | Paymob webhook confirms an **appointment** payment | `amount, patientName, clinicName, appointmentId` | `/appointments/{appointmentId}` |
| 18 | `RevenueIncreased` | All superadmins | Paymob webhook confirms an **appointment** payment | `amount, clinicName, totalRevenue, appointmentId` | `/appointments/{appointmentId}` |
| 8 | `RefundProcessed` | Patient | Refund successfully processed (auto or retried) | `clinicName, amount, appointmentId` | `/appointments/{appointmentId}` |

> `totalRevenue` = running sum of **all** paid appointment payments at send time.
> `PaymentReceived`/`RevenueIncreased` fire only for `PaymentType.Appointment` payments, not e.g. ads.

## 4. Subscriptions & ads

| # | Type | Recipients | Trigger | `data` keys | Deep link |
|---|------|-----------|---------|-------------|-----------|
| 7 | `SubscriptionExpiring` | Clinic owner | Daily job: subscription ends in 3 days / 1 day | `clinicName, date, period` | `/notifications` |
| 9 | `AdExpiring` | Clinic owner | Daily job: ad ends in 3 days / 1 day | `clinicName, date, period` | `/notifications` |

## 5. Messaging & system

| # | Type | Recipients | Trigger | `data` keys | Deep link |
|---|------|-----------|---------|-------------|-----------|
| 1 | `NewMessage` | All conversation participants | A message is sent in a conversation you belong to | `senderName, conversationId` | `/chat/{conversationId}` |
| 5 | `SystemAnnouncement` | Target audience | Manual/planned announcement | `message` | `/notifications` |

---

## 6. Code reference — where each type is sent

| Type | Sending code |
|------|-------------|
| `AppointmentReminder` | `Infrastructure/Services/BackgroundJobs/` (reminder job) |
| `NewMessage` | `Features/Conversations/Commands/SendMessage/SendMessageCommandHandler.cs` |
| `PaymentConfirmation` | `Features/Payment/Commands/ConfirmPaymentWebhook/ConfirmPaymentWebhookCommandHandler.cs` |
| `AppointmentConfirmation` / `AppointmentAccepted` | `Application/Common/Services/AppointmentAcceptanceService.cs` |
| `AppointmentCancellation` | `CancelAppointmentCommandHandler.cs`, `ReservationExpirationJob.cs`, `AbandonedPaymentJob.cs` |
| `CancellationWindowClosed` | `Infrastructure/Services/BackgroundJobs/CancellationWindowJob.cs` |
| `SubscriptionExpiring` / `AdExpiring` | `Infrastructure/Services/BackgroundJobs/ExpiryReminderJob.cs` |
| `RefundProcessed` | `Infrastructure/Services/BackgroundJobs/RefundRetryJob.cs` |
| `AppointmentOutsideAvailability` | `Infrastructure/Services/BackgroundJobs/DoctorAvailabilityValidationJob.cs` |
| `AppointmentOutsideWorkingHours` | `Infrastructure/Services/BackgroundJobs/ClinicWorkingHoursValidationJob.cs` |
| `NewBookingRequest` | `Features/Appointments/Commands/CreateAppointment/CreateAppointmentCommandHandler.cs` |
| `ClinicRegistered` | `Features/Clinics/Commands/RegisterClinic/RegisterClinicCommandHandler.cs` |
| `ClinicApproved` | `Features/Admin/Commands/ApproveClinic/ApproveClinicCommandHandler.cs` |
| `ClinicRejected` | `Features/Admin/Commands/RejectClinic/RejectClinicCommandHandler.cs` |
| `SupportTicketUpdate` | `Features/Admin/Commands/UpdateSupportTicketStatus/UpdateSupportTicketStatusCommandHandler.cs` |
| `PaymentReceived` / `RevenueIncreased` | `Features/Payment/Commands/ConfirmPaymentWebhook/ConfirmPaymentWebhookCommandHandler.cs` |

## 7. Dashboard roles — what each one receives

| Role | Types |
|------|-------|
| **SuperAdmin** | `ClinicRegistered`, `RevenueIncreased` |
| **ClinicOwner** | `NewBookingRequest`, `ClinicApproved`, `ClinicRejected`, `PaymentReceived`, `AppointmentAccepted`, `AppointmentOutsideWorkingHours`, `AppointmentOutsideAvailability`, `SubscriptionExpiring`, `AdExpiring`, `SupportTicketUpdate`, `NewMessage` |
| **Doctor** | `NewBookingRequest`, `AppointmentAccepted`, `AppointmentOutsideAvailability`, `NewMessage` |
| **Staff** | `NewBookingRequest`, `NewMessage` |
| **Patient (mobile)** | `AppointmentConfirmation`, `PaymentConfirmation`, `AppointmentCancellation`, `AppointmentReminder`, `CancellationWindowClosed`, `RefundProcessed`, `NewMessage`, `SystemAnnouncement` |

## 8. Delivery notes

- **Persist + push:** every `SendToUserAsync` call persists the notification row first, then dispatches one FCM message per registered device token of the recipient; FCM failures never block business logic and are logged.
- **Token registration:** web tokens must reach the backend — at login (`login-web` payload `fcmToken` + `devicePlatform: 0`), at clinic registration (pending owners can't log in), and via `POST /api/v1/auth/fcm-token`.
- **Deep links:** dashboard navigation maps `type` → role hub page; mobile deep links use `link` (see `Application/Common/DeepLinkRoutes.cs`).
