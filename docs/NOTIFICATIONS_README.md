# 🔔 ClinicHub Notifications — Feature Guide & Web Dashboard Integration

This README documents the complete **notification feature** of ClinicHub:

1. How the notification system works (architecture, entities, types).
2. How the **web dashboard** integrates with the notification REST API endpoints.
3. How to **receive push notifications correctly** from the backend (FCM web push) and real-time events (Pusher).
4. A full **analysis of every scenario** in the project that sends a notification, with who receives it, what text/payload is sent, and where it happens in the code.

---

## 1. Architecture Overview

```
Business event                          ┌───────────────────────────────────────────────┐
(booking accepted, payment,             │               ClinicHub API                    │
cancel, refund, expiry, chat ...)       │                                               │
        │                               │  Handler calls:                               │
        ▼                               │  _fcmService.SendToUserAsync(userId,          │
┌─────────────────────────────┐         │                               type, params)   │
│ MediatR handler / Hangfire  │ ──────► │        │                                       │
│ background job              │         │        ▼                                       │
└─────────────────────────────┘         │  NotificationBuilderService.BuildAsync         │
                                        │  • resolves Arabic title + body (switch)      │
                                        │  • builds `data` payload (type, ids, link)    │
                                        │  • saves a row in `dbo.Notifications`         │
                                        │    (in-app history — survives missed pushes)  │
                                        │        │                                       │
                                        │        ▼                                       │
                                        │  FcmService.SendToUserAsync                    │
                                        │  • loads ALL device tokens of the user        │
                                        │    from `dbo.UserFbTokens`                     │
                                        │  • sends one FCM message per device           │
                                        │    (Webpush / Android / Apns configs)         │
                                        │  • deletes tokens FCM reports as unregistered │
                                        │        │                                       │
                                        └────────┼──────────────────────────────────────┘
                                                 ▼
                                        ┌──────────────────┐
                                        │ Firebase Cloud   │  (firebase-admin SDK,
                                        │ Messaging (FCM)  │   legacy/native API)
                                        └──────────────────┘
                                          │          │          │
                                          ▼          ▼          ▼
                                     Web dashboard   Android     iOS
                                    (firebase-messaging   (FCM SDK)   (APNs)
                                     JS SDK + service
                                     worker)
```

**Key components & files:**

| Component | File | Role |
|---|---|---|
| Domain entity | `ClinicHub.Domain/Entities/Notification.cs` | `Notifications` table row (in-app history) |
| Type enum | `ClinicHub.Domain/Enums/NotificationType.cs` | 12 notification types (0–11) |
| Device tokens | `ClinicHub.Domain/Entities/UserFbToken.cs` | FCM tokens per user per platform |
| Platform enum | `ClinicHub.Domain/Enums/DevicePlatform.cs` | `Web=0, Android=1, iOS=2` |
| Payload builder | `ClinicHub.Infrastructure/Services/NotificationBuilderService.cs` | Arabic title/body + `data` payload + persists DB row |
| FCM sender | `ClinicHub.Infrastructure/Services/FcmService.cs` | Firebase init, send to all tokens, register/unregister tokens, token cleanup |
| Real-time sender | `ClinicHub.Infrastructure/Services/PusherService.cs` | Pusher events (chat) |
| API endpoints | `ClinicHub.API/Controllers/Version1/NotificationsController.cs` | In-app notification REST endpoints |
| Real-time endpoints | `ClinicHub.API/Controllers/Version1/RealTimeController.cs` | Pusher auth + presence |
| DTO | `ClinicHub.Application/Features/Notifications/DTOs/NotificationDto.cs` | Notification list item shape |
| Deep links | `ClinicHub.Application/Common/DeepLinkRoutes.cs` | SPA routes attached to pushes |

> **Two delivery channels, one source of truth:** every notification is **persisted in the `dbo.Notifications` table** (so the dashboard can render a bell/history even if the push is missed) **and** pushed via FCM to every registered device of the user. Chat events additionally use **Pusher** for instant delivery while the recipient is online.

---

## 2. REST API — Notification Endpoints (Web Integration)

Base route prefix: `api/v{version:apiVersion}` → **`api/v1`**. Both endpoints require **JWT Bearer** auth: `Authorization: Bearer <accessToken>` (any authenticated role — the controller is `[RoleAuthorize]` without a specific role).

### 2.1 GET `api/v1/notifications/pagginated`

Fetch the logged-in user's in-app notification history, newest first.

**Query parameters:**

| Param | Type | Default | Validation |
|---|---|---|---|
| `pageNumber` | int | 1 | ≥ 1 |
| `pageSize` | int | 20 | 1 – 100 |

**Example:**
```http
GET /api/v1/notifications/pagginated?pageNumber=1&pageSize=20
Authorization: Bearer <accessToken>
Accept-Language: ar
```

**Response envelope (`ApiResponse<T>`) with `PagginatedResult<NotificationDto>` as `data`:**
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
        "userId": "0d8f...",
        "senderUserId": null,
        "titleEn": "",
        "titleAr": "تم قبول حجزك",
        "bodyEn": "",
        "bodyAr": "أكمل الدفع لتأكيد موعدك في عيادة الأمل بتاريخ 2026-08-10",
        "isRead": true,
        "clinicId": null,
        "createdAt": "2026-08-08T10:30:00Z",
        "type": 3
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

**`NotificationDto` fields** (`ClinicHub.Application/Features/Notifications/DTOs/NotificationDto.cs`):

| Field | Type | Meaning |
|---|---|---|
| `id` | Guid | Notification id |
| `userId` | Guid | Recipient user id (always the logged-in user) |
| `senderUserId` | Guid? | Sender (e.g. doctor who sent a chat message) |
| `titleEn` / `titleAr` | string | English / Arabic title |
| `bodyEn` / `bodyAr` | string | English / Arabic body |
| `isRead` | bool | Read flag |
| `clinicId` | Guid? | Related clinic (tenancy) |
| `createdAt` | DateTime | Creation time (sort by this — newest first) |
| `type` | int | `NotificationType` enum value (see §4) |

> ⚠️ **Important behavior:** calling this endpoint **marks all returned items as read** (`IsRead = true`) server-side (`GetAllNotificationsPagginatedQueryHandler.cs:41-46`). There is **no separate "mark as read" endpoint**. Design the badge flow accordingly (see §7 checklist).

### 2.2 GET `api/v1/notifications/count`

Returns the **count of unread** notifications for the logged-in user — use it for the bell badge.

**Example:**
```http
GET /api/v1/notifications/count
Authorization: Bearer <accessToken>
```

**Response:**
```json
{
  "success": true,
  "errors": null,
  "message": null,
  "statusCode": 200,
  "data": 3
}
```

> **Badge refresh strategy:** because the list endpoint marks items as read, fetch the list **first** (marking them read) and *then* refresh the count, OR keep a client-side "unread" state and call `/count` only after actions like opening the bell. There is no real-time push for `count` — pair it with FCM foreground messages (§3.4) to update the badge instantly.

### 2.3 Error handling

- `401 Unauthorized` — missing/expired/invalid token.
- `400 Bad Request` — validation errors (`pageNumber < 1`, `pageSize > 100`), returned in `errors` as `{ "PageNumber": ["..."] }` with localized messages.
- All responses use the `ApiResponse<T>` envelope: `{ success, errors, message, statusCode, data }`.

---

## 3. Receiving Push Notifications on the Web (FCM) — Correct Integration

The backend sends web pushes through **Firebase Cloud Messaging** using the legacy `firebase-admin` SDK. A web notification is delivered to the browser as a **Web Push** message and must be displayed by your service worker. Follow these steps **exactly** — most integration failures come from steps 3.2/3.3.

### 3.1 One-time Firebase setup (admin)

1. Firebase Console → your ClinicHub project → **Project settings → Service accounts** → *Generate new private key* → save as `ClinicHub.API/Firebase/firebase-credentials.json` (never commit it — it's in `.gitignore`).
2. **Project settings → General → Your apps** → *Add web app* → copy the `firebaseConfig` object.
3. **Project settings → Cloud Messaging → Web configuration → Generate key pair** → copy the **VAPID public key** (called "Web push certificate").
4. Server config is already wired in `appsettings.{env}.json` (`FirebaseSettings` section + `FirebaseSettings.Web/Android/Ios`). Nothing to change server-side.

### 3.2 Service worker — `firebase-messaging-sw.js` (REQUIRED)

Place this file at the **root of your web app** so it is served at **`/firebase-messaging-sw.js`** — the URL must be exactly this. Background pushes are only handled by this service worker:

```js
// firebase-messaging-sw.js  (serve at domain root!)
importScripts("https://www.gstatic.com/firebasejs/10.12.2/firebase-app-compat.js");
importScripts("https://www.gstatic.com/firebasejs/10.12.2/firebase-messaging-compat.js");

firebase.initializeApp({ messagingSenderId: "YOUR_FIREBASE_SENDER_ID" });

const messaging = firebase.messaging();

// Shown when the tab is closed / app in background
messaging.onBackgroundMessage((payload) => {
  const { title, body, data } = payload;
  self.registration.showNotification(title || "ClinicHub", {
    body: body || "",
    icon: "/notification_logo.png",
    data: data || {},
  });
});

// Opens the deep link supplied by the backend (payload.data.link)
self.addEventListener("notificationclick", (event) => {
  event.notification.close();
  const link = event.notification.data?.link || "/";
  event.waitUntil(clients.openWindow(link));
});
```

### 3.3 Get the token & register it at login

The web dashboard is an ASP.NET Core MVC app — it does **not** call `login-web` from the browser directly. The MVC login form posts to `AccountController.Login`, which forwards the FCM payload to the backend:

**① The login form carries two hidden fields** (`ClinicHub/Views/Account/Login.cshtml:31-32`):
```html
<input type="hidden" name="fcmToken" id="fcmToken" />
<input type="hidden" name="devicePlatform" id="devicePlatform" value="0" />
```

**② `fcm.js` fills `#fcmToken`** (`ClinicHub/wwwroot/js/fcm.js` → `handleLoginPage`) — a non-empty token is guaranteed by three mechanisms:
- **Cache pre-fill:** the last known web token is kept in `localStorage.ch_fcm_token` and written into `#fcmToken` on page load — so even a fast or Enter-key login always carries a token (the server replaces the old web token).
- **Warm-up:** on `DOMContentLoaded` the service worker is registered (needs no permission) and, when permission is already granted, `getToken(messaging, { vapidKey })` runs immediately in the background.
- **Submit wait (max 4 s):** if the form is submitted while a token is genuinely imminent (permission already granted, or the permission prompt is pending and the fetch is in flight), the login waits up to 4 s for the token before submitting — never blocks longer, and proceeds without a token if none arrives.

The permission prompt is requested on the first user interaction (`pointerdown`), as browsers require a user gesture.

**③ The MVC controller forwards it** (`ClinicHub/Controllers/AccountController.cs:66-70`):
```csharp
var result = await _authService.LoginAsync(new LoginRequest(email, password, fcmToken, devicePlatform));
```

**④ The service posts JSON to the backend** (`ClinicHub.Services/Services/Implementations/AuthService.cs:47-52` → `POST {BaseUrl}/api/v1/auth/login-web`, camelCase, enums serialized as numbers):
```json
{ "email": "...", "password": "...", "fcmToken": "dGVzdC0xMjM...", "devicePlatform": 0 }
```
`devicePlatform` is serialized as the **number `0`** (= Web) by Newtonsoft — never the string `"0"`.

Server-side gate (`LoginWebCommandHandler.cs:117-118`):
```csharp
if (!string.IsNullOrEmpty(request.FcmToken) && request.DevicePlatform.HasValue)
    await _fcmService.RegisterTokenAsync(user.Id, request.FcmToken, request.DevicePlatform.Value);
```

**Token lifecycle rules (server behavior):**
- **One active token per user per platform** — re-login with a new token replaces the old web token.
- A token already owned by another user is transferred to the new owner.
- If FCM reports a token as `Unregistered` / `SenderIdMismatch`, the server deletes it automatically — the browser must re-register on next login.
- `devicePlatform` values: `0 = Web`, `1 = Android`, `2 = iOS`.
- Push registration happens on **all** auth flows: `login-web`, `login`, `signup`, `login-facebook`, `login-google`.
- Login still succeeds without a token — push just stays off.

### 3.4 What the backend actually sends to web browsers

Each message has a `notification` part (shown by the service worker) and a `data` part (available in `event.notification.data` / `onMessage(payload.data)`):

```
notification: { title, body }                          ← Arabic, hardcoded server-side
data: {
  "type":  "PaymentConfirmation",                      ← NotificationType name
  "link":  "https://your-frontend/appointments/3fa8…", ← deep link ("" if none)
  ...type-specific keys (clinicName, senderName, amount, date, time,
       reason, message, conversationId, appointmentId, paymentUrl)
}
```

- When the `link` is a valid absolute URL, the server attaches it as **`fcmOptions.link`** — the browser will navigate there when the user clicks the system notification (in Chrome; for full control handle `notificationclick` in the SW as in §3.2).
- Web pushes only arrive when the token is a **web** token (`UserFbToken.DevicePlatform = 0`) — check this in the DB when debugging.

### 3.5 Foreground handling (dashboard is open)

```js
onMessage(messaging, (payload) => {
  // payload.data = { type, link, clinicName, appointmentId, ... }
  showToast(payload.notification?.title, payload.notification?.body);
  if (payload.data?.link) navigateTo(payload.data.link); // or SPA router.push()
});
```

**Recommended dashboard flow on every received push:**
1. Show a toast/snackbar (foreground) — the SW already handled background.
2. Navigate using `data.link` (or ignore — design choice).
3. Refresh the bell: `GET /api/v1/notifications/count` then `GET /api/v1/notifications/pagginated?pageNumber=1&pageSize=20`.
4. Debounce — a burst of pushes (e.g. hourly Hangfire sweeps) may arrive together.

### 3.6 Token refresh & re-registration

Browsers rotate FCM tokens over time and after service-worker updates. In this dashboard:

- **At login** the form always sends a token (the `localStorage.ch_fcm_token` cache, refreshed by the background fetch) — the server replaces the old web token for the platform.
- **On dashboard pages** `fcm.js` (`handleForeground`) re-reads the token once on load when permission is granted and, if it changed, updates `localStorage.ch_fcm_token` — so the **next** login registers the current token.
- If push delivery stops while the SW looks fine, the fastest re-registration is logging out and back in — the login request re-sends the cached token to `POST /api/v1/auth/login-web`.

---

## 4. Notification Types & Push Scenarios (Project Analysis)

### 4.1 The enum — `ClinicHub.Domain/Enums/NotificationType.cs`

| Value | Name | Arabic title (hardcoded) | Used? |
|---|---|---|---|
| 0 | `AppointmentReminder` | تذكير بالموعد | ⚠️ Wired in builder, **no code triggers it yet** |
| 1 | `NewMessage` | رسالة جديدة | ✅ Chat message |
| 2 | `PaymentConfirmation` | تم تأكيد الدفع | ✅ Paymob webhook |
| 3 | `AppointmentConfirmation` | تم قبول حجزك | ✅ Booking accepted |
| 4 | `AppointmentCancellation` | تم إلغاء الموعد | ✅ 6 different triggers |
| 5 | `SystemAnnouncement` | إشعار | ⚠️ Wired in builder, **no code triggers it yet** |
| 6 | `CancellationWindowClosed` | انتهت مهلة الإلغاء | ✅ Scheduled job |
| 7 | `SubscriptionExpiring` | اشتراكك على وشك الانتهاء | ✅ Daily job |
| 8 | `RefundProcessed` | تم رد المبلغ | ✅ Refund job |
| 9 | `AdExpiring` | إعلانك على وشك الانتهاء | ✅ Daily job |
| 10 | `AppointmentOutsideAvailability` | موعد خارج مواعيد الطبيب | ✅ Hourly validation job |
| 11 | `AppointmentOutsideWorkingHours` | موعد خارج ساعات عمل العيادة | ✅ Hourly validation job |

### 4.2 How a notification is created (single flow)

Every scenario below calls the same pipeline:

```
Handler/job → IFcmService.SendToUserAsync(userId, type, params)
  → NotificationBuilderService.BuildAsync   → persists dbo.Notifications row
                                            → builds Arabic title/body + data payload + deep link
  → FcmService.SendToUserAsync              → sends one FCM message per registered device token
```

### 4.3 Patient-facing scenarios (delivered to the patient web dashboard / mobile)

| # | Scenario | Type (value) | Recipient | Title / Body | `data` keys | Deep link | Code location |
|---|---|---|---|---|---|---|---|
| 1 | **Clinic/doctor accepts a booking** — patient must complete payment | `AppointmentConfirmation` (3) | Patient | تم قبول حجزك / أكمل الدفع لتأكيد موعدك في {clinicName} بتاريخ {date} | `clinicName, date, appointmentId, paymentUrl` | `/appointments/{appointmentId}` | `ClinicHub.Application/Common/Services/AppointmentAcceptanceService.cs:84-90` |
| 2 | **Payment confirmed** — Paymob webhook marks TRX successful | `PaymentConfirmation` (2) | Patient | تم تأكيد الدفع / تم تأكيد دفعتك بقيمة {amount} | `amount, appointmentId` | `/appointments/{appointmentId}` | `Features/Payment/Commands/ConfirmPaymentWebhook/ConfirmPaymentWebhookCommandHandler.cs:87-91` |
| 3 | **New chat message** | `NewMessage` (1) | Chat recipient | رسالة جديدة / رسالة جديدة من {senderName} | `senderName, conversationId` | `/chat/{conversationId}` | `Features/Conversations/Commands/SendMessage/SendMessageCommandHandler.cs:146-151` |
| 4 | **User cancels own appointment** | `AppointmentCancellation` (4) | Clinic owner/doctor | تم إلغاء الموعد / تم إلغاء موعدك في {clinicName}: {reason} | `clinicName, reason` | `/appointments` | `Features/Appointments/Commands/CancelAppointment/CancelAppointmentCommandHandler.cs:149-153` |
| 5 | **Doctor rejects appointment** | `AppointmentCancellation` (4) | Patient | same as #4 | `clinicName, date, reason` | `/appointments` | `Features/DoctorDashboard/Commands/DoctorRejectAppointment/DoctorRejectAppointmentCommandHandler.cs:43-48` |
| 6 | **Staff rejects appointment** | `AppointmentCancellation` (4) | Patient | same as #4 | `clinicName, date, reason` | `/appointments` | `Features/StaffDashboard/Commands/StaffRejectAppointment/StaffRejectAppointmentCommandHandler.cs:48-53` |
| 7 | **Doctor sets status = Cancelled** (legacy status 2) | `AppointmentCancellation` (4) | Patient | same as #4 | `clinicName, date, reason` | `/appointments` | `Features/DoctorDashboard/Commands/UpdateAppointmentStatus/UpdateAppointmentStatusCommandHandler.cs:64-69` |
| 8 | **Reservation TTL expired** — Hangfire hourly sweep | `AppointmentCancellation` (4) | Patient | same as #4 | `clinicName, reason` | `/appointments` | `Infrastructure/Services/BackgroundJobs/ReservationExpirationJob.cs:74-78` |
| 9 | **Abandoned checkout > 24h** — Hangfire hourly sweep | `AppointmentCancellation` (4) | Patient | same as #4 | `clinicName, reason` | `/appointments` | `Infrastructure/Services/BackgroundJobs/AbandonedPaymentJob.cs:63-67` |
| 10 | **Cancellation/refund window closed** — scheduled after payment | `CancellationWindowClosed` (6) | Patient | انتهت مهلة الإلغاء / انتهت مهلة الإلغاء والاسترداد لموعدك في {clinicName} | `clinicName, appointmentId` | `/appointments/{appointmentId}` | `Infrastructure/Services/BackgroundJobs/CancellationWindowJob.cs:36-40` |
| 11 | **Refund succeeded** — Hangfire retry job | `RefundProcessed` (8) | Patient | تم رد المبلغ / تم إرجاع مبلغ {amount} الخاص بحجزك في {clinicName} | `clinicName, amount, appointmentId` | `/appointments/{appointmentId}` | `Infrastructure/Services/BackgroundJobs/RefundRetryJob.cs:67-72` |

### 4.4 Clinic-facing scenarios (delivered to clinic admin / doctor dashboard)

| # | Scenario | Type (value) | Recipient | Title / Body | `data` keys | Deep link | Code location |
|---|---|---|---|---|---|---|---|
| 12 | **Subscription expiring** — daily job, 3-day & 1-day windows | `SubscriptionExpiring` (7) | Clinic owner | اشتراكك على وشك الانتهاء / اشتراك عيادة {clinicName} ينتهي في {date} — جدد الآن | `clinicName, date, period` | `/notifications` | `Infrastructure/Services/BackgroundJobs/ExpiryReminderJob.cs:43-44, 49-50, 103-108` |
| 13 | **Ad expiring** — daily job, 3-day & 1-day windows | `AdExpiring` (9) | Clinic owner | إعلانك على وشك الانتهاء / إعلان عيادة {clinicName} ينتهي في {date} — جدد الآن | `clinicName, date, period` | `/notifications` | `ExpiryReminderJob.cs:66-67, 72-73` |
| 14 | **Appointment outside doctor availability** — hourly validation job | `AppointmentOutsideAvailability` (10) | **Doctor + clinic admin** | موعد خارج مواعيد الطبيب / موعد {clinicName} بتاريخ {date} الساعة {time} خارج فترات توفر الطبيب — يرجى المراجعة | `clinicName, date, time, appointmentId` | `/appointments/{appointmentId}` | `Infrastructure/Services/BackgroundJobs/DoctorAvailabilityValidationJob.cs:92-98 (recipients), 107-113` |
| 15 | **Appointment outside clinic working hours** — hourly validation job | `AppointmentOutsideWorkingHours` (11) | **Clinic admin** | موعد خارج ساعات عمل العيادة / موعد {clinicName} بتاريخ {date} الساعة {time} خارج ساعات عمل العيادة — يرجى المراجعة | `clinicName, date, time, appointmentId` | `/appointments/{appointmentId}` | `Infrastructure/Services/BackgroundJobs/ClinicWorkingHoursValidationJob.cs:62-68` |

### 4.5 Summary: what the web dashboard should expect

| Receiver | Types they receive |
|---|---|
| **Patient** | `AppointmentConfirmation` (needs to pay → `paymentUrl`), `PaymentConfirmation`, `NewMessage`, `AppointmentCancellation`, `CancellationWindowClosed`, `RefundProcessed` |
| **Clinic owner** | `AppointmentCancellation` (patient cancelled), `SubscriptionExpiring`, `AdExpiring`, `AppointmentOutsideWorkingHours`, `AppointmentOutsideAvailability` |
| **Doctor** | `AppointmentCancellation` (in some flows), `NewMessage`, `AppointmentOutsideAvailability` |
| **Everyone** | `NewMessage` (chat) |

**Background-job cadence** (registered in `ClinicHub.API/Program.cs:277-301`, dashboard at `/hangfire`):
- **Hourly:** subscriptions-expiration, ads-expiration, abandoned-payments, reservations-expiration, doctor-availability-validation, clinic-working-hours-validation.
- **Daily:** token-cleanup, expiry-reminders (subscription + ads).
- **One-time scheduled:** cancellation-window (after each payment), refund retries.

> The dashboard may therefore receive **bursts of pushes** when hourly/daily jobs sweep — debounce your badge refresh.

### 4.6 Deep links (where pushes point in your SPA)

`ClinicHub.Application/Common/DeepLinkRoutes.cs` + `IDeepLinkService` (base = `EmailSettings.FrontendUrl`, e.g. `https://doctory.runasp.net`):

| Route | Value |
|---|---|
| `Appointments` | `/appointments` |
| `AppointmentDetails` | `/appointments/{appointmentId}` |
| `Chat` | `/chat/{conversationId}` |
| `Notifications` | `/notifications` |

---

## 5. Real-Time Events (Pusher) — Chat & Presence

Pusher powers **instant chat** events while users are online (FCM covers offline). The dashboard should subscribe to its own private channel.

**Channel:** `private-user-{userId}` where `{userId}` = the logged-in user's GUID from the JWT.

**Events the server may emit on your channel:**

| Event | Payload | Meaning |
|---|---|---|
| `new-message` | `MessageDto` | A message arrived in a conversation you belong to |
| `conversation-updated` | `{ conversationId, lastMessage, lastMessageDate }` | Conversation list refresh |
| `messages-read` | `{ conversationId }` | Recipient read the conversation |
| `messages-delivered` | `{ conversationId }` | Recipient received your message (online) |
| `typing` | `{ conversationId, userId, isTyping }` | Typing indicator |

**Pusher client setup (dashboard):**

```js
import Pusher from "pusher-js";

const socketId = new Pusher("APP_KEY", { cluster: "eu" });

// Subscribe to your own channel
const channel = socketId.subscribe(`private-user-${currentUserId}`);

// Auth is delegated to the backend:
socketId.config.authEndpoint = "https://your-api/api/v1/realtime/auth";
socketId.config.auth = {
  headers: { Authorization: `Bearer ${accessToken}` },
};

channel.bind("new-message", (message) => { /* update chat UI */ });
channel.bind("typing", ({ conversationId, userId, isTyping }) => { /* show typing */ });
```

**Real-time API endpoints** (`RealTimeController`, all JWT-protected except the webhook):

| Method | Route | Purpose |
|---|---|---|
| POST | `api/v1/realtime/auth` | Pusher channel auth — sends `socket_id` + `channel_name` **form-encoded** with the Bearer token; returns Pusher's auth JSON |
| POST | `api/v1/realtime/webhook` | `[AllowAnonymous]` — Pusher presence webhook (configure it in the Pusher dashboard) for online/offline cleanup |
| POST | `api/v1/realtime/typing` | Body `{ conversationId, isTyping }` — broadcast typing |
| GET | `api/v1/realtime/typing/{conversationId:guid}` | List of users typing in a conversation |
| GET | `api/v1/realtime/online-users` | Online user ids |
| POST | `api/v1/realtime/connect` / `disconnect` | Body `{ connectionId }` — register/unregister a socket |

> Note: the Pusher AppId/AppKey/AppSecret/Cluster come from `PusherSettings` in `appsettings.{env}.json` (cluster `eu` in Development). Presence data (online users, typing) is stored **in-memory** — it resets on restart and is not reliable across multiple API instances.

---

## 6. Web Dashboard Integration Checklist

### A. Push notifications (FCM) — implemented in this repo
- [x] Firebase web app created; VAPID key generated (Console → Project settings → Cloud Messaging) → `appsettings.*.json` `FirebaseWeb` section.
- [x] `firebase-messaging-sw.js` served at **`/firebase-messaging-sw.js`** (domain root) — `wwwroot/firebase-messaging-sw.js`, with correct `messagingSenderId` + `onBackgroundMessage` + `notificationclick` → opens `data.link`.
- [x] `getToken(messaging, { vapidKey })` called only after permission granted (prompt requested on first `pointerdown`), **on HTTPS**.
- [x] Token sent at **every login**: hidden `#fcmToken` input (`Login.cshtml`) → `AccountController.Login` → `AuthService.LoginAsync` → `POST /api/v1/auth/login-web` with `fcmToken` **non-empty string** + `devicePlatform` **number 0**. Guaranteed by the `localStorage.ch_fcm_token` cache + SW warm-up + 4 s submit wait (§3.3). Verify in DevTools → Network that both fields are in the payload.
- [x] `onMessage` foreground handler → success modal + navigate by type + bell refresh (`fcm.js` → `handleForeground`).
- [x] Token refresh: dashboard pages re-read the token and update the cache for the next login (§3.6).

### B. In-app bell — implemented in this repo
- [x] On dashboard load: `GET /api/v1/notifications/count` → badge, then `GET /api/v1/notifications/pagginated?pageNumber=1&pageSize=20` → list (`fcm.js` `handleForeground` → `injectBell` / `refreshBell` / `loadBellList`; pages at `Views/{Admin,Clinic,Doctor,Staff}/Notifications.cshtml`).
- [x] **Order matters:** the list endpoint marks items as read server-side — the bell fetches the count first (badge) and the list only when the dropdown opens.
- [x] Infinite scroll: increment `pageNumber` until `hasNextPage = false` (pageSize max 100) — not yet paginated in the bell dropdown (shows latest 10; full history on the Notifications page with prev/next).
- [x] Display `titleAr`/`bodyAr` (Arabic) — `titleEn`/`bodyEn` are currently stored empty.
- [x] Render deep link from `data.link` / navigate by `type` when clicked (`navigateByType` → role appointments page; notifications page items navigate the same way).

### C. Real-time chat (optional, if the dashboard embeds chat)
- [ ] Pusher JS client on `private-user-{userId}` with backend `authEndpoint` + Bearer header.
- [ ] Handle `new-message`, `conversation-updated`, `messages-read`, `messages-delivered`, `typing`.

---

## 7. Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| Login succeeds but no `UserFbToken` row (`DevicePlatform = 0`) | Web token wasn't in the login request. Check: (a) first visit — the permission prompt was still unanswered when the form was submitted (now mitigated by the 4 s submit wait + token cache, §3.3); (b) Notification permission blocked in the browser — check `chrome://settings/content/notifications`; (c) `getToken()` failed — read the `[FCM]` warnings in DevTools console; (d) the `login-web` payload in DevTools → Network must contain `fcmToken` (non-empty) + `devicePlatform: 0` (number, not string) |
| `getToken()` rejects `messaging/invalid-vapid-key` | Wrong VAPID key — copy from Project settings → Cloud Messaging → Web push certificates |
| `getToken()` rejects `messaging/sender-id-mismatch` | `messagingSenderId` in `firebaseConfig`/SW ≠ project number |
| `getToken()` rejects `messaging/registration-token-not-issued` | `firebase-messaging-sw.js` not served from **domain root** or SW has errors (Application → Service Workers) |
| Push works on Android/iOS but not web | No web token registered (`UserFbToken` with `DevicePlatform = 0`) |
| Push never arrives while tab is open | SW only shows background pushes; add `onMessage` (§3.5) for foreground |
| Clicking push doesn't navigate | `data.link` empty (server sends "" when no deep link) — handle in `notificationclick` with a fallback route |
| Token deleted unexpectedly | FCM reported it `Unregistered` — re-register at next login |
| DB row exists but no push | FCM dispatch failure is swallowed by design — check Serilog for `FirebaseMessagingException` |
| Badge count wrong | `/count` = unread; list endpoint marks read — re-check fetch ordering (§6.B) |
| `FirebaseApp` creation throws at startup | `firebase-credentials.json` missing or wrong `CredentialsFilePath` |

---

## 8. Current Behavior & Known Notes

- **Frontend web integration (this repo):** login flow is `Login.cshtml` hidden fields (`fcmToken` + `devicePlatform=0`) → `AccountController.Login` → `AuthService.LoginAsync` → `POST /api/v1/auth/login-web`. `wwwroot/js/fcm.js` guarantees a token is attached to every login via the `localStorage.ch_fcm_token` cache + service-worker warm-up + a 4 s submit wait, and refreshes the cached token on dashboard pages (token rotation). The bell (`handleForeground`) polls `GET /api/v1/notifications/count` every 60 s and refreshes the badge on each foreground push.
- **Pusher (real-time chat)** is **not** consumed by this dashboard yet — the `pusher-js` client setup in §5 is for future work.
- **Localization:** push titles/bodies are **Arabic-only**, hardcoded in `NotificationBuilderService.cs`. The `Notifications` table stores `TitleEn/TitleAr`/`BodyEn/BodyAr`, but English columns are written as **empty strings** today. `messages.{en,ar}.json` resource files contain **no** notification keys.
- **`AppointmentReminder` (0) and `SystemAnnouncement` (5)** are fully wired in the builder but **no code path triggers them yet** — they are ready for future features (e.g. appointment reminders, admin broadcasts).
- **FCM dispatch is fire-and-forget:** failures are logged/ignored so business logic (booking, payment) never blocks on push.
- **Webpush config:** `fcmOptions.link` is set only when the deep link is a valid absolute URL; the web icon (`FirebaseSettings.Web.Icon`) is currently commented out in `FcmService.cs:136,146`.
- **Cancellation reasons from background jobs** (ReservationExpirationJob, AbandonedPaymentJob) are stored with mojibake (corrupted Arabic encoding) — cosmetic issue in the persisted body.
- **Presence (online/typing) is in-memory** (`ChatConnectionManager`, singleton) — resets on restart, not multi-instance safe.
- **DB schema:** `dbo.Notifications` (Id, UserId, SenderUserId?, ClinicId?, TitleEn, TitleAr, BodyEn, BodyAr, Type, IsRead, audit columns) and `dbo.UserFbTokens` (UserId, Token ≤500 chars unique-when-active, DevicePlatform, audit columns). Migrations in `ClinicHub.Persistence/Migrations/`.
- **Security note:** Pusher keys and Firebase project config exist in `appsettings.{env}.json` — treat them as non-secret app config; the **service-account private key** (`Firebase/firebase-credentials.json`) must never be committed or exposed.
