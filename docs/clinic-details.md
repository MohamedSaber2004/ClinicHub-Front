# Clinic Details Endpoint — Frontend Integration Guide

## `GET /api/v1/admin/clinics/{id}/details`

Fetches full clinic profile including doctors, staff, and ratings in a single call.

---

### Authentication

Requires `Bearer` token with `SuperAdmin` or `ClinicOwner` role.

```
Authorization: Bearer <token>
```

---

### Response (200 OK)

```json
{
  "statusCode": 200,
  "message": "Success",
  "data": {
    "id": "guid",
    "name": "عيادة الأسنان الحديثة",
    "nameAr": "عيادة الأسنان الحديثة",
    "description": "Modern dental clinic",
    "arDescription": "عيادة أسنان متكاملة",
    "address": "Cairo, Egypt",
    "addressAr": "القاهرة، مصر",
    "phone": "01012345678",
    "email": "clinic@example.com",
    "website": "https://example.com",
    "logo": "https://cdn.example.com/logo.png",
    "workingHours": "9:00 AM - 9:00 PM",
    "workingDays": [
      { "dayOfWeek": "Saturday", "startTime": "09:00:00", "endTime": "21:00:00" },
      { "dayOfWeek": "Sunday", "startTime": "09:00:00", "endTime": "21:00:00" }
    ],
    "lat": 30.0444,
    "lng": 31.2357,
    "isRegistered": true,
    "status": "Active",
    "isActive": true,
    "specializationId": "guid",
    "specializationName": "Dentistry",
    "specializationNameAr": "طب الأسنان",
    "imageUrl": null,
    "clinicAdminId": "guid",
    "ownerName": "Ahmed Ali",
    "ownerEmail": "ahmed@example.com",
    "ownerPhone": "01012345678",
    "subscriptionStatus": "Active",
    "createdAt": "2026-07-01T10:00:00Z",
    "updatedAt": "2026-07-15T14:30:00Z",
    "createdBy": "system",
    "updatedBy": "admin-id",

    "doctors": [
      {
        "id": "guid",
        "name": "Dr. Sara Mohamed",
        "image": "https://cdn.example.com/doctor.jpg",
        "phone": "01012345678",
        "email": "sara@clinic.com",
        "specializationArName": "طب الأسنان",
        "specializationEnName": "Dentistry",
        "bio": "Specialist in cosmetic dentistry",
        "yearsOfExperience": 8
      }
    ],

    "staff": [
      {
        "id": "guid",
        "fullName": "Mona Hassan",
        "email": "mona@clinic.com",
        "phoneNumber": "01098765432",
        "isActive": true,
        "createdAt": "2026-07-01T10:00:00Z"
      }
    ],

    "averageRating": 4.5,
    "totalRatings": 12,
    "recentRatings": [
      {
        "id": "guid",
        "userId": "guid",
        "userName": "Khaled",
        "doctorId": null,
        "clinicId": "guid",
        "value": 5,
        "review": "Great experience",
        "createdAt": "2026-07-20T15:00:00Z"
      }
    ]
  }
}
```

---

### Error Responses

| Status | Meaning |
|--------|---------|
| **401** | Missing or invalid token |
| **403** | Logged in but not `SuperAdmin` or `ClinicOwner` |
| **404** | Clinic not found (`{ "message": "Clinic not found" }`) |

---

### Nullable Fields

| Field | Null when |
|-------|-----------|
| `doctors` | Clinic has no doctors |
| `staff` | Clinic has no staff |
| `averageRating` | Clinic has no ratings yet |
| `recentRatings` | Clinic has no ratings yet |

---

### Notes for Frontend

- Ratings list returns the **last 10** reviews ordered by most recent.
- `averageRating` is rounded to 1 decimal place.
- Doctors list returns **only non-deleted** doctors.
- Staff is determined by `ApplicationUser` records with role `Staff` and matching `ClinicId`.
- To display the star rating, use `averageRating` (out of 5) and show `totalRatings` count beside it (e.g., "★ 4.5 (12 reviews)").
