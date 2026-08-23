# EMR & Patient Records — API Connectivity Analysis

## Goal Description
Detailed audit of API integrations across all Electronic Medical Records (EMR) and patient-related pages in the ClinicHub ecosystem.

---

## Connectivity Summary Matrix

| Page / Feature | Route | Status | Backend API Endpoint | Service Contract |
|---|---|---|---|---|
| **مرضى العيادة (Patients List)** | `/Clinic/DoctorPatients`<br>`/Doctor/Patients` | 🟢 **متصل بالـ API بالكامل** | `GET /api/v1/doctors/patients` | `IDoctorDashboardService.GetPatientsAsync` |
| **تاريخ وسجل زيارات المريض (Visit History)** | `/Clinic/DoctorPatientHistory/{id}`<br>`/Doctor/PatientHistory/{id}` | 🟢 **متصل بالـ API بالكامل** | `GET /api/v1/doctors/patients/{id}/history` | `IDoctorDashboardService.GetPatientHistoryAsync` |
| **تسجيل مريض جديد وفتح ملف (Patient Intake)** | `/Staff/RegisterPatient` | 🟢 **متصل بالـ API بالكامل** | `POST /api/v1/patients/register` | `IStaffDashboardService.RegisterPatientAsync` |
| **السجل الطبي الإلكتروني الشامل (Medical Records / e-Rx)** | `/Clinic/MedicalRecords` | 🟡 **تصميم فقط (Static / Mock)** | *لا يوجد endpoint مخصص للـ e-Rx حالياً* | يعود مباشرة `View()` |
| **إدارة بوابة المرضى (Patient Portal Admin)** | `/Clinic/PatientPortal` | 🟡 **تصميم فقط (Static / Mock)** | *لا يوجد endpoint مخصص للمحادثات* | يعود مباشرة `View()` |

---

## Detailed Breakdown

### 1. Pages Fully Connected to Live Backend API 🟢
- **`DoctorPatients` (`/Clinic/DoctorPatients` & `/Doctor/Patients`)**:
  - Live search and pagination.
  - Returns `PagginatedResult<DoctorPatientDto>` containing patient full names, ages, genders, total visits count, and last visit dates.
- **`DoctorPatientHistory` (`/Clinic/DoctorPatientHistory/{patientId}` & `/Doctor/PatientHistory/{patientId}`)**:
  - Fetches the patient's full medical visit history.
  - Returns `PagginatedResult<PatientHistoryDto>` containing appointment date, time, consultation type (home vs. clinic), chief complaint (الشكوى), chronic diseases (الأمراض المزمنة), and status.
- **`RegisterPatient` (`/Staff/RegisterPatient`)**:
  - Sends `POST /api/v1/patients/register` to create a new patient profile and assign an initial medical file.

---

### 2. Pages Currently Design / Mock Only 🟡
- **`MedicalRecords.cshtml` (`/Clinic/MedicalRecords`)**:
  - The controller action [`ClinicController.MedicalRecords()`](file:///D:/Programing/Projects/doctory/ClinicHub-Front/ClinicHub/Controllers/ClinicController.cs#L444-L447) returns `View()` directly.
  - The view displays static mock rows (EMR codes `#EMR-2041`, latest diagnoses, and e-Prescription badges `e-Rx`).
  - There is currently no dedicated backend endpoint for comprehensive e-Prescriptions or diagnoses CRUD.

---

## Recommended Next Steps

1. **Option A (Connect to existing Patients API)**: We can update `MedicalRecords.cshtml` and `ClinicController.MedicalRecords` to consume the live `_doctorDashboardService.GetPatientsAsync()` so that it lists all real patients in the clinic with links to their live medical histories.
2. **Option B (Maintain as Rich Mock UI)**: Keep the advanced EMR layout (e-Rx, diagnoses, search filters) with structured mock data in `Data/MockData.cs` until the backend team exposes full EMR endpoints.
