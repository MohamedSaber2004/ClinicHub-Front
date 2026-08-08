# 🔔 Push Notifications & Web Login — Backend Report (for Frontend Team)

**Date:** 2026-08-08
**Prepared by:** Backend team (ClinicHub API)
**Scope:** Why the FCM token is not registered after web login, the FCM producing issue, and why mobile push notifications are not delivered.
**Related docs:** `docs/FCM_NOTIFICATIONS_README.md`

---

## 1. Summary of Findings

| # | Symptom | Root cause (backend analysis) | Owner |
|---|---------|-------------------------------|-------|
| 1 | After web login, no FCM token row exists in `UserFbTokens` | Backend registers the token **only if both** `fcmToken` AND `devicePlatform` are present in the login payload. If the frontend sends only one of them (or sends the platform as a string), registration is skipped silently. | **Frontend** (payload) |
| 2 | FCM producing / no push arrives on mobile | All FCM dispatch errors are **swallowed silently** (`catch (Exception) { }`) — the API never logs why a send failed, so failures look like "push not pushed" with zero trace. | Backend (fix pending) + Frontend (diagnosis) |
| 3 | Mobile tokens disappear from DB after first push attempt | `FcmService` **auto-deletes** any token FCM answers `Unregistered` or `SenderIdMismatch` with. If the Firebase service-account (`project_id=doctory-1aca1`) does not match the app's sender ID (`google-services.json` / `messagingSenderId`), every send fails and the token is deleted → token "never registered" + no push. | Both teams |
| 4 | Android notifications do not appear | Server sends `ChannelId = "clinic_hub_default"`; Android 8+ silently drops notifications for channels the app never created. | **Mobile team** |

---

## 2. Issue 1 — FCM token not registered after web login

### Backend contract (what the API requires)

Endpoint: `POST /api/v1/auth/login-web`

```json
{
  "email": "user@x.com",
  "password": "...",
  "fcmToken": "cwNa...real-web-push-token...",
  "devicePlatform": 0
}
```

`devicePlatform` values (numeric enum): `0 = Web`, `1 = Android`, `2 = iOS`.

Backend condition (`LoginWebCommandHandler.cs:121-133`):

```csharp
var fcmTokenIsEmpty = string.IsNullOrWhiteSpace(request.FcmToken);
if (!fcmTokenIsEmpty && request.DevicePlatform.HasValue)
{
    await _fcmService.RegisterTokenAsync(user.Id, request.FcmToken, request.DevicePlatform.Value);
}
else
{
    _logger.LogWarning("FCM token NOT registered for user {UserId}. FcmTokenEmpty={FcmTokenEmpty}, DevicePlatformProvided={DevicePlatformProvided} ...");
}
```

Registration happens **only when BOTH are true**:

1. `fcmToken` is a non-empty, non-whitespace string
2. `devicePlatform` is a valid enum **number** (not `"0"` string, not `null`, not missing)

**Important:** `fcmToken` and `devicePlatform` are both optional, unvalidated fields. The login succeeds (HTTP 200) even when they are missing — the token is simply not registered.

### Most likely frontend causes (in order of probability)

1. **`devicePlatform` is not sent at all** on the web login call → registration skipped.
2. **`devicePlatform` sent as string** `"0"` instead of number `0` → JSON enum binding fails → `HasValue` = false → skipped.
3. **`fcmToken` sent as empty string** `""` or `null` → `IsNullOrWhiteSpace` → skipped.
4. **`getToken()` on the frontend returned null/threw** (bad VAPID key, wrong `messagingSenderId`, service worker not at domain root, permission denied) → nothing to send.
5. Login returned **403** (e.g. no active subscription for ClinicOwner/Staff/Doctor) → handler threw before reaching token code. (Note: token registration runs *before* the subscription check, so a 200 means registration had a chance to run; a 403 means it never ran for dashboard roles without an active subscription.)

### What the frontend must do

- Get a web FCM token **before** calling login:
  - Firebase web app must exist in the same project as `firebase-credentials.json` (`doctory-1aca1`).
  - `firebase-messaging-sw.js` must be served from the **domain root** (`/firebase-messaging-sw.js`) with the correct `messagingSenderId`.
  - Request `Notification.requestPermission()`; only if `"granted"` call `getToken(messaging, { vapidKey: "<VAPID_PUBLIC_KEY>" })`.
- Include **both** fields in the login body only when a real token exists:
  ```js
  if (fcmToken) {
    body.fcmToken = fcmToken;      // non-empty string
    body.devicePlatform = 0;       // NUMBER, never "0"
  }
  ```
- Verify in DevTools → Network → `login-web` request payload that both fields are present.
- Re-send the token on **every** login (browsers rotate web push tokens).

### How to verify server-side

```sql
SELECT Id, UserId, Token, DevicePlatform, IsDeleted, CreatedAt
FROM UserFbTokens
WHERE UserId = '<userId from login response>'
ORDER BY CreatedAt DESC;
```

- Row with `DevicePlatform = 0` and `IsDeleted = 0` → web token registered ✅
- No row → payload did not include both fields (or login didn't reach line 121)
- `IsDeleted = 1` rows → old web tokens replaced (normal)

---

## 3. Issue 2 — "FCM producing" problem & why mobile push is not pushed

### Backend facts (`FcmService.cs`)

`SendToUserAsync` (lines 60-90) is the single entry point for **all** push notifications (appointments, payments, chat, refunds, reminders):

```csharp
foreach (var token in tokens)
{
    try
    {
        await SendToDeviceAsync(token.Token, payload, token.DevicePlatform);
    }
    catch (FirebaseMessagingException ex) when (
        ex.MessagingErrorCode == MessagingErrorCode.Unregistered ||
        ex.MessagingErrorCode == MessagingErrorCode.SenderIdMismatch)
    {
        _tokenRepository.Delete(token);          // ← token DELETED
    }
    catch (Exception)
    {
        // ← ALL other errors silently swallowed, NO logging
    }
}
```

Problems identified:

1. **Zero observability** — `FcmService` has no `ILogger` at all. Any failure (invalid credential, wrong project, network, quota) is swallowed. The only way to confirm a push was attempted is FCM-side (Firebase Console → Cloud Messaging → Reports) or server logs of the *caller*.
2. **Tokens are deleted on any FCM rejection** — including `SenderIdMismatch`. If the service-account used by the API (`doctory-1aca1`) does not belong to the same project as the mobile app's `google-services.json` / `messagingSenderId`, every send returns `SenderIdMismatch` → the freshly-registered token is deleted → looks like "token was never registered" and push is never delivered.
3. **Android channel mismatch** — server sends `Android.Notification.ChannelId = "clinic_hub_default"`. If the mobile app does not create a notification channel with **exactly** this ID, Android 8+ silently discards the notification (app process receives nothing). Mobile team must create `channel "clinic_hub_default"` at app startup.
4. **No tokens → silent no-op** — if `GetUserTokensAsync` returns nothing (because of Issue 1 or Issue 3's deletions), `SendToUserAsync` returns without any log. The in-app `Notifications` table row is still created, so the bell icon may show the notification even though no push arrived.

### Required checks by the frontend/mobile teams

| Check | How | Expected |
|---|---|---|
| Same Firebase project everywhere | Compare `project_id` in server `Firebase/firebase-credentials.json` (`doctory-1aca1`) with mobile `google-services.json` and web `firebaseConfig.projectId` | All three must match |
| Sender ID matches | Server service-account `project_number` vs mobile `messagingSenderId` | Must match |
| Token validity | In Firebase Console → Cloud Messaging → Test notification, paste the token from the `UserFbTokens` row | Should deliver; if "Invalid/unregistered token" → token belongs to a different project or is stale |
| Android channel | In the app, create channel `clinic_hub_default` with high importance | Notifications show on Android 8+ |
| iOS config | Server sends `Apns` with `Sound/Badge/ContentAvailable/Category` from `appsettings` → `FirebaseSettings.Ios` | APNs cert/key must be valid in Firebase project |

### Known backend limitation (fix planned)

- FCM dispatch errors are silently ignored by design ("never block business logic"). A follow-up will add `ILogger<FcmService>` + structured logging of `MessagingErrorCode` so failures become visible in logs. Until then, **any "no push" report must be investigated from the Firebase Console reports and the checks above.**

---

## 4. Recommended fix on frontend — full correct web login flow

```js
export async function getFcmToken(messaging, vapidKey) {
  if (!("serviceWorker" in navigator) || !("Notification" in window)) return null;

  const permission = await Notification.requestPermission();
  if (permission !== "granted") {
    console.warn("[FCM] permission not granted:", permission);
    return null;
  }

  try {
    await navigator.serviceWorker.register("/firebase-messaging-sw.js");
    const token = await getToken(messaging, { vapidKey });
    if (!token) console.warn("[FCM] getToken returned null");
    else console.log("[FCM] token obtained:", token.slice(0, 30) + "...");
    return token;
  } catch (err) {
    console.error("[FCM] getToken threw:", err);   // ← root cause shows here
    return null;
  }
}

// in login handler:
const fcmToken = await getFcmToken(messaging, vapidKey);
const body = { email, password };
if (fcmToken) {
  body.fcmToken = fcmToken;
  body.devicePlatform = 0;     // number, never string
}
const res = await fetch("/api/v1/auth/login-web", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
```

---

## 5. Checklist for the frontend team

- [ ] Confirm `projectId`, `messagingSenderId`, VAPID key match the server's Firebase project (`doctory-1aca1`)
- [ ] Confirm `firebase-messaging-sw.js` is served from the domain root and registers successfully
- [ ] `getToken()` returns a real token (log it, check it doesn't throw `messaging/invalid-vapid-key` or `messaging/sender-id-mismatch`)
- [ ] Login payload includes `fcmToken` (non-empty) **and** `devicePlatform` as number `0` — verify in Network tab
- [ ] Check DB row `UserFbTokens` → `DevicePlatform = 0`, `IsDeleted = 0` after login
- [ ] Mobile: create notification channel `clinic_hub_default` (Android) with high importance
- [ ] Mobile: `google-services.json` sender ID matches the server service-account project
- [ ] Test send from Firebase Console using a registered token from the DB

---

## 6. Action items (backend side, for reference)

1. Add `ILogger<FcmService>` and log `MessagingErrorCode` + reason on every FCM failure (no more silent swallowing).
2. Consider a dedicated `POST /api/v1/auth/fcm-token` endpoint so the token can be (re)registered outside login.
3. Re-evaluate auto-deleting tokens on `SenderIdMismatch` — currently this removes tokens that might be fixable, making the symptom "token not registered".
