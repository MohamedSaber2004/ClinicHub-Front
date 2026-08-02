# User Profile API — Frontend Integration Guide (All Roles)

> **Audience:** Frontend team (Doctory web app).
> **Date:** 2026-08-01
> **Base URL:** `https://doctory-icare.runasp.net` — all paths below are relative to `/api/v1`.

One endpoint serves the **logged-in user's profile for every role** — Patient/User, Doctor, Staff,
ClinicOwner and SuperAdmin. The profile is resolved **from the Bearer token**; never send a user id
in the URL or body.

---

## 1. Endpoint

| Method | Route | Auth | Works for |
|--------|-------|------|-----------|
| `GET` | `/auth/profile` | Bearer token | **All roles** |

The route requires authentication (`[RoleAuthorize]` with no specific role), so any logged-in user
can call it. Invalid/expired token → `401 Unauthorized`.

### curl

```bash
curl -s -H "Authorization: Bearer <ACCESS_TOKEN>" \
     -H "Accept-Language: ar" \
     https://doctory-icare.runasp.net/api/v1/auth/profile
```

---

## 2. Response

The API wraps every result in the standard envelope:

```json
{
  "success": true,
  "data": { ... },
  "message": "تم بنجاح",
  "errors": {},
  "statusCode": 200
}
```

`data` (same shape for all roles):

```json
{
  "id": "0a1e2b3c-2222-2222-2222-222222222222",
  "fullName": "د. أحمد محمد",
  "email": "ahmed@clinic.com",
  "gender": 1,
  "phoneNumber": "01012345678",
  "birthDate": "1990-05-15T00:00:00",
  "profilePictureUrl": null,
  "language": 2,
  "roles": "Doctor",
  "isFreelanceDoctor": false
}
```

> `gender`, `birthDate`, `profilePictureUrl` can be `null` when never set. `message` is localized
> (default Arabic, controlled by the `Accept-Language` header).

### Field reference

| Field | Type | Notes |
|-------|------|-------|
| `id` | string (GUID) | User id — use it only for display/linking; the API itself resolves identity from the token |
| `fullName` | string | Full name as entered at registration |
| `email` | string | Login email |
| `gender` | number `1`-`3` | `1 = Male`, `2 = Female`, `3 = other` — nullable |
| `phoneNumber` | string | May be empty string |
| `birthDate` | string (date-time) | `yyyy-MM-ddTHH:mm:ss` — nullable |
| `profilePictureUrl` | string | Relative path (e.g. `/files/...`) — **prepend the API origin** to render. Nullable |
| `language` | number `1`-`2` | `1 = en`, `2 = ar` (default ar) |
| `roles` | string | The user's **single role name** (e.g. `"Doctor"`, `"ClinicOwner"`, `"Staff"`, `"SuperAdmin"`, `"User"`) |
| `isFreelanceDoctor` | boolean | `true` only when the logged-in doctor is freelance (no clinic) |

### Roles the frontend can expect

| `roles` value | Notes |
|---------------|-------|
| `User` | Patient / app user |
| `Doctor` | Works for a clinic or freelance (`isFreelanceDoctor` tells which) |
| `Staff` | Clinic staff (receptionist) |
| `ClinicOwner` | Owns one or more clinics |
| `SuperAdmin` | Platform admin |

> ⚠️ `roles` contains **only the first role**. Users with multiple roles still receive a single
> string — route to the correct dashboard by checking this string.

---

## 3. Error cases

| Code | Meaning | Body |
|------|---------|------|
| 401 | Missing, invalid or expired token | `{ "success": false, "data": null, "message": "...", "errors": {}, "statusCode": 401 }` |

For 401 the frontend should redirect to login / refresh the token via `POST /auth/refresh-token`.

---

## 4. Where to get the rest of the role context

`/auth/profile` returns **only** identity + language. Role-specific context comes from other calls:

| Need | Call |
|------|------|
| Clinic id, clinic status, verification status, "is clinic setup complete" | `POST /auth/login` / `POST /auth/login-web` response (`AuthResponseDto` — available once at login; cache it) |
| Doctor details (specialty, clinic, fees, ratings) | `GET /doctors/{id}` · `GET /doctors/{id}/details` |
| Clinic data for owner/staff | `GET /admin/clinics/{id}/details` |
| Dashboard stats per role | `GET /doctors/dashboard/stats` · `GET /staff/dashboard/stats` · `GET /admin/dashboard/stats` |

---

## 5. Related endpoints

### `PATCH /auth/profile/update` — edit my profile (all roles)

All fields optional — send only what changed:

```json
{ "fullName": "أحمد محمد", "phoneNumber": "01122334455", "birthDate": "1990-05-15", "gender": 1 }
```

| Field | Rule |
|-------|------|
| `fullName` | Optional, non-empty when provided |
| `phoneNumber` | Optional |
| `birthDate` | Optional, must not be in the future |
| `gender` | Optional, `1`-`3` |
| `profileImageUrl` | Optional — set to the path returned by the avatar upload (`POST /attachments/upload`) |

Response `data: true` on success.

### `PUT /auth/language/update` — change language

```json
{ "language": 2 }
```

`1 = en`, `2 = ar`. Response `data: true`.

### Avatar upload (before saving `profileImageUrl`)

`POST /attachments/upload` (multipart `file`, `place` = the attachments category). Use the returned
file path as `profileImageUrl`.

---

## 6. Frontend checklist

- [ ] Call `GET /auth/profile` on dashboard load (any role) with the Bearer token.
- [ ] Handle `401` → refresh token → retry once → redirect to login.
- [ ] Render `profilePictureUrl` with the API origin prepended.
- [ ] Route the user by the `roles` string (`User` / `Doctor` / `Staff` / `ClinicOwner` / `SuperAdmin`).
- [ ] Show `language` (`2` = Arabic) to pick the UI culture.
- [ ] When the logged-in user is a doctor, read `isFreelanceDoctor` to choose between clinic and
      freelance profile UIs.
