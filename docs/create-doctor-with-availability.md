# POST Create Doctor with Availability

Creates a new `ApplicationUser` + `Doctor` + `DoctorAvailability` records in a single transaction. The user is auto-assigned the `Doctor` role.

---

## Routes

| Role | Method | Route |
|---|---|---|
| `SuperAdmin` | POST | `/api/v1/admin/clinics/{clinicId:guid}/doctors` |
| `ClinicOwner` | POST | `/api/v1/admin/clinics/doctors` |

For `ClinicOwner` the `ClinicId` is taken from the current user's assigned clinic — ignore it in the request body.

---

## Request Body

```json
{
  "clinicId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "specializationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fullName": "Ahmed Ali",
  "email": "ahmed@example.com",
  "phoneNumber": "+201234567890",
  "password": "P@ssw0rd",
  "birthDate": "1990-05-15",
  "gender": 1,
  "bio": "Cardiologist with 10 years of experience.",
  "yearsOfExperience": 10,
  "doctorImage": "doctor_avatar_abc123.jpg",
  "availabilities": [
    {
      "dayOfWeek": 0,
      "startTime": "09:00:00",
      "endTime": "17:00:00",
      "slotDurationMinutes": 30
    },
    {
      "dayOfWeek": 2,
      "startTime": "10:00:00",
      "endTime": "14:00:00",
      "slotDurationMinutes": 30
    }
  ]
}
```

### Field reference

| Field | Type | Required | Notes |
|---|---|---|---|
| `clinicId` | `guid` | Yes (except ClinicOwner) | Ignored for ClinicOwner route |
| `specializationId` | `guid` | Yes | Must exist and not be soft-deleted |
| `fullName` | `string` | Yes | |
| `email` | `string` | Yes | Validated for uniqueness; checked against existing users (including soft-deleted) |
| `phoneNumber` | `string` | Yes | Validated for uniqueness among non-deleted users |
| `password` | `string` | Yes | Min 6 characters |
| `gender` | `int` | Yes | `1` = Male, `2` = Female |
| `birthDate` | `date` | No | ISO 8601 (`yyyy-MM-dd`) |
| `bio` | `string` | No | |
| `yearsOfExperience` | `int` | No | Default `0` |
| `doctorImage` | `string` | No | Filename only (not full URL); stored as `ApplicationUser.ProfilePictureUrl` |
| `availabilities` | `array` | No | Replaces ALL existing availability records if sent |

#### Availability item

| Field | Type | Required | Notes |
|---|---|---|---|
| `dayOfWeek` | `int` | Yes | `0`=Sunday … `6`=Saturday |
| `startTime` | `time` | Yes | `HH:mm:ss` (24-hour) |
| `endTime` | `time` | Yes | Must be after `startTime` |
| `slotDurationMinutes` | `int` | No | Default `30`; max `480` |

---

## Response `201 Created`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userName": "Ahmed Ali",
  "userEmail": "ahmed@example.com",
  "userPhoneNumber": "+201234567890",
  "profilePictureUrl": "doctor_avatar_abc123.jpg",
  "clinicId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clinicName": null,
  "specializationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "specializationName": null,
  "bio": "Cardiologist with 10 years of experience.",
  "yearsOfExperience": 10,
  "isActive": true,
  "createdAt": "2026-07-29T12:00:00",
  "availabilities": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "dayOfWeek": 0,
      "startTime": "09:00:00",
      "endTime": "17:00:00",
      "slotDurationMinutes": 30
    },
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "dayOfWeek": 2,
      "startTime": "10:00:00",
      "endTime": "14:00:00",
      "slotDurationMinutes": 30
    }
  ]
}
```

### Response fields

| Field | Type | Notes |
|---|---|---|
| `id` | `guid` | Doctor ID |
| `userId` | `guid` | Linked ApplicationUser ID |
| `userName` | `string` | From `ApplicationUser.FullName` |
| `userEmail` | `string` | From `ApplicationUser.Email` |
| `userPhoneNumber` | `string` | From `ApplicationUser.PhoneNumber` |
| `profilePictureUrl` | `string` | From `ApplicationUser.ProfilePictureUrl` (filename only) |
| `clinicId` | `guid` | |
| `clinicName` | `string` | Mapped from `Clinic.Name` (null until explicitly included) |
| `specializationId` | `guid` | |
| `specializationName` | `string` | Mapped from `Specialization.Name` (null until explicitly included) |
| `bio` | `string` | |
| `yearsOfExperience` | `int` | |
| `isActive` | `bool` | |
| `createdAt` | `datetime` | |
| `availabilities` | `array` | List of availability slots |

---

## Errors

| Status | Message | When |
|---|---|---|
| `400` | Clinic not found | `ClinicId` doesn't exist |
| `400` | Specialization not found | `SpecializationId` doesn't exist |
| `400` | Email/Phone already exists | User with same email or phone (non-deleted) already in DB |
| `400` | User is already a doctor in this clinic | Same `UserId` + `ClinicId` combination already exists |
| `400` | Identity errors | Password too weak, etc. |

Validation is handled by `CreateDoctorWithAvailabilityCommandValidator` (FluentValidation pipeline). Handler wraps all DB writes in `BeginTransaction/Commit/Rollback`.
