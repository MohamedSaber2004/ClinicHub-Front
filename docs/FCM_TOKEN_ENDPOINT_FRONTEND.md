# 🔔 Web Push Notifications — New FCM Token Endpoint (Frontend Guide)

**Date:** 2026-08-08
**For:** Frontend team (web dashboard)
**What changed:** The backend now exposes a dedicated endpoint to register the push notification token **any time after login** — no more waiting for the next login.

---

## 1. Why this endpoint exists

**The problem we found:** the live database has **zero web push tokens**. The token was only sent to the backend during login — so if the browser permission was granted *after* a login (or the first login happened before the permission prompt was answered), the token was never registered and push never worked.

**The fix:** register the token **at the moment the browser produces it**, not at the next login.

---

## 2. The new endpoint

```
POST {apiBaseUrl}/api/v1/auth/fcm-token
Authorization: Bearer <accessToken>
Content-Type: application/json
```

### Request body

```json
{
  "fcmToken": "cwNaB...browser-push-token...",
  "devicePlatform": 0
}
```

| Field | Type | Value |
|---|---|---|
| `fcmToken` | string | Non-empty FCM web token from `getToken()` |
| `devicePlatform` | number | `0` = Web (**must be a number**, not `"0"`) |

### Responses

| Status | Meaning |
|---|---|
| `200` | Token registered/replaced — response body contains a success message (`data`) |
| `400` | `fcmToken` empty or `devicePlatform` missing |
| `401` | No valid access token |

Behavior (same rules as login-time registration):
- One active token per user per platform — calling it again replaces the old web token.
- If the same token is already registered to another user, it is moved to the current user.
- Safe to call repeatedly (every page load, every token refresh).

---

## 3. When to call it

Call it whenever you obtain (or already have) a valid token:

1. **After the user clicks "تفعيل الإشعارات"** on the dashboard banner and permission becomes `granted`.
2. **On every dashboard page load** when `Notification.permission === "granted"` (covers the case where permission was granted in an earlier session).
3. **After a token rotation** (browsers rotate web tokens over time).
4. **After login** (still works — keeps login as a fallback).

---

## 4. Complete integration example

Add to `fcm.js` (inside `handleForeground`):

```js
function registerTokenOnBackend(token) {
    var accessToken = localStorage.getItem("accessToken");
    if (!accessToken) return;

    fetch(cfg.apiBaseUrl + "/api/v1/auth/fcm-token", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "Authorization": "Bearer " + accessToken
        },
        body: JSON.stringify({ fcmToken: token, devicePlatform: 0 })
    })
    .then(function (r) { return r.json(); })
    .then(function (json) {
        console.log("[FCM] token registered on backend", json && json.data ? json.data : json);
    })
    .catch(function (err) {
        console.warn("[FCM] backend token registration failed:", err);
    });
}
```

Replace the body of `syncCachedToken` (currently it only caches to localStorage):

```js
function syncCachedToken() {
    if (!("Notification" in window) || Notification.permission !== "granted") return;
    registerServiceWorker()
        .then(function () { return getToken(messaging, { vapidKey: VAPID_PUBLIC_KEY }); })
        .then(function (token) {
            if (!token) return;
            try { localStorage.setItem(TOKEN_CACHE_KEY, token); } catch (e) {}
            console.log("[FCM] token ready, registering on backend (" + token.length + " chars)");
            registerTokenOnBackend(token);   // ← registers NOW, not at next login
        })
        .catch(function (err) {
            console.warn("[FCM] token refresh failed:", err && err.message ? err.message : err);
        });
}
```

Also call `registerTokenOnBackend` inside the banner button handler after `p === "granted"` (currently it only calls `syncCachedToken()` — that now covers it automatically since `syncCachedToken` registers on the backend too).

---

## 5. Verify it works

1. Open the dashboard, click **تفعيل الإشعارات**, click **Allow**.
2. Console should show: `[FCM] token ready, registering on backend (…)` then `[FCM] token registered on backend …`.
3. Confirm the row exists (backend side):
   ```sql
   SELECT UserId, Token, DevicePlatform, IsDeleted, CreatedAt
   FROM UserFbTokens
   WHERE DevicePlatform = 0
   ORDER BY CreatedAt DESC;
   ```
   A row with `DevicePlatform = 0` and `IsDeleted = 0` = registered. ✅
4. Trigger any notification (e.g. a booking/cancellation for that user). Browser should show it:
   - Dashboard open → in-app modal + bell badge.
   - Dashboard closed → system notification from the service worker.

---

## 6. Still not displaying? Checklist

- [ ] Console shows `[FCM] token produced successfully` — if not, read `[FCM] getToken failed: <error>` (e.g. `messaging/invalid-vapid-key` → wrong VAPID key in `FirebaseWeb:VapidKey`)
- [ ] Console shows `[FCM] token registered on backend`
- [ ] DB has a `DevicePlatform = 0` row (query above)
- [ ] `firebase-messaging-sw.js` served at domain root and registered (Application → Service Workers)
- [ ] Browser notification permission granted (not just "default")

---

## 7. Related files

| File | Role |
|---|---|
| `ClinicHub.Application/Features/Auth/Commands/RegisterFcmToken/` | Command + handler + validator |
| `ClinicHub.API/Controllers/Version1/AuthController.cs` | Endpoint `POST /api/v1/auth/fcm-token` |
| `ClinicHub.API/Routes/ApiRoutes.cs` | Route constant |
| `ClinicHub.Infrastructure/Services/FcmService.cs` | Token registration logic |
| `docs/PUSH_WEB_LOGIN_BACKEND_REPORT.md` | Full investigation report + evidence |
