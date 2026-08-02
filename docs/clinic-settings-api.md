# Clinic Settings API — Frontend Integration Guide

Documentation of the **clinic configuration/settings** form shown to the Clinic Owner so the backend team can implement the matching endpoint.

## Page

- **Frontend page:** `/Clinic/Settings` (إعدادات العيادة — Clinic Settings)
- **Who uses it:** Clinic Owner (logged in user with `ManageClinicSettings` permission)
- **Current state:** Form is UI-only (mock data, save button does not call the API yet). The backend endpoint is required to persist these fields.

---

## Fields Reference

The page is composed of 3 sections: Basic Information, Location, and Status.

### 1. Basic Information (المعلومات الأساسية)

| # | HTML `id` | Field (EN) | Label (AR) | Type | Required | Description / Notes |
|---|-----------|------------|------------|------|----------|---------------------|
| 1 | `settingsName` | `name` | اسم العيادة | `string` | ✅ Yes | Public clinic display name. Frontend blocks saving if empty. |
| 2 | `settingsDoctor` | `responsibleDoctor` | الطبيب المسؤول | `string` | ❌ No | Name of the doctor responsible for the clinic (displayed in clinic profile). |
| 3 | `settingsDesc` | `description` | الوصف | `string` (textarea, 3 rows) | ❌ No | Clinic description shown in public profile. |
| 4 | `settingsPhone` | `phone` | رقم الهاتف | `string` | ❌ No | Contact phone number (displayed in clinic contact info). |
| 5 | `settingsManager` | `managerName` | مدير العيادة | `string` | ❌ No | Clinic manager name (not necessarily a user account). |

### 2. Location (الموقع)

| # | HTML `id` | Field (EN) | Label (AR) | Type | Required | Description / Notes |
|---|-----------|------------|------------|------|----------|---------------------|
| 6 | `settingsLocation` | `location` | الموقع (الغرفة) | `string` | ❌ No | Building/floor/room text, e.g. `الطابق 3 - غرفة 302`. |
| 7 | `settingsSpecialty` | `specialty` | التخصص الرئيسي | `string` | ❌ No | One value from the specialty dropdown (see allowed values below). |
| 8 | `settingsLat` | `latitude` | الموقع على الخريطة | `double` | ❌ No | Map pin latitude. Precision: 6 decimal places (e.g. `31.040900`). Updated by clicking the map or the "موقعي الحالي" (my location) button. |
| 9 | `settingsLng` | `longitude` | الموقع على الخريطة | `double` | ❌ No | Map pin longitude. Precision: 6 decimal places (e.g. `31.378500`). |

### 3. Status (الحالة)

| # | HTML `id` | Field (EN) | Label (AR) | Type | Required | Description / Notes |
|---|-----------|------------|------------|------|----------|---------------------|
| 10 | `settingsActive` | `isActive` | العيادة نشطة | `boolean` | ✅ Yes | Toggle switch. `true` = clinic active/visible, `false` = inactive. |

---

## Specialty Dropdown — Allowed Values

The `specialty` field is a `<select>` with these options:

```
القلب والأوعية الدموية   (Cardiology)
الأمراض العصبية           (Neurology)
جراحة العظام              (Orthopedics)
الأمراض الجلدية           (Dermatology)
طب الأطفال                (Pediatrics)
```

The currently saved specialty is always the first (pre-selected) option; the list above is for new selection.

---

## Suggested Endpoint

### Update Clinic Settings

```
PUT  /api/v1/clinics/{clinicId}
```

**Auth:** `[Authorize]` + clinic owner of `{clinicId}` (or `ManageClinicSettings` permission).

**Request body (camelCase):**

```json
{
  "name": "عيادة القلب التخصصية",
  "responsibleDoctor": "د. سارة أحمد",
  "description": "عيادة متخصصة في أمراض القلب والأوعية الدموية",
  "phone": "01012345678",
  "managerName": "أحمد محمود",
  "location": "الطابق 3 - غرفة 302",
  "specialty": "القلب والأوعية الدموية",
  "latitude": 31.040900,
  "longitude": 31.378500,
  "isActive": true
}
```

**Response envelope** (same convention as the rest of the API):

```json
{
  "success": true,
  "data": { "clinicId": "guid" },
  "message": "Clinic settings updated successfully",
  "errors": {},
  "statusCode": 200
}
```

On validation failure (HTTP 400), return field-level errors in the `errors` object:

```json
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": {
    "Name": ["Clinic name is required"]
  },
  "statusCode": 400
}
```

---

## Behavior Notes for the Backend

1. **Localization** — user-facing messages should use localization keys; default language **Arabic** (controlled by `Accept-Language` header, same as invoices API).
2. **Coordinates** — the frontend sends `latitude`/`longitude` with 6-decimal precision; they are stored and re-displayed on the map on page load. Default when unset: `31.0409, 31.3785`.
3. **Name is mandatory** — the frontend validates it before save; backend should also validate (`Required`).
4. **No file upload** in this form — the clinic image (`ImageUrl`) is managed elsewhere (attachment upload flow), not part of this payload.
5. **Read endpoint** — the settings page needs a GET that returns the same fields to pre-fill the form:
   ```
   GET  /api/v1/clinics/{clinicId}
   ```
   Response data shape matches the request body above.

---

## Frontend DTO Reference (Mock — `MockClinic`)

The form is currently pre-filled from mock data with these properties (final DTO should mirror the field names above):

| Mock property | Type | Maps to field |
|---------------|------|---------------|
| `Name` | `string` | `name` |
| `ResponsibleDoctor` | `string` | `responsibleDoctor` |
| `Description` | `string` | `description` |
| `Phone` | `string` | `phone` |
| `ManagerName` | `string` | `managerName` |
| `Location` | `string` | `location` |
| `Specialty` | `string` | `specialty` |
| `Latitude` | `double` | `latitude` |
| `Longitude` | `double` | `longitude` |
| `IsActive` | `bool` | `isActive` |
