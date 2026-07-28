# Update Doctor & Get Doctor By ID

---

## Get Doctor By ID

### Endpoint

```
GET /api/v{version}/doctors/{id:guid}
```

**Auth:** `SuperAdmin`, `ClinicOwner`, (or any authenticated role that has access)

### Response (200 OK)

```json
{
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userName": "Ahmed Mohamed",
    "userEmail": "ahmed.doctor@clinic.com",
    "userPhoneNumber": "+201234567890",
    "profilePictureUrl": "https://cdn.clinichub.com/doctors/123.jpg",
    "clinicId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clinicName": "ClinicHub Medical Center",
    "specializationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "specializationName": "قلبية",
    "bio": "Experienced cardiologist with 10+ years",
    "yearsOfExperience": 10,
    "isActive": true,
    "createdAt": "2026-07-28T21:00:00Z",
    "availabilities": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "dayOfWeek": 1,
        "startTime": "09:00:00",
        "endTime": "17:00:00",
        "slotDurationMinutes": 30
      },
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "dayOfWeek": 3,
        "startTime": "10:00:00",
        "endTime": "14:00:00",
        "slotDurationMinutes": 30
      }
    ]
  },
  "message": "Ok",
  "statusCode": 200
}
```

| Field | Type | Description |
|-------|------|-------------|
| `id` | Guid | Doctor ID |
| `userId` | Guid | Linked user account ID |
| `userName` | string | Doctor's full name |
| `userEmail` | string | Doctor's email |
| `userPhoneNumber` | string? | Doctor's phone number |
| `profilePictureUrl` | string? | Doctor's profile image URL |
| `clinicId` | Guid | Clinic ID |
| `clinicName` | string | Clinic name |
| `specializationId` | Guid | Specialization ID |
| `specializationName` | string | Specialization name (Arabic) |
| `bio` | string | Doctor biography |
| `yearsOfExperience` | int | Years of experience |
| `isActive` | bool | Whether the doctor is active |
| `createdAt` | datetime | Creation timestamp |
| `availabilities` | array | Weekly working hours schedule |

### Availability entry

| Field | Type | Description |
|-------|------|-------------|
| `id` | Guid | Availability record ID |
| `dayOfWeek` | int | 0=Sunday … 6=Saturday |
| `startTime` | string | `HH:mm:ss` (24h) |
| `endTime` | string | `HH:mm:ss` (24h) |
| `slotDurationMinutes` | int | Appointment slot length in minutes |

---

## Update Doctor

### Endpoint

```
PUT /api/v{version}/doctors/{id:guid}
```

**Auth:** `SuperAdmin`, `ClinicOwner`  
**Plan permission:** `ManageDoctors`

> ClinicOwner can only update doctors belonging to their own clinic.

### Request Body

```json
{
  "fullName": "Ahmed Mohamed Updated",
  "email": "ahmed.new@clinic.com",
  "phoneNumber": "+201234567891",
  "birthDate": "1990-05-15",
  "gender": 1,
  "doctorImage": "doctor_new_photo.jpg",
  "bio": "Updated bio text",
  "yearsOfExperience": 12,
  "isActive": true,
  "availabilities": [
    {
      "dayOfWeek": 1,
      "startTime": "09:00:00",
      "endTime": "17:00:00",
      "slotDurationMinutes": 30
    },
    {
      "dayOfWeek": 2,
      "startTime": "09:00:00",
      "endTime": "15:00:00",
      "slotDurationMinutes": 30
    }
  ]
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `fullName` | string | No | Update user's full name |
| `email` | string | No | Update user's email (must be unique) |
| `phoneNumber` | string | No | Update user's phone (must be unique) |
| `birthDate` | string (date) | No | Update birth date (`yyyy-MM-dd`) |
| `gender` | int | No | 1=Male, 2=Female |
| `doctorImage` | string | No | Update profile picture filename |
| `bio` | string | No | Update doctor bio |
| `yearsOfExperience` | int | No | Must be >= 0 |
| `isActive` | bool | No | Activate/deactivate doctor |
| `availabilities` | array | No | **Replaces** all existing availability |

### How partial updates work

All fields are optional — only send the fields you want to change. Fields with `null` or omitted are left unchanged.

### How availability update works

- If `availabilities` is **empty array `[]` or omitted** → existing schedule stays unchanged
- If `availabilities` has entries → **all existing availability records are deleted** and replaced with the new ones (atomic transaction)

### Transaction guarantee

User data, doctor data, and availability are updated in a single database transaction. Either **all succeed** or **all fail**.

### Response (200 OK)

```json
{
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userName": "Ahmed Mohamed",
    "userEmail": "ahmed.doctor@clinic.com",
    "userPhoneNumber": "+201234567890",
    "profilePictureUrl": "https://cdn.clinichub.com/doctors/123.jpg",
    "clinicId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clinicName": "ClinicHub Medical Center",
    "specializationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "specializationName": "قلبية",
    "bio": "Updated bio text",
    "yearsOfExperience": 12,
    "isActive": true,
    "createdAt": "2026-07-28T21:00:00Z",
    "availabilities": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "dayOfWeek": 1,
        "startTime": "09:00:00",
        "endTime": "17:00:00",
        "slotDurationMinutes": 30
      },
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "dayOfWeek": 2,
        "startTime": "09:00:00",
        "endTime": "15:00:00",
        "slotDurationMinutes": 30
      }
    ]
  },
  "message": "Ok",
  "statusCode": 200
}
```

---

## Frontend Integration

### Updated UI — Edit Doctor Form

When the user clicks "Edit" on a doctor, call `GET /doctors/{id}` first to populate the form, then `PUT /doctors/{id}` to save changes.

```
┌──────────────────────────────────────┐
│  Edit Doctor                         │
│                                      │
│  Full Name    [Ahmed Mohamed_____]   │
│  Email        [ahmed@clinic.com__]   │
│  Phone        [+201234567890____]   │
│  Birth Date   [____date picker___]   │
│  Gender       [Male ▾]              │
│  Profile Pic  [Upload / Change]     │
│                                      │
│  Bio:         [textarea pre-filled]  │
│  Experience:  [12]                   │
│  Active:      [toggle: yes]          │
│                                      │
│  ── Working Hours ──                 │
│  ┌──────────────────────────────┐    │
│  │ Day: [Mon ▾] From [09:00]   │    │
│  │ To:  [17:00] Slot [30] min  │    │
│  │ [Remove]                    │    │
│  ├──────────────────────────────┤    │
│  │ Day: [Tue ▾] From [09:00]   │    │
│  │ To:  [15:00] Slot [30] min  │    │
│  │ [Remove]                    │    │
│  ├──────────────────────────────┤    │
│  │ [+ Add Day]                 │    │
│  └──────────────────────────────┘    │
│                                      │
│  [Cancel]              [Save]        │
└──────────────────────────────────────┘
```

### Fetch example (JavaScript)

**Step 1 — Fetch current doctor data:**

```javascript
const res = await fetch(`/api/v1/doctors/${doctorId}`);
const doctor = (await res.json()).data;

// Populate form
fullNameInput.value = doctor.userName;
emailInput.value = doctor.userEmail;
phoneInput.value = doctor.userPhoneNumber;
birthDateInput.value = doctor.birthDate; // format as needed
genderSelect.value = doctor.gender;      // map 1/2
profilePicPreview.src = doctor.profilePictureUrl;
bioInput.value = doctor.bio;
experienceInput.value = doctor.yearsOfExperience;
activeToggle.checked = doctor.isActive;
workingHours = doctor.availabilities.map(a => ({
  dayOfWeek: a.dayOfWeek,
  startTime: a.startTime.slice(0, 5),  // "09:00"
  endTime: a.endTime.slice(0, 5),      // "17:00"
  slotDurationMinutes: a.slotDurationMinutes
}));
```

**Step 2 — Save changes:**

```javascript
const body = {};

// Only include changed fields (server ignores null/omitted)
if (changed.fullName) body.fullName = fullNameInput.value;
if (changed.email) body.email = emailInput.value;
if (changed.phone) body.phoneNumber = phoneInput.value;
if (changed.birthDate) body.birthDate = birthDateInput.value;
if (changed.gender) body.gender = Number(genderSelect.value);
if (changed.doctorImage) body.doctorImage = uploadedFileName;
if (changed.bio) body.bio = bioInput.value;
if (changed.experience) body.yearsOfExperience = Number(experienceInput.value);
if (changed.isActive !== undefined) body.isActive = activeToggle.checked;
if (changed.availabilities) {
  body.availabilities = workingHours.map(wh => ({
    dayOfWeek: wh.dayOfWeek,
    startTime: wh.startTime + ":00",
    endTime: wh.endTime + ":00",
    slotDurationMinutes: wh.slotDurationMinutes || 30
  }));
}

const res = await fetch(`/api/v1/doctors/${doctorId}`, {
  method: "PUT",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body)
});
```

### Notes for UI

- All fields are optional — only send the ones that changed. The server ignores `null`/omitted fields.
- `availabilities` replaces ALL existing records when sent. If the user doesn't modify the schedule, **omit the field** or send the current values.
- The `id` in each availability response entry is for reference only — you don't need to send it back in the update request.
- `dayOfWeek` is integer (0-6), `gender` is integer (1-2).
- Time format is **24-hour** `HH:mm:ss` — append `:00` for seconds.
- `doctorImage` is a filename string, not a full URL.
- Email and phone are validated for uniqueness — if already taken by another user, the update is rejected.
