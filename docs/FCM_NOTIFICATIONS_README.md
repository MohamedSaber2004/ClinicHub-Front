# 🔔 FCM Notifications — Logic, Flow & Web Dashboard Setup

This document explains how **Firebase Cloud Messaging (FCM)** and in-app notifications work in ClinicHub, and exactly what the **website dashboard** must implement to receive and display them.

---

## 1️⃣ Architecture Overview

```
Business event                    ┌────────────────────────────────────────────┐
(booking, payment, refund,        │           ClinicHub API (server)           │
job, chat, expiry ...)            │                                            │
        │                         │  Handler calls:                            │
        ▼                         │  _fcmService.SendToUserAsync(userId,       │
┌─────────────────────┐           │                            type, params)   │
│ MediatR handler /   │ ────────► │        │                                   │
│ Hangfire background │           │        ▼                                   │
└─────────────────────┘           │  NotificationBuilderService.BuildAsync     │
                                  │  • resolves Arabic title + body (switch)   │
                                  │  • builds data payload (type, ids, link)   │
                                  │  • saves a row in the `Notifications`      │
                                  │    table (in-app history)                  │
                                  │        │                                   │
                                  │        ▼                                   │
                                  │  FcmService.SendToUserAsync                │
                                  │  • loads all registered device tokens      │
                                  │    of the user (UserFbToken table)         │
                                  │  • sends one FCM message per device        │
                                  │    (Android / iOS / Webpush configs)       │
                                  │  • deletes tokens FCM says are Unregistered│
                                  │        │                                   │
                                  └────────┼───────────────────────────────────┘
                                           ▼
                                  ┌──────────────────┐
                                  │ Firebase Cloud   │  (legacy/native API via
                                  │ Messaging        │   firebase-admin SDK)
                                  └──────────────────┘
                                    │          │           │
                                    ▼          ▼           ▼
                                 Android     iOS        Web dashboard
                                (FCM SDK)  (APNs)   (firebase-messaging JS)
```

**Key files:**
| File | Role |
|---|---|
| `ClinicHub.Application/Common/Interfaces/IFcmService.cs` | Service contract |
| `ClinicHub.Infrastructure/Services/FcmService.cs` | Firebase init + send/register/unregister tokens |
| `ClinicHub.Infrastructure/Services/NotificationBuilderService.cs` | Arabic title/body + payload + in-app DB record |
| `ClinicHub.Application/Common/Options/FirebaseSettings.cs` | Config model |
| `ClinicHub.Domain/Entities/UserFbToken.cs` | Device tokens per user |
| `ClinicHub.Domain/Enums/DevicePlatform.cs` | `Web=0`, `Android=1`, `iOS=2` |
| `ClinicHub.Domain/Enums/NotificationType.cs` | All notification types (0–9) |

---

## 2️⃣ One-Time Firebase Setup (Admin)

### 2.1 Firebase project
1. Go to [Firebase Console](https://console.firebase.google.com) → **Add project** (or use the existing ClinicHub project).
2. **Project settings → Service accounts → Generate new private key** → download `firebase-credentials.json`.
3. Add a **Web app** to the project → copy the `firebaseConfig` object and the **Web push certificate / VAPID key** (Project settings → Cloud Messaging → Web configuration → "Generate key pair").

### 2.2 Place credentials in the API
```
ClinicHub.API/Firebase/firebase-credentials.json
```
The file path is read from `appsettings.{env}.json`:
```json
"FirebaseSettings": {
  "CredentialsFilePath": "Firebase/firebase-credentials.json",
  "Web":     { "Sound": "default", "ClickAction": "/",  "Icon": "/notification_logo.png", "Link": "/" },
  "Android": { "Sound": "default", "ChannelId": "clinic_hub_default", "Badge": 1, "ClickAction": ".MainActivity", "Icon": "notification_logo", "ImageUrl": "https://yourdomain/notification_logo.png" },
  "Ios":     { "Sound": "default", "Badge": 1, "Category": "clinic_hub" }
}
```
- `FirebaseSettings` exists in **all 3** appsettings (Development / Test / Production).
- The service looks for the file relative to the app's base directory, then falls back to the absolute path if not found.
- **Important:** `firebase-credentials.json` contains a private key — never commit it (keep it in `.gitignore`).

### 2.3 Runtime init (already implemented — nothing to do)
On the first `FcmService` creation, the app initializes Firebase once (thread-safe, double-checked lock):
```csharp
FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromFile(credentialPath) });
```

---

## 3️⃣ What the Server Sends — Notification Types & Triggers

| Type | Value | Triggered by | Arabic title | Deep link |
|---|---|---|---|---|
| `AppointmentReminder` | 0 | Appointment reminder | تذكير بالموعد | `AppointmentDetails/{id}` |
| `NewMessage` | 1 | New chat message | رسالة جديدة | `Chat/{conversationId}` |
| `PaymentConfirmation` | 2 | Payment confirmed (Paymob webhook) | تم تأكيد الدفع | `AppointmentDetails/{id}` |
| `AppointmentConfirmation` | 3 | Clinic accepted a booking | تم قبول حجزك | `AppointmentDetails/{id}` |
| `AppointmentCancellation` | 4 | Cancelled / reservation expired (Hangfire jobs) | تم إلغاء الموعد | `Appointments` |
| `SystemAnnouncement` | 5 | Admin/system messages | إشعار | `Notifications` |
| `CancellationWindowClosed` | 6 | Hangfire `CancellationWindowJob` | انتهت مهلة الإلغاء | `AppointmentDetails/{id}` |
| `SubscriptionExpiring` | 7 | Hangfire `ExpiryReminderJob` (3d / 1d) | اشتراكك على وشك الانتهاء | `Notifications` |
| `RefundProcessed` | 8 | Hangfire `RefundRetryJob` success | تم رد المبلغ | `AppointmentDetails/{id}` |
| `AdExpiring` | 9 | Hangfire `ExpiryReminderJob` (3d / 1d) | إعلانك على وشك الانتهاء | `Notifications` |

**Payload data keys** every message carries:
```
type          → NotificationType name (e.g. "PaymentConfirmation")
link          → deep link URL ("" if none) — used by webpush FCM options
clinicName / senderName / amount / date / time / reason / message /
conversationId / appointmentId / paymentUrl → only for the types that use them
```

Each notification is also **persisted in the `Notifications` table**, so the dashboard can render an in-app bell/history even when the push is missed.

---

## 4️⃣ Device Token Lifecycle

- **Register** — automatically done by the API during **every login/signup path** — token registration is implemented in these handlers:
  - `LoginWebCommandHandler` (dashboard login) — line 117
  - `LoginCommandHandler` (mobile login) — line 115
  - `SignupCommandHandler` — line 63
  - `LoginWithFacebookCommandHandler` — line 123
  - `LoginWithGoogleCommandHandler` — line 121
  - The request DTO accepts:
  ```json
  { "email": "user@x.com", "password": "…", "fcmToken": "…", "devicePlatform": 0 }
  ```
  `devicePlatform`: `0=Web`, `1=Android`, `2=iOS`. Registration runs **only if both `fcmToken` and `devicePlatform` are sent** (both are optional).
- **One token per user per platform** — registering a new token deletes the old one of the same platform; a token already used by another user is moved to the new owner.
- **Auto-cleanup** — if FCM answers with `Unregistered` or `SenderIdMismatch`, the API deletes the token automatically.
- **Dispatch failures never block business logic** — FCM errors are logged/ignored so e.g. a booking is saved even if push fails.

---

## 5️⃣ Web Dashboard Integration (what YOU must implement)

### 5.1 Firebase config + VAPID key
Add to your frontend build (Vite/React/Next…):
```js
// firebaseConfig from Firebase Console → Project settings → General → Your apps
import { initializeApp } from "firebase/app";
import { getMessaging, getToken, onMessage } from "firebase/messaging";

const firebaseConfig = {
  apiKey: "AIza…",
  authDomain: "…firebaseapp.com",
  projectId: "…",
  storageBucket: "…",
  messagingSenderId: "…",
  appId: "1:…",
  // Only needed for the legacy JS SDK — modern SDK reads VAPID via getToken()
};
const app = initializeApp(firebaseConfig);
const messaging = getMessaging(app);
```
Get the **VAPID key** from Firebase Console → Project settings → **Cloud Messaging** → Web Push certificates.

### 5.2 Service worker file — REQUIRED for web push
Place this file at the **root of your web app** (served at `/firebase-messaging-sw.js` — the URL matters):
```js
// firebase-messaging-sw.js
importScripts("https://www.gstatic.com/firebasejs/10.12.2/firebase-app-compat.js");
importScripts("https://www.gstatic.com/firebasejs/10.12.2/firebase-messaging-compat.js");

firebase.initializeApp({ messagingSenderId: "YOUR_SENDER_ID" });

const messaging = firebase.messaging();

// Background message handler: shows the notification + opens the deep link
messaging.onBackgroundMessage((payload) => {
  const { title, body, icon, data } = payload;
  const notificationTitle = title || "ClinicHub";
  const notificationOptions = {
    body: body || "",
    icon: icon || "/notification_logo.png",
    data: data || {},
    click_action: data?.link || "/",
  };
  self.registration.showNotification(notificationTitle, notificationOptions);
});

self.addEventListener("notificationclick", (event) => {
  event.notification.close();
  const link = event.notification.data?.link || "/";
  event.waitUntil(clients.openWindow(link));
});
```

### 5.3 Register the service worker + request permission + get token
```js
export async function getFcmToken() {
  if (!("serviceWorker" in navigator) || !("Notification" in window)) return null;

  const permission = await Notification.requestPermission();
  if (permission !== "granted") return null;

  await navigator.serviceWorker.register("/firebase-messaging-sw.js");
  return getToken(messaging, { vapidKey: "YOUR_VAPID_PUBLIC_KEY" });
}
```

### 5.3.1 Full login flow (complete frontend example with every failure logged)

This is the **exact pattern the dashboard must implement** — note that the login still succeeds even when push is unavailable; the token fields are simply omitted:

```js
async function loginWeb(email, password) {
  let fcmToken = null;

  try {
    // 1. Get a web FCM token BEFORE calling the API
    //    Returns null if: no service worker support, permission denied,
    //    getToken() threw (bad VAPID key / bad firebaseConfig / wrong senderId)
    fcmToken = await getFcmToken();
    if (fcmToken) {
      console.log("[FCM] token obtained, will register at login:", fcmToken.slice(0, 30) + "...");
    } else {
      console.warn("[FCM] no token — login will still work but push will be OFF");
    }
  } catch (err) {
    console.error("[FCM] getToken() threw:", err);   // ← shows the ROOT CAUSE here
  }

  // 2. Send BOTH fields (fcmToken + devicePlatform=0) only if the token exists
  const body = { email, password };
  if (fcmToken) {
    body.fcmToken = fcmToken;        // must be the real token string, NOT empty ""
    body.devicePlatform = 0;         // MUST be a number 0 (Web), never "0" as string
  }

  const res = await fetch("https://your-api/api/v1/auth/login-web", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  if (res.status === 200) {
    const json = await res.json();
    console.log("[FCM] login OK — check DB row after this (see §6)");
    return json.data;
  }
  throw new Error(`login failed: ${res.status}`);
}
```

**Golden rule:** the server registers the token **only if** `fcmToken` is a non-empty string **AND** `devicePlatform` is a number. If the login request payload is `{"email":…, "password":…}` only, **no record is ever created** — by design, and silently (the handler logs nothing).
### 5.4 Send the token to the API at every login

**A) Website dashboard login — `POST /api/v1/auth/login-web` (recommended for the dashboard)**

The web dashboard has its own login endpoint. Its request DTO (`LoginWebCommand`) already supports FCM — same shape as the mobile login:
```json
POST /api/v1/auth/login-web
{
  "email": "user@x.com",
  "password": "…",
  "fcmToken": "<token from getFcmToken()>",
  "devicePlatform": 0
}
```
`LoginWebCommandHandler` (line 117–118) registers the token **only when BOTH fields are provided**:
```csharp
if (!string.IsNullOrEmpty(request.FcmToken) && request.DevicePlatform.HasValue)
    await _fcmService.RegisterTokenAsync(user.Id, request.FcmToken, request.DevicePlatform.Value);
```
So if the browser already granted permission on a previous visit, include the existing token on every login — the API replaces the old web token with the new one (one token per user per platform, see §4).

**B) Mobile / other logins** — `POST /api/v1/auth/login`, `/api/v1/auth/signup`, `/api/v1/auth/login-facebook`, `/api/v1/auth/login-google` accept the same two optional fields and register the token the same way.

**C) Facebook login specifics** — `POST /api/v1/auth/login-facebook`:
```json
{
  "accessToken": "<facebook-access-token>",
  "fcmToken": "<token>",
  "devicePlatform": 0
}
```
- `LoginWithFacebookCommandValidator` validates only `accessToken` (must be non-empty, localized error `FacebookTokenRequired`).
- `fcmToken` / `devicePlatform` are **optional and unvalidated** — if present, `LoginWithFacebookCommandHandler` (line 123–124) registers them after successful authentication.

**Token refresh:** browsers rotate tokens periodically — re-call the login endpoint whenever you detect a new token.

### 5.5 Handle foreground messages + render in-app list
```js
onMessage(messaging, (payload) => {
  // Notification arrived while the dashboard is open
  // payload.data = { type, link, appointmentId, ... }
  showToast(payload.notification?.title, payload.notification?.body);
  if (payload.data?.link) location.href = payload.data.link;   // or SPA router push
});
```

**In-app history** (persisted server-side) — fetch on dashboard load and after each new push:
```
GET  /api/v1/notifications/pagginated?pageNumber=1&pageSize=20   → list (JWT auth required)
GET  /api/v1/notifications/count                                 → unread/read count for the badge
```
> Route prefix is `api/v1` in the base route constants; both endpoints are `[RoleAuthorize]` (any authenticated user).

### 5.6 Frontend summary checklist
- [ ] Firebase web app created, VAPID key generated (Project settings → Cloud Messaging → Web push certificates)
- [ ] `firebase-messaging-sw.js` served from **domain root** (`/firebase-messaging-sw.js`) with correct `messagingSenderId`
- [ ] Permission requested + token fetched with VAPID key (test manually in console first)
- [ ] Token sent at login (`POST /api/v1/auth/login-web` with `fcmToken` **non-empty** + `devicePlatform` as **number 0**)
- [ ] Verify in Network tab that the payload actually contains both fields
- [ ] `onMessage` handler for foreground pushes (toast + navigate by `data.link`)
- [ ] Notification bell reads `/notifications/pagginated` + `/notifications/count`
- [ ] Token refreshed after browser rotation (re-login or refresh endpoint)
- [ ] Confirm DB row: `SELECT * FROM UserFbTokens WHERE UserId='…'` → `DevicePlatform = 0`

---

## 6️⃣ Root-Cause Diagnosis — "No FCM record created after web login"

The server code is correct; registration is **100% driven by the request payload**. If no `UserFbToken` row appears, one of the two conditions in `LoginWebCommandHandler.cs:117` was false:

```csharp
if (!string.IsNullOrEmpty(request.FcmToken) && request.DevicePlatform.HasValue)
```

Follow this decision tree top-to-bottom:

### Step 1 — Did the login actually succeed? (HTTP 200)
The registration line runs **only after** authentication passes (password check, `IsDeleted`, `IsActive`, pending verification). A `401/400/403` response means the handler threw before reaching the token code.
- ✅ Check the response status of `POST /api/v1/auth/login-web` in DevTools → Network.

### Step 2 — Inspect the exact request payload (DevTools → Network → login-web request → Payload)
| What you see in the payload | Conclusion |
|---|---|
| `fcmToken: "…"` **and** `devicePlatform: 0` both present | Server should have registered it → go to **Step 4** |
| `fcmToken` missing, `devicePlatform` missing (payload has only email/password) | **Root cause: frontend didn't include them** → go to **Step 3** |
| `fcmToken: ""` (empty string) | `IsNullOrEmpty` → skipped. Fix: only add the field when you have a real token |
| `devicePlatform: "0"` (string) | Enum binding fails → `HasValue` false → skipped. Fix: send number `0`, not string |
| `devicePlatform: null` | Skipped. Fix: send `0` (Web) |

### Step 3 — Why is there no token? (browser console + application tab)
Run `getFcmToken()` manually in the console and read the **exact error** it throws:

| Console error / behavior | Root cause | Fix |
|---|---|---|
| `getToken` rejects with "messaging/invalid-vapid-key" | VAPID key missing/wrong in `getToken(messaging, { vapidKey })` | Copy the **Web Push certificate** from Firebase Console → Project settings → Cloud Messaging |
| `getToken` rejects with "messaging/sender-id-mismatch" | `messagingSenderId` in `firebaseConfig`/SW doesn't match the Firebase project | Fix `messagingSenderId` to the project number (Project settings → General) |
| `getToken` rejects with "messaging/unsupported-browser" | Browser doesn't support Web Push (old browser / no secure context) | Use Chrome/Edge/Firefox on HTTPS |
| `getToken` rejects with "messaging/registration-token-not-issued" or SW errors | `firebase-messaging-sw.js` not served from the **domain root** (e.g. `/assets/…`) or SW file has errors | Serve it at exactly `/firebase-messaging-sw.js`, check SW errors in Application → Service Workers |
| `getToken` returns `undefined`/`null` silently | `Notification.requestPermission()` returned `"denied"` or `"default"` (never asked, or user blocked) | Re-request permission; in Firefox also check "Always send pushes" setting |
| `getToken` throws "firebase is not defined" (SW) | Wrong URL/version of the compat scripts in the SW | Use the exact `importScripts` URLs from §5.2 |
| DevTools shows "Page is not secure" | FCM web push requires **HTTPS** (localhost is allowed) | Deploy to HTTPS or use `https://localhost` for dev |

### Step 4 — Verify what the server actually stored
If the payload had both fields and login returned 200, check the DB for the user:
```sql
SELECT Id, UserId, Token, DevicePlatform, IsDeleted, CreatedAt
FROM UserFbTokens
WHERE UserId = '<userId-from-login-response>'
ORDER BY CreatedAt DESC;
```
- A row with `DevicePlatform = 0` = web registered ✅
- Row with `IsDeleted = 1` = old web token replaced by a newer registration (normal — one token per platform)
- No row at all → the payload from **Step 2** did not contain both fields (or the login threw before line 117)

### Step 5 — Server-side facts (why it *looks* like a server bug but isn't)
- The handler **skips silently** when the fields are absent — no log line exists for the skip or the registration.
- `RegisterTokenAsync` deletes the user's previous token of the **same platform** before inserting (so re-login = 1 active web row).
- A token already owned by another user is transferred to the new user on registration.

---

## 7️⃣ Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| `FirebaseApp` creation throws | `firebase-credentials.json` missing or invalid path in `FirebaseSettings.CredentialsFilePath` |
| Web push never arrives | Service worker not at domain root; wrong VAPID key; `messagingSenderId` mismatch in SW; no `UserFbToken` row with `DevicePlatform = 0` (go through §6) |
| Push works on Android but not web | Web notifications need a **registered web token** — check the `UserFbToken` row (`DevicePlatform = 0`) |
| Token deleted unexpectedly | FCM reported the token `Unregistered` — re-register on next login |
| Notification saved in DB but no push | FCM dispatch failure is swallowed by design (see §4) — check Serilog for `FirebaseMessagingException` |
| Badge never updates | `/notifications/count` is read-only; unread marking is handled by a separate read endpoint (implement in your frontend) |
| Login works but no `UserFbToken` row | Payload missing `fcmToken` / `devicePlatform` (see §6 Steps 2–3) |

---

## 8️⃣ Notes / Current Behavior

- All push titles/bodies are **Arabic** and hardcoded in `NotificationBuilderService`; localization files do not cover push text.
- Webpush is only sent a `link` (`FcmOptions.Link`) when the payload link is a valid absolute URL (from `IDeepLinkService`).
- Android sends `Priority.High` + channel `clinic_hub_default` (create the channel in your Android app with the same ID).
- iOS sends `contentAvailable: true` with category `clinic_hub`.
