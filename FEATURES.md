# ClinicHub (Doctory) — Business Features Inventory

> Every business feature below is **fully implemented and integrated with the backend**
> (View + Controller + Service contract wired end-to-end). Described by: business capability,
> workflow, roles involved, and business rules.

**Roles**: `SystemAdmin` (سوبر أدمن) · `ClinicOwner` (مالك عيادة) · `ClinicManager` · `Doctor` (طبيب) · `ClinicStaff` (استقبال/طاقم) · `Patient` (مريض — عبر تطبيق الموبايل)
**Plan features** (subscription-gated): appointments, patient records, basic/advanced reports, marketing tools, priority support, online booking, staff management, doctor management

---

## 1. Clinic Onboarding & Registration

**Capability:** A clinic registers on the platform and goes through an approval workflow before operating.

- **Registration wizard (3 steps)**: clinic info → documents/attachments → review, with Google Maps location picker
- **RegistrationSubmitted**: 4-step progress ticket — تم الاستلام → قيد المراجعة → الموافقة → التفعيل
- **PendingApproval**: clinic awaits admin review after submitting
- **Verification Center (admin)**: review registered clinics (`PendingClinics` approve/reject, `VerificationCenter` list)
- **VerificationApproved**: activation landing after admin approval (query params: userId, role, status, token)
- **SubscriptionRequired**: gate shown when an approved clinic has no active subscription

**Roles**: Patient/ClinicOwner (register) · SystemAdmin (review/approve/reject)
**Business rules**: pending owners can't log in until approved; FCM token captured at registration time; admin can grant a subscription to activate the dashboard.

## 2. Authentication & Account Management

**Capability:** Role-based login and profile management across all dashboards.

- Login with JWT (access + refresh), logout, token refresh; `returnUrl` support
- Password reset flow: ForgotPassword → 6-digit VerifyCode (TempData email) → ResetPassword
- My profile (all 4 dashboards): full name, phone, birth date, gender, avatar upload — `GET/PATCH /auth/profile`
- Role-based dashboards + permission checks (`Permission` flag enum: manage clinics/doctors/users/subscriptions/payments/specializations/support, etc.)

**Roles**: all

## 3. Subscriptions & Billing (B2B — clinics)

**Capability:** Clinic plans, purchase, feature gating, and admin-side subscription administration.

- **Plan catalog**: 3 plans on public site (`Subscriptions`) with monthly/yearly pricing
- **Plan features** define what each plan unlocks (ManageAppointments, ManagePatientRecords, Basic/AdvancedReports, MarketingTools, PrioritySupport, OnlineBooking, ManageStaff, ManageDoctors)
- **Clinic side**: `MySubscription` — current plan card, usage limits (max doctors/staff), features list; **subscribe** → Paymob payment gateway → `PaymentResult` with countdown redirect; **cancel subscription**
- **Subscription gating**: every clinic request checks `HasActivePlan`; expired/required → redirect to `MySubscription` with Arabic error
- **Admin side**: list all subscriptions, revoke, **create/grant a subscription to a clinic** (clinic + plan + period + start date, price auto-computed), plan management (create/edit/deactivate plans)
- **Reservation settings** (clinic): consultation fee, currency, max advance booking days (30), reservation TTL (10 min), cancellation window (120 min) — GET/PUT `/admin/clinics/settings`

**Roles**: ClinicOwner (buy/manage) · SystemAdmin (grant/revoke/plans)
**Business rules**: creating a subscription unlocks the clinic dashboard immediately; can't use marketing ads below advanced plans (403).

## 4. Payments & Financial Operations (admin)

**Capability:** Full visibility and manual intervention on money movement.

- **Payments list**: filter by type (appointment/subscription/ads), status (pending/success/failed/refunded), method (cash/Paymob), date range, search + pagination
- **Monthly stats**: today revenue, subscriptions/appointments/ads revenue, pending/success/failed/refunded counts (month-scoped)
- **Payment details**: full record + timeline entries (info/success/danger markers)
- **Manual payment**: create a manual subscription payment (cash or Paymob wallet)
- **Refund**: refund a successful payment with reason (confirm modal)
- **User payment history**: per-user payments sub-page

**Roles**: SystemAdmin
**Business rules**: refund only when status = success; manual payments restricted to subscription type; ads orders go through the separate ads flow.

## 5. Ads & Marketing (B2B — clinics)

**Capability:** Clinics purchase advertising packages; admins manage the ad inventory.

- **Clinic side** (`Marketing`): my ads list with status (pending payment/active/expired/cancelled), buy modal — pick package (price × duration), live price preview → Paymob redirect → `AdPaymentResult`
- **Eligibility gate**: requires `MarketingTools` plan feature + active plan, else upsell card → upgrade to advanced plan
- **Admin side** (`Ads`): paginated ad list, deactivate with reason; ad packages CRUD; ad order creation via payments page (eligible clinic + package → Paymob URL in new tab)

**Roles**: ClinicOwner (buy) · SystemAdmin (manage/deactivate)
**Business rules**: ad price = package price × (duration / package duration); instant activation after payment; ads keep running even if subscription lapses.

## 6. Appointments & Queue Management

**Capability:** Day-to-day appointment operations for clinic, doctor, and staff.

- **Doctor appointments** (doctor + clinic-owner views): list, status update with notes (`UpdateDoctorAppointmentStatus`)
- **Staff queue**: paginated queue with **check-in / complete** actions
- **Staff appointments**: approve/reject pending appointments
- **Fixed slot duration**: every appointment = 30 minutes (fixed rule across all doctors/days) — enforced in availability UI and settings page
- **Doctor availability editor** (doctor + clinic-owner views): weekly working hours grid, save as week-replace
- **Reservation window**: booking dates limited to today + maxAdvanceBookingDays (30)

**Roles**: ClinicOwner · Doctor · ClinicStaff
**Business rules**: confirm modal fires action only on تأكيد (cancel/ESC does nothing); cancelled-appointment window enforced backend-side (120 min).

## 7. Patients & Medical Records (EMR)

**Capability:** Patient registry, history, and clinical records.

- **Patients list** (doctor + clinic-owner views) with pagination
- **Patient history**: full visit history per patient (`/Doctor/PatientHistory/{patientId}` + clinic-owner equivalent)
- **Medical records (EMR)**: clinical records management per patient
- **Register patient** (staff): create patient on the spot at reception
- **Patient portal**: patient-facing records view

**Roles**: ClinicOwner · Doctor · ClinicStaff (register) · Patient (view own — mobile)

## 8. Ratings & Reviews

**Capability:** Transparent quality ratings displayed on dashboards.

- **Three rating dimensions**: Doctor (1), Clinic (2), Place Cleanliness (3) — each rated separately
- **Clinic dashboard**: clinic rating + place cleanliness rating, averages, counts, review cards with full/partial star rendering
- **Doctor dashboard**: doctor rating summary + review list
- Submission happens via mobile app (`POST /ratings`); web dashboards read-only

**Roles**: Doctor/ClinicOwner (view) · Patient (submit — mobile)

## 9. Notifications & Web Push (FCM)

**Capability:** Real-time event notifications with role-aware navigation.

- **18 notification types** (0–18): NewMessage, SubscriptionExpiring, AdExpiring, AppointmentOutsideAvailability, AppointmentOutsideWorkingHours, NewBookingRequest, ClinicRegistered, ClinicApproved, ClinicRejected, SupportTicketUpdate, PaymentReceived, RevenueIncreased…
- **Notification bell** (all 4 dashboards): unread count badge + paginated center; list marks items read
- **Web push**: FCM token captured on login/registration, rotated on dashboards; service worker resolves `notificationclick` → per-role destination (appointments for appointment types, clinic pages for clinic types, support for tickets)
- **Appointment notifications** navigate straight to the role's appointments page

**Roles**: all

## 10. Support Tickets

**Capability:** Clinic-side support requests; admin-side ticket management.

- **Clinic side**: submit support requests, view ticket updates
- **Admin side**: manage/respond to tickets (`SupportTicketUpdate` notification type when updated)

**Roles**: ClinicOwner (submit) · SystemAdmin (manage)

## 11. Specializations (Catalog)

**Capability:** Medical specialty catalog powering doctors & clinic registration.

- Admin: list, create, edit, delete specializations with icons (upload place 13 = Specialization/Icons)
- Consumed by: clinic registration wizard, doctor creation, clinic settings, admin user creation

**Roles**: SystemAdmin (manage) · ClinicOwner (select in registration/settings)

## 12. Clinic Operations Management (Admin)

**Capability:** Superadmin supervision of the whole platform.

- **Clinics**: list + detailed view (tabs: overview / doctors / staff / ratings), activate/deactivate
- **Doctors**: list, details, edit, delete, change password; user creation with availability JSON
- **Users**: list + CRUD, change password, per-user sub-pages (Overview / Visits / Requests / Payments)
- **Dashboard**: platform stats, activity feed, urgent support tickets
- **Reports & KPIs**: appointment revenue analytics, operational reports, KPI dashboard (stub — JS redirect)

**Roles**: SystemAdmin

---

## Feature Coverage Summary

| # | Business Feature | Owner | Doctor | Staff | SuperAdmin |
|---|---|---|---|---|---|
| 1 | Clinic onboarding & verification | ✅ | | | ✅ |
| 2 | Auth & account management | ✅ | ✅ | ✅ | ✅ |
| 3 | Subscriptions & billing | ✅ | | | ✅ |
| 4 | Payments & financials | | | | ✅ |
| 5 | Ads & marketing | ✅ | | | ✅ |
| 6 | Appointments & queue | ✅ | ✅ | ✅ | |
| 7 | Patients & medical records | ✅ | ✅ | ✅ | |
| 8 | Ratings & reviews | ✅ | ✅ | | |
| 9 | Notifications & web push | ✅ | ✅ | ✅ | ✅ |
| 10 | Support tickets | ✅ | | | ✅ |
| 11 | Specializations | | | | ✅ |
| 12 | Clinic operations (admin) | | | | ✅ |

**Known gaps**: Admin KPI page (stub), online booking & smart reservation removed from web (kept for mobile app), payment frame legacy.
