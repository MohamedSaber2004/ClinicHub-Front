# Create Doctor with Availability

Creates a new doctor and optionally assigns their weekly availability schedule in a single request.

---

## Endpoints

### 1. SuperAdmin — Create doctor in any clinic

```
POST /api/v{version}/admin/clinics/{clinicId:guid}/doctors
```

**Auth:** `SuperAdmin`  
**Plan permission:** (none required)

### 2. ClinicOwner — Create doctor in own clinic

```
POST /api/v{version}/admin/clinics/doctors
```

**Auth:** `ClinicOwner`  
**Plan permission:** `ManageDoctors`

> ClinicOwner does NOT send `clinicId` in the body — it is auto-assigned from the owner's current clinic.

---

## Request Body

```json
{
  "clinicId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "specializationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "bio": "Experienced cardiologist with 10+ years",
  "yearsOfExperience": 10,
  "availabilities": [
    {
      "dayOfWeek": 1,
      "startTime": "09:00:00",
      "endTime": "17:00:00",
      "slotDurationMinutes": 30
    },
    {
      "dayOfWeek": 3,
      "startTime": "10:00:00",
      "endTime": "14:00:00",
      "slotDurationMinutes": 30
    }
  ]
}
```

### Field details

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `clinicId` | Guid | Yes | Clinic to assign the doctor to |
| `userId` | Guid | Yes | Existing application user to link as doctor |
| `specializationId` | Guid | Yes | Medical specialization |
| `bio` | string | No | Doctor biography (default empty) |
| `yearsOfExperience` | int | Yes | Must be >= 0 |
| `availabilities` | array | No | Weekly schedule entries (optional, empty = no schedule) |

### Availability entry fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `dayOfWeek` | int | Yes | 0=Sunday, 1=Monday ... 6=Saturday |
| `startTime` | string (time) | Yes | Format: `HH:mm:ss` (24h) |
| `endTime` | string (time) | Yes | Must be > startTime |
| `slotDurationMinutes` | int | No | Appointment slot length (default 30, min 1, max 480) |

---

## Response Body (201 Created)

```json
{
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userName": "Ahmed Ali",
    "userEmail": "ahmed@example.com",
    "clinicId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clinicName": "ClinicHub Medical Center",
    "specializationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "specializationName": "قلبية",
    "bio": "Experienced cardiologist with 10+ years",
    "yearsOfExperience": 10,
    "isActive": true,
    "createdAt": "2026-07-28T21:00:00Z"
  },
  "message": "Created Successfully",
  "statusCode": 201
}
```

### Error responses

| Status | Meaning |
|--------|---------|
| `400` | Validation error (invalid time range, missing fields, doctor already exists in clinic) |
| `404` | Clinic, User, or Specialization not found |
| `401` | Unauthenticated |
| `403` | Insufficient role/permission |

**400 example:**

```json
{
  "data": null,
  "message": "Doctor already exists in this clinic",
  "statusCode": 400
}
```

---

## Business Logic

### When to use
- **ClinicOwner dashboard** — "Add Doctor" form. Owner selects an existing user, picks a specialization, fills bio/experience, and sets the doctor's weekly working hours.
- **SuperAdmin panel** — Same form but can also choose which clinic the doctor belongs to.

### What happens server-side
1. Validates clinic, user, and specialization exist
2. Checks no duplicate doctor (same user + same clinic)
3. Creates the `Doctor` record
4. For each availability entry, creates a `DoctorAvailability` record linked to the doctor and clinic
5. Returns the created doctor data

### Availability entries are optional
If `availabilities` is empty or omitted, the doctor is created without a schedule. The schedule can be added later via the Availability endpoints.

---

## Frontend Integration — ClinicOwner Dashboard

### New UI fields in "Add Doctor" form

Add a **"Working Hours"** section below the existing doctor fields:

```
┌─────────────────────────────────────┐
│  Add New Doctor                     │
│                                     │
│  User:      [select user dropdown]  │
│  Specialization: [select dropdown]  │
│  Bio:       [textarea]              │
│  Experience: [number input]         │
│                                     │
│  ── Working Hours ──                │
│  ┌─────────────────────────────┐    │
│  │ Day: [Mon ▼] From [09:00]  │    │
│  │ To:  [17:00] Slot [30] min │    │
│  │ [+ Add Day]                │    │
│  ├─────────────────────────────┤    │
│  │ Day: [Wed ▼] From [10:00]  │    │
│  │ To:  [14:00] Slot [30] min │    │
│  │ [Remove]                   │    │
│  └─────────────────────────────┘    │
│                                     │
│  [Cancel]              [Create]     │
└─────────────────────────────────────┘
```

### Day mapping (enum to display)

| Value | Display |
|-------|---------|
| 0 | Sunday |
| 1 | Monday |
| 2 | Tuesday |
| 3 | Wednesday |
| 4 | Thursday |
| 5 | Friday |
| 6 | Saturday |

### Example API call (fetch)

```javascript
const body = {
  clinicId: selectedClinicId,  // only for SuperAdmin; ClinicOwner skips this
  userId: selectedUserId,
  specializationId: selectedSpecializationId,
  bio: bioText,
  yearsOfExperience: Number(experienceYears),
  availabilities: workingHours.map(wh => ({
    dayOfWeek: wh.day,       // 0-6
    startTime: wh.from + ":00",  // "09:00:00"
    endTime: wh.to + ":00",      // "17:00:00"
    slotDurationMinutes: wh.slotDuration  // default 30
  }))
};

const res = await fetch("/api/v1/admin/clinics/doctors", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body)
});
```

### Notes for UI
- `availabilities` is optional — if the user doesn't set any working hours, send an empty array `[]` or omit the field
- `slotDurationMinutes` defaults to 30 on the server; only send it if you want a custom value
- `dayOfWeek` is an **integer** (0-6), not a string
- Time format is **24-hour** `HH:mm:ss` — always append `:00` for seconds
