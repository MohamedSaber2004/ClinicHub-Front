# ClinicHub — Frontend Integration Guide

This guide provides a comprehensive walkthrough for integrating the **ClinicHub** backend with your frontend (React, Next.js, Vue, or Mobile apps). It covers clinic registration, superadmin approval, subscription payments via Paymob, JWT decoding, route protection, and error handling.

---

## 🗺️ System Architecture & Workflow

```
[ New Clinic Owner ]
       │
       ▼
 1. Register Clinic & Owner  ────────►  POST /api/v1/clinics/register
       │                                Status: 202 Accepted (Pending Approval)
       ▼
 2. SuperAdmin Approves      ────────►  POST /api/v1/admin/dashboard/verifications/{userId}/approve
       │                                (Activates User AND Clinic)
       ▼
 3. Login to Web Dashboard   ────────►  POST /api/v1/auth/login-web
       │                                ➔ If NO Subscription: HTTP 403 (Returns draft JWT tokens)
       ▼
 4. View Plans & Subscribe   ────────►  GET  /api/v1/plans
       │                                POST /api/v1/subscriptions/initiate-payment
       ▼
 5. Complete Paymob Payment  ────────►  Redirect to Paymob Checkout URL
       │                                Returns to /payments/result?success=true
       ▼
 6. Refresh JWT Token        ────────►  POST /api/v1/auth/refresh-token
       │                                (New JWT has "HasActiveSubscription": "True")
       ▼
 7. Full Access Unlocked! 🎉
```

---

## 🔐 1. Authentication & JWT Structure

### Decoding Access Tokens
Every JWT access token returned upon login or token refresh contains essential claims:

```json
{
  "sub": "user-guid-id",
  "email": "doctor@clinic.com",
  "role": "ClinicOwner",
  "ClinicId": "clinic-guid-id",
  "HasActiveSubscription": "True",
  "exp": 1784851200
}
```

### Checking Active Subscription in Frontend (JS / TS)
```typescript
import { jwtDecode } from 'jwt-decode';

interface CustomJwtPayload {
  sub: string;
  role: string;
  ClinicId?: string;
  HasActiveSubscription?: string;
}

export function checkSubscriptionStatus(token: string): boolean {
  try {
    const decoded = jwtDecode<CustomJwtPayload>(token);
    return decoded.HasActiveSubscription === 'True';
  } catch {
    return false;
  }
}
```

---

## 🔄 2. End-to-End User Integration Flow

### Step 1: Clinic Registration
Public endpoint — allows clinic owners to register themselves and their clinic details in a single step.

- **Endpoint**: `POST /api/v1/clinics/register`
- **Headers**: `Content-Type: application/json`
- **Body**:
  ```json
  {
    "fullName": "Dr. John Doe",
    "email": "john@clinic.com",
    "phoneNumber": "01012345678",
    "password": "Password123!",
    "clinicName": "Hope Clinic",
    "clinicAddress": "123 Main St, Cairo",
    "specializationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "professionalPracticeCardImage": "https://storage.../card.jpg",
    "taxCardImage": "https://storage.../tax.jpg",
    "unionIdCardImage": "https://storage.../union.jpg",
    "doctorImage": "https://storage.../doctor.jpg"
  }
  ```
- **Response** (`202 Accepted`):
  ```json
  {
    "success": true,
    "message": "Registration submitted for admin approval.",
    "data": {
      "userId": "guid-id",
      "isPendingApproval": true
    }
  }
  ```
- **Frontend Action**: Show "Registration Submitted — Awaiting SuperAdmin Approval" page.

---

### Step 2: SuperAdmin Approval (Admin Panel)
SuperAdmin reviews pending clinic verifications and approves them.

- **Fetch Pending Verifications**: `GET /api/v1/admin/dashboard/verifications/pending`
- **Approve Verification**: `POST /api/v1/admin/dashboard/verifications/{userId}/approve`
- **Backend Effect**: 
  - Sets `user.IsActive = true`
  - Sets `clinic.Status = ClinicStatus.Active`
  - Creates linked `Doctor` entity.

---

### Step 3: Web Login & Subscription Detection
After SuperAdmin approval, the user logs in via `login-web`.

- **Endpoint**: `POST /api/v1/auth/login-web`
- **Body**:
  ```json
  {
    "email": "john@clinic.com",
    "password": "Password123!"
  }
  ```

#### Handling the Login Response:

1. **Scenario A — Account Still Pending Approval (HTTP 403)**:
   `data` is `null`. Show "Account is under review by admin."

2. **Scenario B — Approved but No Active Subscription (HTTP 403)**:
   `data` contains valid tokens (`accessToken`, `refreshToken`)!
   ```json
   {
     "success": false,
     "message": "An active subscription is required to access the dashboard.",
     "data": {
       "accessToken": "eyJhbGci...",
       "refreshToken": "xyz123...",
       "roles": "ClinicOwner",
       "clinicId": "clinic-guid-id"
     },
     "statusCode": 403
   }
   ```
   **Frontend Action**: Store the tokens in `localStorage` / state and redirect immediately to `/plans` or `/subscribe`.

3. **Scenario C — Active Subscription Exists (HTTP 200)**:
   Full access granted. Redirect to `/dashboard`.

```typescript
async function handleWebLogin(credentials) {
  const response = await fetch('/api/v1/auth/login-web', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(credentials)
  });

  const resData = await response.json();

  if (response.status === 200) {
    saveTokens(resData.data.accessToken, resData.data.refreshToken);
    window.location.href = '/dashboard';
  } else if (response.status === 403) {
    if (resData.data?.accessToken) {
      // Approved by admin, but needs subscription!
      saveTokens(resData.data.accessToken, resData.data.refreshToken);
      window.location.href = '/subscription-required';
    } else {
      // Account still pending approval
      showToast('Your account is awaiting SuperAdmin verification.');
    }
  } else {
    showToast(resData.message || 'Invalid credentials.');
  }
}
```

---

### Step 4: Plan Selection & Initiating Payment
To subscribe, fetch available plans and send an initiation request.

#### 1. Fetch Plans
- **Endpoint**: `GET /api/v1/plans` (Public)
- **Response**:
  ```json
  [
    {
      "id": "b3a12345-...",
      "name": "Standard",
      "priceMonthly": 1000,
      "priceYearly": 10000,
      "maxDoctors": 5,
      "maxStaff": 15,
      "features": "[\"appointments\",\"patient_records\",\"basic_reports\"]"
    }
  ]
  ```

#### 2. Initiate Paymob Payment
- **Endpoint**: `POST /api/v1/subscriptions/initiate-payment`
- **Headers**: `Authorization: Bearer <accessToken>`
- **Body**:
  ```json
  {
    "planId": "b3a12345-...",
    "period": 0  // 0 = Monthly, 1 = Yearly
  }
  ```
- **Response** (`200 OK`):
  ```json
  {
    "paymentId": "payment-guid",
    "paymobRedirectUrl": "https://accept.paymob.com/unifiedcheckout/?paymentKey=...",
    "planName": "Standard",
    "amount": 1000,
    "currency": "EGP"
  }
  ```

#### 3. Redirect User to Paymob
```typescript
window.location.href = responseData.paymobRedirectUrl;
```

---

### Step 5: Handling Paymob Payment Callback
After completing checkout on Paymob, Paymob redirects the user back to your frontend result route (e.g., `/payments/result`):

```typescript
// On /payments/result page mount:
const urlParams = new URLSearchParams(window.location.search);
const isSuccess = urlParams.get('success') === 'true';

if (isSuccess) {
  // 1. Refresh the JWT to include HasActiveSubscription: "True"
  await handleRefreshToken();
  // 2. Redirect to dashboard
  window.location.href = '/dashboard';
} else {
  showError('Payment failed or was cancelled. Please try again.');
  window.location.href = '/plans';
}
```

---

### Step 6: Refreshing Token After Payment
Call the refresh token endpoint to get an updated JWT token containing `"HasActiveSubscription": "True"`:

- **Endpoint**: `POST /api/v1/auth/refresh-token`
- **Body**:
  ```json
  {
    "refreshToken": "stored-refresh-token"
  }
  ```
- **Response** (`200 OK`): Returns new `accessToken` and `refreshToken`.

---

## 🛡️ 3. Client-Side Route Protection (React / Next.js Example)

```tsx
import React from 'react';
import { Navigate } from 'react-router-dom';
import { checkSubscriptionStatus } from './authUtils';

export const ProtectedDashboardRoute = ({ children }: { children: React.ReactNode }) => {
  const token = localStorage.getItem('accessToken');

  if (!token) {
    return <Navigate to="/login" replace />;
  }

  const hasSubscription = checkSubscriptionStatus(token);

  if (!hasSubscription) {
    return <Navigate to="/subscription-required" replace />;
  }

  return <>{children}</>;
};
```

---

## 🌐 4. Multi-Language Support (Localization)

Send the `Accept-Language` header in all API requests to receive pre-translated messages:

- Arabic (Default): `Accept-Language: ar`
- English: `Accept-Language: en`

```typescript
const headers = {
  'Content-Type': 'application/json',
  'Accept-Language': currentLocale, // 'ar' or 'en'
  'Authorization': `Bearer ${accessToken}`
};
```

---

## 📊 5. Summary Table of API Endpoints

| Action | HTTP Method | Endpoint | Auth Required |
| :--- | :--- | :--- | :--- |
| **Get Plans** | `GET` | `/api/v1/plans` | ❌ No |
| **Register Clinic** | `POST` | `/api/v1/clinics/register` | ❌ No |
| **Web Login** | `POST` | `/api/v1/auth/login-web` | ❌ No |
| **Refresh Token** | `POST` | `/api/v1/auth/refresh-token` | ❌ No |
| **Initiate Payment** | `POST` | `/api/v1/subscriptions/initiate-payment` | 🔐 Yes (`ClinicOwner`) |
| **My Subscription** | `GET` | `/api/v1/subscriptions/my` | 🔐 Yes (`ClinicOwner`) |
| **Cancel Subscription**| `POST` | `/api/v1/subscriptions/my/cancel` | 🔐 Yes (`ClinicOwner`) |
| **Approve Verification**| `POST` | `/api/v1/admin/dashboard/verifications/{userId}/approve` | 🔐 Yes (`SuperAdmin`) |
| **Approve Clinic** | `POST` | `/api/v1/admin/dashboard/clinics/{clinicId}/approve` | 🔐 Yes (`SuperAdmin`) |
