# 🔍 Verification Center API Contract — README for Backend Team

> **Purpose:** The frontend (`ClinicHub`) gets an **unknown exception** when Super Admin clicks **"قبول الطلب" (Accept)** in the Verification Center (`/Admin/Verification`).
> This document defines the **exact contract** the frontend expects, so the backend team can detect what is missing/misaligned.

---

## 1. The Error Observed (Frontend)

- Action: Super Admin → Verification Center → select a pending request → **قبول الطلب** → confirm.
- User sees: `عذراً، حدث خطأ غير متوقع أثناء معالجة الطلب.`
- Previous raw error (before frontend hardening): `Error converting value {null} to type 'System.Boolean'. Path 'data', line 1, position 40.`

### What the frontend does (call chain)

```
VerificationCenter.cshtml JS
   └─ POST /Admin/AcceptVerification?userId={guid}   (X-Requested-With: XMLHttpRequest)
        └─ AdminController.AcceptVerification
             └─ IUserVerificationService.ApproveUserVerificationAsync
                  └─ POST {base}/admin/users/{userId}/approve   ← BACKEND CALL
```

---

## 2. Endpoint #1 — Accept (approve) a verification request

| Item | Value |
|---|---|
| **Method / URL** | `POST {base}/admin/users/{userId}/approve` |
| **Auth** | Requires Super Admin / SystemAdmin bearer token |
| **Path param** | `userId` — `Guid` of the **user** (NOT the verification record id) |
| **Body** | `{"userId": "5f2c0000-0000-0000-0000-000000000001"}` |
| **Content-Type** | `application/json` |

### Frontend request body (serialized by `ApproveUserVerficationRequest`)
```json
{ "userId": "5f2c0000-0000-0000-0000-000000000001" }
```

### Expected success response (contract)
```json
{
  "success": true,
  "message": "تم قبول طلب التحقق بنجاح",
  "data": true
}
```
> ⚠️ **`data` MUST be a boolean (`true`)**. `null` breaks Newtonsoft deserialization on the frontend — this was the original 500 error.

### Expected failure response (contract)
```json
{
  "success": false,
  "message": "سبب رفض العملية بالعربية",
  "data": null
}
```
> ⚠️ **Non-2xx status + this body is fine** — the frontend extracts `message`/`errors` and shows it. What is NOT fine: a **2xx (200) response with `data: null` or missing `success`**.

---

## 3. Endpoint #2 — Reject a verification request

| Item | Value |
|---|---|
| **Method / URL** | `POST {base}/admin/users/{userId}/reject` |
| **Auth** | Super Admin / SystemAdmin bearer token |
| **Path param** | `userId` — `Guid` of the **user** |
| **Body** | `{"userId": "...", "notes": "السبب"}` |
| **Content-Type** | `application/json` |

### Request body (serialized by `RejectUserVerificationRequest`)
```json
{ "userId": "5f2c0000-0000-0000-0000-000000000001", "notes": "مستندات غير مكتملة" }
```

### Expected success response
```json
{ "success": true, "message": "تم رفض طلب التحقق بنجاح", "data": true }
```

### Expected failure response
```json
{ "success": false, "message": "...", "data": null }
```

---

## 4. Endpoint #3 — List pending verification requests

| Item | Value |
|---|---|
| **Method / URL** | `GET {base}/admin/users/pending?PageNumber=1&PageSize=20` |
| **Auth** | Super Admin bearer token |

### Expected response (contract)
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "00000000-0000-0000-0000-000000000000",
        "userId": "5f2c0000-0000-0000-0000-000000000001",
        "userFullName": "د. سارة أحمد",
        "userEmail": "sara@example.com",
        "userPhoneNumber": "+201001234567",
        "status": 0,
        "requestedRole": 4,
        "requestedAt": "2026-07-20T10:30:00Z",
        "reviewedAt": null,
        "reviewedByFullName": null,
        "notes": null,
        "professionalPracticeCardImage": "files/xxx.jpg",
        "taxCardImage": "files/yyy.jpg",
        "unionIdCardImage": null,
        "doctorImage": "files/zzz.jpg"
      }
    ],
    "totalCount": 1,
    "pageNumber": 1,
    "pageSize": 20
  }
}
```

### Field expectations
| Field | Type | Notes |
|---|---|---|
| `id` | Guid | Verification **record** id |
| `userId` | Guid | User id — **this is what accept/reject use** |
| `status` | int | `0` pending, `1` approved, `2` rejected (`VerificationStatus`) |
| `requestedRole` | int | `0` None, `1` User, `2` SuperAdmin, `4` Doctor, `8` Staff, `16` ClinicOwner (`UserType`) |
| image fields | string \| null | resolved to full URL by frontend; `null` ok |

> ⚠️ **`requestedRole` MUST match the enum above.** The UI maps `16 → "مالك عيادة"`, `4 → "طبيب"`. Values outside `{1,2,4,8,16}` render as the raw number.

---

## 5. Enums used by the frontend (single source of truth)

### `VerificationStatus`
| Value | Name | UI badge |
|---|---|---|
| 0 | Pending | قيد الانتظار (warning) |
| 1 | Approved | مقبول (success) |
| 2 | Rejected | مرفوض (danger) |

### `UserType` (bit flags)
| Value | Name | UI label |
|---|---|---|
| 0 | None | لا يوجد |
| 1 | User | مستخدم |
| 2 | SuperAdmin | مدير عام |
| 4 | Doctor | طبيب |
| 8 | Staff | موظف |
| 16 | ClinicOwner | مالك عيادة |

---

## 6. Backend checklist — detect what's missing

When testing `POST {base}/admin/users/{userId}/approve`, verify in this order:

- [ ] **1. Route exists** — `/admin/users/{userId}/approve` returns something other than 404. (Frontend route builder: `DoctoryRoutes.cs:112`.)
- [ ] **2. Auth** — endpoint accepts the Super Admin token the frontend sends. Test 401/403 separately.
- [ ] **3. Body binding** — request model is `ApproveUserVerficationRequest` with `UserId` (`Guid`). Missing `userId` → 400.
- [ ] **4. Status codes** — the endpoint MUST NOT return 200 when the operation failed. Use 400/404/409/500 with a JSON error body.
- [ ] **5. Response envelope** — every response has `success` (bool), `message` (string), `data` (bool or null).
  - ✅ `200 {"success":true,...,"data":true}` — success
  - ✅ `400 {"success":false,...,"data":null,"message":"..."}` — business failure
  - ❌ `200 {"success":false,"data":null}` — **ambiguous, breaks frontend success detection**
  - ❌ `200 {"data":null}` — **missing `success` = frontend reads "failure"**
- [ ] **6. `data` field type** — `data` must never be a JSON object/string when frontend expects `bool`.
- [ ] **7. Business logic** — accepting should:
  - update the verification record to `Approved (1)`,
  - set `ReviewedAt`, `ReviewedByFullName`,
  - flip the **user's** status to active/verified (so they can log in),
  - if `requestedRole == ClinicOwner (16)` → maybe create/activate the clinic subscription.
  - ⚠️ If the user/clinic record is missing (e.g., seed data removed), backend MUST return 404 with message — **not throw an unhandled exception**.
- [ ] **8. Idempotency** — approving an already-approved request should return a clear message (`"الطلب معالج بالفعل"`), not throw.

---

## 7. Most likely root causes (given frontend evidence)

1. **Backend throws unhandled exception** (e.g., `NullReferenceException` because the verification record or user doesn't exist) → ASP.NET returns `500` with HTML/ProblemDetails body that has **no `success`/`message` fields** → frontend shows generic "حدث خطأ غير متوقع".
2. **`data: null` on a 200** — exactly the earlier `Error converting value {null} to type 'System.Boolean'` error. If that reappears, the backend returns `{"success":...,"data":null}` on a **2xx**.
3. **Wrong id used** — frontend sends `userId` (from the pending list). If the backend expects the verification **record** `id` instead, it will not find the entity → throw.

---

## 8. How to reproduce & capture for backend

1. Open browser devtools → **Network** tab.
2. Click قبول الطلب → confirm.
3. Copy the failed request:
   - URL: `POST {base}/admin/users/{userId}/approve`
   - Request payload: `{"userId":"..."}`
   - Response status + response body (JSON).
4. Include those 3 items in the backend ticket — that's everything the backend needs.

---

## 9. Frontend side (already hardened, no further action needed)

- `UserVerificationService.cs` now:
  - throws `ApiException` with the backend's real message on non-2xx (no more generic 500 masking),
  - parses responses with `ParseBoolResponse` (JObject-based) so `data: null` can no longer crash deserialization,
  - falls back to `success=true` only when `success` is an explicit JSON `true`.
- `AdminController.AcceptVerification`/`RejectVerification` return clean `{ success, message }` JSON for AJAX.
