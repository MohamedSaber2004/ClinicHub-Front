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
| **السجل الطبي الإلكتروني الشامل (Medical Records / e-Rx)** | `/Clinic/MedicalRecords` | 🟢 **متصل بالـ API (بيانات المرضى الحقيقية)** | `GET /api/v1/doctors/patients` + رابط لسجل الزيارات الحي | `IDoctorDashboardService.GetPatientsAsync` |
| **إدارة بوابة المرضى (Patient Portal Admin)** | `/Clinic/PatientPortal` | 🟡 **تصميم فقط (Static / Mock)** | المحادثات: `GET /api/v1/conversations` ✅ موجود — الإحصائيات والتوغلز: ❌ غير موجودة | يعود مباشرة `View()` |

> **ملاحظة مراجعة (Backend Audit):** تم فحص مشروع الـ Backend (`E:\ClinicHub`) بالكامل والبحث عن
> `MedicalRecord | Prescription | eRx | ERX | EMR` → **صفر نتائج**.
> لا يوجد Controller أو Entity أو Feature خاص بالسجل الطبي أو الروشتة الإلكترونية.

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
- **`PatientPortal.cshtml` (`/Clinic/PatientPortal`)**:
  - The controller action `ClinicController.PatientPortal()` returns `View()` directly.
  - **تحديث مهم (تصحيح للتقرير السابق):** الادعاء بأنه "لا يوجد endpoint للمحادثات" **لم يعد صحيحاً** —
    الـ Backend يحتوي على `ConversationsController` كامل بـ 9 endpoints:
    ```
    GET    /api/v1/conversations                              (قائمة محادثات paginated)
    GET    /api/v1/conversations/{id}
    POST   /api/v1/conversations/create
    PUT    /api/v1/conversations/{id}/update
    PUT    /api/v1/conversations/{id}/settings
    GET    /api/v1/conversations/{conversationId}/messages
    POST   /api/v1/conversations/{conversationId}/messages
    DELETE /api/v1/conversations/messages/{messageId}
    DELETE /api/v1/conversations/{id}
    ```
  - ما زال مفقوداً في الـ Backend: endpoint لإحصائيات البوابة (المرضى النشطين، التحاليل المرفوعة، نسبة التقييمات) وendpoint لتفعيل/تعطيل خدمات البوابة (Toggles).

### 3. MedicalRecords — تم الربط بالبيانات الحقيقية ✅ (تحديث)
- `ClinicController.MedicalRecords(searchTerm, pageNumber, pageSize)` أصبحت تستهلك `_doctorDashboardService.GetPatientsAsync()` → `GET api/v1/doctors/patients` بنفس نمط صفحة مرضى العيادة (بحث + pagination + معالجة أخطاء ثلاثية الطبقات).
- تم حذف كل البيانات الثابتة من العرض: أكواد `#EMR-xxxx` الوهمية، التشخيصات التجريبية، شارات e-Rx، زر "إنشاء سجل جديد" الميت، وقائمة العيادات الثابتة.
- الجدول الآن يعرض بيانات حقيقية فقط: المريض، العمر/الجنس، عدد الزيارات، آخر زيارة — مع زر ينقل إلى السجل الطبي الحي لكل مريض (`DoctorPatientHistory`).
- أعمدة التشخيص وe-Rx لن تعود إلا بعد أن يوفر الـ Backend endpoints الخاصة بها (قسم ب أدناه).

---

## Backend Audit — Missing Endpoints (E:\ClinicHub)

### أ. MedicalRecords / EMR — مفقود بالكامل ❌

ما تحتاجه الصفحة مقابل الموجود فعلياً:

| عمود الجدول في الواجهة | متوفر حالياً؟ | المصدر |
|---|---|---|
| رقم السجل (EMR Code) | ❌ لا يوجد مفهوم EMR Code في الـ Backend | — |
| المريض + العمر | ✅ نعم | `GET api/v1/doctors/patients` → `DoctorPatientDto` |
| آخر زيارة | ✅ نعم | نفس الـ DTO أعلاه (`LastVisitDate`) |
| التشخيص الأخير | ⚠️ جزئياً | لا يوجد حقل Diagnosis مستقل — الأقرب هو `Appointment.ChronicDiseases` (نص حر) عبر `GET api/v1/doctors/patients/{patientId}/history` |
| روشتة إلكترونية e-Rx | ❌ لا يوجد أي Entity أو Endpoint | — |

**أقرب تغطية موجودة اليوم:**
- `GET api/v1/doctors/patients` → يغطي اسم المريض، العمر، عدد الزيارات، آخر زيارة (4 من 7 أعمدة).
- `GET api/v1/doctors/patients/{patientId}/history` → الأمراض المزمنة فقط، بدون تشخيص أو روشتة.

### ب. Endpoints المقترح طلبها من فريق الـ Backend

**المسار الكامل (Full EMR):**
```
GET    /api/v1/emr/records?searchTerm=&clinicId=&page=     → قائمة سجلات + آخر تشخيص + ملخص e-Rx
POST   /api/v1/emr/records                                 → إنشاء سجل
GET    /api/v1/emr/records/{id}                            → تفاصيل السجل
PUT    /api/v1/emr/records/{id}                            → تعديل
GET    /api/v1/emr/records/{id}/prescriptions              → قائمة الروشتات
POST   /api/v1/emr/records/{id}/prescriptions              → إنشاء روشتة (بقائمة أدوية)
```

**المسار البسيط (Minimal):**
إضافة حقلي `Diagnosis` + مجموعة `Prescriptions` على Entity الـ `Appointment` وتوسيع `PatientHistoryDto` —
بدون إنشاء نظام EMR منفصل، مع الاستفادة من `GetPatientsAsync` + `GetPatientHistoryAsync` الموجودين.

### ج. PatientPortal — ما زال مفقوداً بعد وجود المحادثات
- `GET /api/v1/portal/stats` → مرضى نشطون على التطبيق، تحاليل مرفوعة إلكترونياً، نسبة تقييمات إيجابية.
- `PUT /api/v1/portal/settings` → toggles تفعيل عرض الروشتات / المحادثات / رفع نتائج المعامل.

---

## Recommended Next Steps

1. **MedicalRecords — Option A (Partial connect now)**: Update `MedicalRecords.cshtml` and `ClinicController.MedicalRecords` to consume the live `_doctorDashboardService.GetPatientsAsync()` so it lists real patients with links to their live histories (covers patient/age/last-visit columns; diagnosis & e-Rx stay mock until backend ships EMR endpoints).
2. **PatientPortal — Option A (Connect chats now)**: Wire the chat section to the existing `ConversationsController` endpoints (`GET /api/v1/conversations` + `POST .../messages`) — no new backend work required.
3. **Request from Backend Team**: The full EMR endpoints listed in section (ب) above, plus portal stats/settings endpoints.
4. **Option B (Either page)**: Keep rich mock UIs with structured data in `Data/MockData.cs` until the backend exposes the missing endpoints.
