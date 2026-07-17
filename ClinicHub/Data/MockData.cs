namespace ClinicHub.Data
{
    // --- Dashboard Models ---
    public class MockStat
    {
        public string Value { get; set; } = "";
        public string Label { get; set; } = "";
        public string IconColor { get; set; } = "primary";
        public string SvgPath { get; set; } = "";
    }

    public class MockQuickStatus
    {
        public string Text { get; set; } = "";
        public string Color { get; set; } = "success";
    }

    public class MockTableRow
    {
        public Dictionary<string, string> Cells { get; set; } = new();
    }

    public class MockPayment
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Payer { get; set; } = "";
        public string Type { get; set; } = "";
        public string TypeClass { get; set; } = "badge-info";
        public string Amount { get; set; } = "";
        public string Method { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusClass { get; set; } = "badge-success";
        public string Date { get; set; } = "";
    }

    public class MockPaymentDetail
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Payer { get; set; } = "";
        public string PayerType { get; set; } = "";
        public string PayerEmail { get; set; } = "";
        public string PayerPhone { get; set; } = "";
        public string Type { get; set; } = "";
        public string TypeClass { get; set; } = "badge-info";
        public string Amount { get; set; } = "";
        public string Method { get; set; } = "";
        public string TransactionId { get; set; } = "";
        public string RefNumber { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusClass { get; set; } = "badge-success";
        public string Date { get; set; } = "";
        public string ItemName { get; set; } = "";
        public List<MockTimelineEntry> Timeline { get; set; } = new();
        public string Notes { get; set; } = "";
    }

    public class MockTimelineEntry
    {
        public string Date { get; set; } = "";
        public string Text { get; set; } = "";
        public string Marker { get; set; } = "info";
    }

    public class MockSubscriptionPlan
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Duration { get; set; } = "";
        public string Price { get; set; } = "";
        public string Currency { get; set; } = "ريال";
        public string Badge { get; set; } = "";
        public string PlanClass { get; set; } = "";
        public string[] Features { get; set; } = Array.Empty<string>();
        public bool[] FeatureEnabled { get; set; } = Array.Empty<bool>();
        public bool IsActive { get; set; } = true;
    }

    public class MockClinicRegistration
    {
        public int Id { get; set; }
        public string ClinicName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public double Latitude { get; set; } = 31.0409;
        public double Longitude { get; set; } = 31.3785;
        public string ResponsibleDoctor { get; set; } = "";
        public string LicenseNumber { get; set; } = "";
        public int PackageId { get; set; }
        public string PackageName { get; set; } = "";
        public List<string> Documents { get; set; } = new();
        public string Status { get; set; } = "pending"; // pending, approved, rejected
        public string StatusClass { get; set; } = "badge-warning";
        public string SubmittedAt { get; set; } = "";
        public string? ReviewedAt { get; set; }
        public string? AdminNotes { get; set; }
    }

    public static class MockData
    {
        public const string CurrencySymbol = "ج.م";
        // ========== Dashboard ==========
        public static List<MockStat> GetDashboardStats() => new()
        {
            new() { Value = "156", Label = "إجمالي طلبات التحقق", IconColor = "primary", SvgPath = "M12 2C9.243 2 7 4.243 7 7v2c0 2.206 1.794 4 4 4v2.184c-2.804.418-4.994 2.617-4.994 5.316V22h2v-1.5c0-2.206 1.794-4 4-4s4 1.794 4 4V22h2v-1.5c0-2.699-2.19-4.898-4.994-5.316V13c2.206 0 4-1.794 4-4V7c0-2.757-2.243-5-5-5zM9 7c0-1.654 1.346-3 3-3s3 1.346 3 3v2c0 1.654-1.346 3-3 3s-3-1.346-3-3V7z" },
            new() { Value = "24", Label = "العيادات النشطة", IconColor = "blue", SvgPath = "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V5h14v14zm-7-2h2v-4h4v-2h-4V7h-2v4H8v2h4z" },
            new() { Value = "6", Label = "التخصصات الطبية", IconColor = "green", SvgPath = "M21.41 11.58l-9-9C12.05 2.22 11.55 2 11 2H4c-1.1 0-2 .9-2 2v7c0 .55.22 1.05.59 1.42l9 9c.36.36.86.58 1.41.58.55 0 1.05-.22 1.41-.59l7-7c.37-.36.59-.86.59-1.41 0-.55-.23-1.06-.59-1.42zM5.5 7C4.67 7 4 6.33 4 5.5S4.67 4 5.5 4 7 4.67 7 5.5 6.33 7 5.5 7z" },
            new() { Value = "2,847", Label = "إجمالي المستخدمين", IconColor = "primary", SvgPath = "M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5s-3 1.34-3 3 1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 2 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z" },
            new() { Value = "3", Label = "تذاكر مفتوحة", IconColor = "primary", SvgPath = "M19 12v-2c0-3.866-3.134-7-7-7S5 6.134 5 10v2H3v7h3v-9c0-3.313 2.687-6 6-6s6 2.687 6 6v9h3v-7h-2zM7 15v2c0 1.103.897 2 2 2h2v-6H7v2zm8 0h-2v2h2c1.103 0 2-.897 2-2v-2h-2v2z" },
            new() { Value = "5", Label = "إعلانات مجدولة", IconColor = "blue", SvgPath = "M12 22c1.1 0 2-.9 2-2h-4c0 1.1.9 2 2 2zm6-6v-5c0-3.07-1.64-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.63 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2z" },
            new() { Value = "2", Label = "اشتراك منتهي", IconColor = "amber", SvgPath = "M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z" },
        };

        public static List<MockQuickStatus> GetQuickStatuses() => new()
        {
        };

        public static List<MockTableRow> GetDoctorsOnDuty() => new()
        {
            new() { Cells = new() { { "name", "د. عمار" }, { "role", "أستاذ" }, { "specialty", "الأمراض الجلدية" } } },
            new() { Cells = new() { { "name", "د. خان" }, { "role", "خبير طبي" }, { "specialty", "الأمراض الجلدية" } } },
            new() { Cells = new() { { "name", "د. عبد الله" }, { "role", "مسؤول اتصال" }, { "specialty", "أمراض الأعصاب" } } },
            new() { Cells = new() { { "name", "د. علياء" }, { "role", "متعاون" }, { "specialty", "طب الأسرة" } } },
        };

        public static List<MockTableRow> GetUrgentTickets() => new()
        {
            new() { Cells = new() { { "code", "#1024" }, { "subject", "تعطل نظام تسجيل الدخول" }, { "reporter", "عيادة السلام" }, { "priority", "عالية" }, { "priorityClass", "badge-danger" }, { "date", "2026-07-03" } } },
            new() { Cells = new() { { "code", "#1021" }, { "subject", "مشكلة في عرض التقارير" }, { "reporter", "عيادات التخصصات الدقيقة" }, { "priority", "عالية" }, { "priorityClass", "badge-danger" }, { "date", "2026-07-01" } } },
            new() { Cells = new() { { "code", "#1023" }, { "subject", "استفسار عن فاتورة شهر يونيو" }, { "reporter", "د. سارة أحمد" }, { "priority", "متوسطة" }, { "priorityClass", "badge-warning" }, { "date", "2026-07-02" } } },
        };

        public static List<MockTableRow> GetSubscribers() => new()
        {
            new() { Cells = new() { { "name", "مجمع عيادات السلام الطبي" }, { "start", "1 يناير 2026" }, { "end", "31 ديسمبر 2026" }, { "package", "باقة متقدمة" }, { "packageClass", "badge-info" }, { "doctors", "15" }, { "status", "نشط" }, { "statusClass", "badge-success" } } },
            new() { Cells = new() { { "name", "مستشفى النور التخصصي" }, { "start", "15 فبراير 2026" }, { "end", "14 فبراير 2027" }, { "package", "باقة متقدمة" }, { "packageClass", "badge-info" }, { "doctors", "32" }, { "status", "نشط" }, { "statusClass", "badge-success" } } },
            new() { Cells = new() { { "name", "عيادات الدكتور سمير" }, { "start", "1 مارس 2026" }, { "end", "30 سبتمبر 2026" }, { "package", "باقة أساسية" }, { "packageClass", "badge-success" }, { "doctors", "5" }, { "status", "نشط" }, { "statusClass", "badge-success" } } },
            new() { Cells = new() { { "name", "مركز رعاية اليوم الواحد" }, { "start", "1 يناير 2025" }, { "end", "31 ديسمبر 2025" }, { "package", "باقة أساسية" }, { "packageClass", "badge-success" }, { "doctors", "8" }, { "status", "منتهي" }, { "statusClass", "badge-danger" } } },
            new() { Cells = new() { { "name", "عيادات التخصصات الدقيقة" }, { "start", "1 أبريل 2026" }, { "end", "31 مارس 2027" }, { "package", "باقة متقدمة" }, { "packageClass", "badge-info" }, { "doctors", "22" }, { "status", "نشط" }, { "statusClass", "badge-success" } } },
        };

        // ========== Subscription Plans ==========
        public static List<MockSubscriptionPlan> GetActiveSubscriptionPlans() =>
            GetAllSubscriptionPlans().Where(p => p.IsActive).ToList();

        public static List<MockSubscriptionPlan> GetAllSubscriptionPlans() => new()
        {
            new()
            {
                Id = 1,
                Name = "الباقة الأساسية",
                Duration = "شهرياً",
                Price = "999",
                Currency = "ريال / شهرياً",
                Badge = "",
                PlanClass = "plan-free",
                IsActive = true,
                Features = new[]
                {
                    "إدراج العيادة في المنصة",
                    "إدارة حتى 5 أطباء",
                    "لوحة تحكم بسيطة",
                    "دعم فني عبر البريد الإلكتروني",
                    "تقرير شهري واحد",
                },
                FeatureEnabled = new[] { true, true, true, true, false }
            },
            new()
            {
                Id = 2,
                Name = "الباقة المتقدمة",
                Duration = "شهرياً",
                Price = "2,499",
                Currency = "ريال / شهرياً",
                Badge = "الأكثر طلباً",
                PlanClass = "plan-standard",
                IsActive = true,
                Features = new[]
                {
                    "إدراج العيادة في المنصة",
                    "إدارة حتى 15 طبيب",
                    "لوحة تحكم متكاملة",
                    "إدارة المواعيد والحجوزات",
                    "تقارير وإحصائيات متقدمة",
                    "دعم فني عبر الهاتف والبريد",
                    "إعلانات ترويجية مجانية",
                },
                FeatureEnabled = new[] { true, true, true, true, true, true, false }
            },
            new()
            {
                Id = 3,
                Name = "الباقة الاحترافية",
                Duration = "سنوياً",
                Price = "19,999",
                Currency = "ريال / سنوياً",
                Badge = "وفر 33%",
                PlanClass = "plan-premium",
                IsActive = true,
                Features = new[]
                {
                    "إدراج العيادة في المنصة",
                    "عدد غير محدود من الأطباء",
                    "لوحة تحكم متكاملة",
                    "إدارة المواعيد والحجوزات",
                    "تقارير وإحصائيات متقدمة",
                    "السجلات الطبية الإلكترونية",
                    "إعلانات ترويجية مُمولة",
                    "دعم فني على مدار الساعة",
                    "مدير حساب مخصص",
                },
                FeatureEnabled = new[] { true, true, true, true, true, true, true, true, true }
            },
            new()
            {
                Id = 4,
                Name = "الباقة التجريبية",
                Duration = "شهر واحد",
                Price = "",
                Currency = "",
                Badge = "تجربة مجانية",
                PlanClass = "plan-free",
                IsActive = true,
                Features = new[]
                {
                    "إدراج العيادة في المنصة",
                    "إدارة حتى 3 أطباء",
                    "جميع ميزات الباقة الأساسية",
                },
                FeatureEnabled = new[] { true, true, true }
            },
            new()
            {
                Id = 5,
                Name = "الباقة القديمة",
                Duration = "شهرياً",
                Price = "1,499",
                Currency = "ريال / شهرياً",
                Badge = "",
                PlanClass = "plan-free",
                IsActive = false,
                Features = new[]
                {
                    "إدراج العيادة في المنصة",
                    "إدارة حتى 10 أطباء",
                    "لوحة تحكم متكاملة",
                },
                FeatureEnabled = new[] { true, true, true }
            },
        };

        // ========== Clinic Registrations ==========
        public static List<MockClinicRegistration> GetAllClinicRegistrations() => new()
        {
            new()
            {
                Id = 1,
                ClinicName = "مجمع عيادات النور الطبي",
                Email = "info@alnoor-clinic.com",
                Phone = "0501234567",
                Address = "الرياض - حي النور - شارع الملك عبد الله",
                ResponsibleDoctor = "د. أحمد السعيد",
                LicenseNumber = "LIC-2026-00421",
                PackageId = 2,
                PackageName = "الباقة المتقدمة",
                Documents = new() { "license_scan.pdf", "clinic_photo_1.jpg", "doctor_cert.pdf" },
                Status = "pending",
                StatusClass = "badge-warning",
                SubmittedAt = "2026-07-02",
            },
            new()
            {
                Id = 2,
                ClinicName = "عيادات الدكتور فهد التخصصية",
                Email = "info@drfahad-clinic.com",
                Phone = "0559876543",
                Address = "جدة - حي الشاطئ - طريق الكورنيش",
                ResponsibleDoctor = "د. فهد العتيبي",
                LicenseNumber = "LIC-2026-00389",
                PackageId = 1,
                PackageName = "الباقة الأساسية",
                Documents = new() { "license_scan.pdf", "partnership_deed.pdf" },
                Status = "pending",
                StatusClass = "badge-warning",
                SubmittedAt = "2026-07-01",
            },
            new()
            {
                Id = 3,
                ClinicName = "مركز إشراق الطبي",
                Email = "info@eshraq-med.com",
                Phone = "0591122334",
                Address = "الدمام - حي الفيصلية - شارع 14",
                ResponsibleDoctor = "د. ليلى الشمري",
                LicenseNumber = "LIC-2026-00345",
                PackageId = 3,
                PackageName = "الباقة الاحترافية",
                Documents = new() { "license_scan.pdf", "clinic_photo_1.jpg", "clinic_photo_2.jpg", "doctor_cert.pdf", "lab_license.pdf" },
                Status = "approved",
                StatusClass = "badge-success",
                SubmittedAt = "2026-06-28",
                ReviewedAt = "2026-06-30",
                AdminNotes = "تم مراجعة المستندات واعتمادها. في انتظار الدفع لتفعيل الاشتراك.",
            },
            new()
            {
                Id = 4,
                ClinicName = "عيادات التخصصات الدقيقة",
                Email = "info@specialized-clinic.com",
                Phone = "0588776655",
                Address = "مكة المكرمة - حي العزيزية",
                ResponsibleDoctor = "د. عبد الرحمن القحطاني",
                LicenseNumber = "LIC-2026-00210",
                PackageId = 2,
                PackageName = "الباقة المتقدمة",
                Documents = new() { "license_scan.pdf", "doctor_cert.pdf" },
                Status = "rejected",
                StatusClass = "badge-danger",
                SubmittedAt = "2026-06-25",
                ReviewedAt = "2026-06-27",
                AdminNotes = "المستندات المقدمة غير مكتملة. يرجى التواصل مع العيادة لإعادة التقديم.",
            },
            new()
            {
                Id = 5,
                ClinicName = "عيادة الأمل للأطفال",
                Email = "info@alamal-pediatrics.com",
                Phone = "0566554433",
                Address = "الخبر - حي العقربية",
                ResponsibleDoctor = "د. مريم الحسن",
                LicenseNumber = "LIC-2026-00512",
                PackageId = 3,
                PackageName = "الباقة الاحترافية",
                Documents = new() { "license_scan.pdf", "clinic_photo_1.jpg", "doctor_cert.pdf", "insurance_doc.pdf" },
                Status = "paid",
                StatusClass = "badge-success",
                SubmittedAt = "2026-06-20",
                ReviewedAt = "2026-06-22",
                AdminNotes = "تم اعتماد الطلب واستلام الدفع. تم تفعيل العيادة في النظام.",
            },
        };

        public static List<MockClinicRegistration> GetPendingClinicRegistrations() =>
            GetAllClinicRegistrations().Where(r => r.Status == "pending").ToList();

        public static int GetPendingClinicRegistrationsCount() =>
            GetAllClinicRegistrations().Count(r => r.Status == "pending");

        public static List<MockTableRow> GetActivityLog() => new()
        {
            new() { Cells = new() { { "date", "2026-07-03 14:30" }, { "action", "إضافة طبيب جديد" }, { "user", "المشرف العام" }, { "detail", "تم إضافة د. أحمد علي إلى عيادة القلب" } } },
            new() { Cells = new() { { "date", "2026-07-03 11:15" }, { "action", "تفعيل اشتراك" }, { "user", "مدير النظام" }, { "detail", "تم تفعيل اشتراك عيادات السلام حتى 2027-01-01" } } },
            new() { Cells = new() { { "date", "2026-07-02 09:45" }, { "action", "تحديث بيانات عيادة" }, { "user", "المشرف العام" }, { "detail", "تم تحديث معلومات عيادة العظام" } } },
            new() { Cells = new() { { "date", "2026-07-01 16:00" }, { "action", "تسجيل مريض جديد" }, { "user", "طاقم الاستقبال" }, { "detail", "تم تسجيل المريض محمد عمر في قسم القلب" } } },
            new() { Cells = new() { { "date", "2026-06-30 13:20" }, { "action", "إلغاء موعد" }, { "user", "د. سارة أحمد" }, { "detail", "تم إلغاء موعد المريض فهد الشمري" } } },
        };

        public static List<MockTableRow> GetNewPatients() => new()
        {
            new() { Cells = new() { { "name", "عزير" }, { "dept", "القلب والأوعية الدموية" }, { "date", "3 مارس 2025" }, { "phone", "01012345678" } } },
            new() { Cells = new() { { "name", "حارس" }, { "dept", "أمراض الأعصاب" }, { "date", "4 مارس 2025" }, { "phone", "01123456789" } } },
            new() { Cells = new() { { "name", "حمزة" }, { "dept", "أمراض النساء" }, { "date", "5 مارس 2025" }, { "phone", "01234567890" } } },
        };

        // ========== Payments ==========
        public static List<MockStat> GetPaymentStats() => new()
        {
            new() { Value = "284,700", Label = "إجمالي الإيرادات", IconColor = "primary", SvgPath = "M11.8 10.9c-2.27-.59-3-1.2-3-2.15 0-1.09 1.01-1.85 2.7-1.85 1.78 0 2.44.85 2.5 2.1h2.21c-.07-1.72-1.12-3.3-3.21-3.81V3h-3v2.16c-1.94.42-3.5 1.68-3.5 3.61 0 2.31 1.91 3.46 4.7 4.13 2.5.6 3 1.48 3 2.41 0 .69-.49 1.79-2.7 1.79-2.06 0-2.87-.92-2.98-2.1h-2.2c.12 2.19 1.76 3.42 3.68 3.83V21h3v-2.15c1.95-.37 3.5-1.5 3.5-3.55 0-2.84-2.43-3.81-4.7-4.4z" },
            new() { Value = "45,200", Label = "مدفوعات هذا الشهر", IconColor = "blue", SvgPath = "M19 3h-4.18C14.4 1.84 13.3 1 12 1c-1.3 0-2.4.84-2.82 2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-7 0c.55 0 1 .45 1 1s-.45 1-1 1-1-.45-1-1 .45-1 1-1zm-2 14l-4-4 1.41-1.41L10 14.17l6.59-6.59L18 9l-8 8z" },
            new() { Value = "3", Label = "معاملات معلقة", IconColor = "amber", SvgPath = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z" },
            new() { Value = "12%", Label = "نسبة العمولات", IconColor = "green", SvgPath = "M7 10l5 5 5-5z" },
        };

        public static List<MockPayment> GetPayments() => new()
        {
            new() { Id = 1, Code = "#P-01", Payer = "مجمع عيادات السلام الطبي", Type = "اشتراك عيادة", TypeClass = "badge-info", Amount = "5,000", Method = "Paymob - بطاقة", Status = "ناجح", StatusClass = "badge-success", Date = "2026-07-01" },
            new() { Id = 2, Code = "#P-02", Payer = "د. عمار السيد", Type = "عمولة طبيب", TypeClass = "badge-warning", Amount = "500", Method = "تحويل بنكي", Status = "معلق", StatusClass = "badge-warning", Date = "2026-07-02" },
            new() { Id = 3, Code = "#P-03", Payer = "محمد عمر (مريض)", Type = "موعد مريض", TypeClass = "badge-success", Amount = "200", Method = "Paymob - محفظة", Status = "ناجح", StatusClass = "badge-success", Date = "2026-07-03" },
            new() { Id = 4, Code = "#P-04", Payer = "مستشفى النور التخصصي", Type = "اشتراك عيادة", TypeClass = "badge-info", Amount = "12,000", Method = "تحويل بنكي", Status = "فاشل", StatusClass = "badge-danger", Date = "2026-06-28" },
            new() { Id = 5, Code = "#P-05", Payer = "عيادات التخصصات الدقيقة", Type = "خدمة إعلانية", TypeClass = "badge-danger", Amount = "3,500", Method = "Paymob - بطاقة", Status = "مسترد", StatusClass = "badge-info", Date = "2026-06-25" },
        };

        public static MockPaymentDetail GetPaymentDetail(int id) => id switch
        {
            2 => new MockPaymentDetail
            {
                Id = 2, Code = "#P-02", Payer = "د. عمار السيد", PayerType = "طبيب",
                PayerEmail = "ammar@clinic.com", PayerPhone = "01012345678",
                Type = "عمولة طبيب", TypeClass = "badge-warning", Amount = "500",
                Method = "تحويل بنكي", TransactionId = Guid.NewGuid().ToString("N").Substring(0, 12),
                RefNumber = "PM-2026-002", Status = "معلق", StatusClass = "badge-warning",
                Date = "2026-07-02", ItemName = "عمولة كشوفات شهر يونيو",
                Notes = "في انتظار تأكيد المعاملة من البنك.",
                Timeline = new()
                {
                    new() { Date = "2026-07-02 10:31", Text = "تم إرسال طلب التحويل البنكي", Marker = "warning" },
                    new() { Date = "2026-07-02 10:30", Text = "تم إنشاء المعاملة", Marker = "info" },
                }
            },
            3 => new MockPaymentDetail
            {
                Id = 3, Code = "#P-03", Payer = "محمد عمر (مريض)", PayerType = "مريض",
                PayerEmail = "mohamed@email.com", PayerPhone = "01111122222",
                Type = "موعد مريض", TypeClass = "badge-success", Amount = "200",
                Method = "Paymob - محفظة إلكترونية", TransactionId = Guid.NewGuid().ToString("N").Substring(0, 12),
                RefNumber = "PM-2026-003", Status = "ناجح", StatusClass = "badge-success",
                Date = "2026-07-03", ItemName = "كشف موعد",
                Notes = "لا توجد ملاحظات إضافية.",
                Timeline = new()
                {
                    new() { Date = "2026-07-03 10:32", Text = "تم تأكيد الدفع واستلام المبلغ", Marker = "success" },
                    new() { Date = "2026-07-03 10:31", Text = "تم تأكيد الدفع عبر Paymob", Marker = "success" },
                    new() { Date = "2026-07-03 10:30", Text = "تم إنشاء المعاملة", Marker = "info" },
                }
            },
            4 => new MockPaymentDetail
            {
                Id = 4, Code = "#P-04", Payer = "مستشفى النور التخصصي", PayerType = "عيادة",
                PayerEmail = "info@alnoor-hospital.com", PayerPhone = "01223456789",
                Type = "اشتراك عيادة", TypeClass = "badge-info", Amount = "12,000",
                Method = "تحويل بنكي", TransactionId = Guid.NewGuid().ToString("N").Substring(0, 12),
                RefNumber = "PM-2026-004", Status = "فاشل", StatusClass = "badge-danger",
                Date = "2026-06-28", ItemName = "اشتراك سنوي - 2026",
                Notes = "فشلت المعاملة بسبب خطأ في بيانات التحويل. يرجى مراجعة البيانات وإعادة المحاولة.",
                Timeline = new()
                {
                    new() { Date = "2026-06-28 11:00", Text = "فشلت المعاملة - بيانات غير صحيحة", Marker = "danger" },
                    new() { Date = "2026-06-28 10:55", Text = "تم إرسال طلب التحويل البنكي", Marker = "warning" },
                    new() { Date = "2026-06-28 10:30", Text = "تم إنشاء المعاملة", Marker = "info" },
                }
            },
            5 => new MockPaymentDetail
            {
                Id = 5, Code = "#P-05", Payer = "عيادات التخصصات الدقيقة", PayerType = "عيادة",
                PayerEmail = "info@specialized-clinic.com", PayerPhone = "01512345678",
                Type = "خدمة إعلانية", TypeClass = "badge-danger", Amount = "3,500",
                Method = "Paymob - بطاقة ائتمان", TransactionId = Guid.NewGuid().ToString("N").Substring(0, 12),
                RefNumber = "PM-2026-005", Status = "مسترد", StatusClass = "badge-info",
                Date = "2026-06-25", ItemName = "بانر إعلاني - شهرين",
                Notes = "تم استرداد المبلغ بناءً على طلب العيادة.",
                Timeline = new()
                {
                    new() { Date = "2026-06-27 14:00", Text = "تم استرداد المبلغ بناءً على طلب العيادة", Marker = "info" },
                    new() { Date = "2026-06-25 11:00", Text = "تم تأكيد الدفع عبر Paymob", Marker = "success" },
                    new() { Date = "2026-06-25 10:30", Text = "تم إنشاء المعاملة", Marker = "info" },
                }
            },
            _ => new MockPaymentDetail
            {
                Id = 1, Code = "#P-01", Payer = "مجمع عيادات السلام الطبي", PayerType = "عيادة",
                PayerEmail = "info@alsalam-clinic.com", PayerPhone = "01223456789",
                Type = "اشتراك عيادة", TypeClass = "badge-info", Amount = "5,000",
                Method = "Paymob - بطاقة ائتمان", TransactionId = Guid.NewGuid().ToString("N").Substring(0, 12),
                RefNumber = "PM-2026-001", Status = "ناجح", StatusClass = "badge-success",
                Date = "2026-07-01", ItemName = "اشتراك شهري - يوليو 2026",
                Notes = "لا توجد ملاحظات إضافية.",
                Timeline = new()
                {
                    new() { Date = "2026-07-01 10:32", Text = "تم تأكيد الدفع واستلام المبلغ", Marker = "success" },
                    new() { Date = "2026-07-01 10:31", Text = "تم تأكيد الدفع عبر Paymob", Marker = "success" },
                    new() { Date = "2026-07-01 10:30", Text = "تم إنشاء المعاملة", Marker = "info" },
                }
            },
        };

        // ========== Users ==========
        public static List<MockUser> GetUsers() => new()
        {
            new() { Id = 1, Name = "محمد عمر", Email = "mohamed@email.com", Phone = "+966 50 111 2222", Initials = "مع", RegistrationDate = "15 يناير 2025", Status = "نشط", StatusClass = "badge-success", Role = UserRole.Patient, TotalVisits = 24, AvgRating = 4.5, TotalSpent = "12,450" },
            new() { Id = 2, Name = "سارة أحمد", Email = "sara@email.com", Phone = "+966 55 333 4444", Initials = "سأ", RegistrationDate = "3 مارس 2025", Status = "نشط", StatusClass = "badge-success", Role = UserRole.Patient, TotalVisits = 15, AvgRating = 4.8, TotalSpent = "8,200" },
            new() { Id = 3, Name = "خالد الزهراني", Email = "khalid@email.com", Phone = "+966 50 555 6666", Initials = "خز", RegistrationDate = "20 يونيو 2025", Status = "نشط", StatusClass = "badge-success", Role = UserRole.Patient, TotalVisits = 8, AvgRating = 3.9, TotalSpent = "3,600" },
            new() { Id = 4, Name = "فاطمة الناصر", Email = "fatima@email.com", Phone = "+966 55 777 8888", Initials = "فن", RegistrationDate = "1 أبريل 2026", Status = "نشط", StatusClass = "badge-success", Role = UserRole.Patient, TotalVisits = 3, AvgRating = 5.0, TotalSpent = "600" },
            new() { Id = 5, Name = "عبد الله السعيد", Email = "abdullah@email.com", Phone = "+966 50 999 0000", Initials = "عس", RegistrationDate = "10 فبراير 2025", Status = "غير نشط", StatusClass = "badge-warning", Role = UserRole.Patient, TotalVisits = 2, AvgRating = 3.0, TotalSpent = "400" },
            new() { Id = 6, Name = "أحمد المدير", Email = "ahmed@clinic1.com", Phone = "+966 55 111 2222", Initials = "أم", RegistrationDate = "1 يناير 2025", Status = "نشط", StatusClass = "badge-success", Role = UserRole.ClinicOwner, TotalVisits = 0, AvgRating = 0, TotalSpent = "0" },
            new() { Id = 7, Name = "هشام فؤاد", Email = "hisham@clinic2.com", Phone = "+966 55 333 4444", Initials = "هف", RegistrationDate = "15 فبراير 2025", Status = "نشط", StatusClass = "badge-success", Role = UserRole.ClinicOwner, TotalVisits = 0, AvgRating = 0, TotalSpent = "0" },
            new() { Id = 8, Name = "خالد الزهراني", Email = "khalid@clinic3.com", Phone = "+966 55 555 6666", Initials = "خز", RegistrationDate = "1 مارس 2025", Status = "نشط", StatusClass = "badge-success", Role = UserRole.ClinicOwner, TotalVisits = 0, AvgRating = 0, TotalSpent = "0" },
            new() { Id = 9, Name = "عمار السيد", Email = "ammar@clinic4.com", Phone = "+966 55 777 8888", Initials = "عس", RegistrationDate = "20 مارس 2025", Status = "نشط", StatusClass = "badge-success", Role = UserRole.ClinicOwner, TotalVisits = 0, AvgRating = 0, TotalSpent = "0" },
            new() { Id = 10, Name = "منى سعيد", Email = "mona@clinic5.com", Phone = "+966 55 999 0000", Initials = "مس", RegistrationDate = "1 أبريل 2025", Status = "غير نشط", StatusClass = "badge-warning", Role = UserRole.ClinicOwner, TotalVisits = 0, AvgRating = 0, TotalSpent = "0" },
        };

        public static MockUserOverview GetUserOverview(int id) => id switch
        {
            2 => new MockUserOverview
            {
                Id = 2, Name = "سارة أحمد", Initials = "سأ", Email = "sara@email.com", Phone = "+966 55 333 4444",
                RegistrationDate = "3 مارس 2025", Status = "نشط", StatusClass = "badge-success",
                TotalVisits = 15, AvgRating = 4.8, TotalSpent = "8,200",
                Activity = new()
                {
                    new() { Date = "2026-07-02", Text = "قامت بتقييم زيارة عيادة القلب", Icon = "star" },
                    new() { Date = "2026-06-28", Text = "قامت بحجز موعد في عيادة الجلدية", Icon = "calendar" },
                    new() { Date = "2026-06-20", Text = "قامت بالتعليق على منشور العيادة", Icon = "comment" },
                    new() { Date = "2026-06-15", Text = "قامت بتحديث الملف الشخصي", Icon = "edit" },
                    new() { Date = "2026-06-10", Text = "تم إلغاء موعد - عيادة العظام", Icon = "cancel" },
                }
            },
            3 => new MockUserOverview
            {
                Id = 3, Name = "خالد الزهراني", Initials = "خز", Email = "khalid@email.com", Phone = "+966 50 555 6666",
                RegistrationDate = "20 يونيو 2025", Status = "نشط", StatusClass = "badge-success",
                TotalVisits = 8, AvgRating = 3.9, TotalSpent = "3,600",
                Activity = new()
                {
                    new() { Date = "2026-07-01", Text = "قام بحجز موعد في عيادة العظام", Icon = "calendar" },
                    new() { Date = "2026-06-25", Text = "قام بتقييم زيارة عيادة الأعصاب", Icon = "star" },
                    new() { Date = "2026-06-10", Text = "قام بتحديث الملف الشخصي", Icon = "edit" },
                }
            },
            4 => new MockUserOverview
            {
                Id = 4, Name = "فاطمة الناصر", Initials = "فن", Email = "fatima@email.com", Phone = "+966 55 777 8888",
                RegistrationDate = "1 أبريل 2026", Status = "نشط", StatusClass = "badge-success",
                TotalVisits = 3, AvgRating = 5.0, TotalSpent = "600",
                Activity = new()
                {
                    new() { Date = "2026-06-15", Text = "قامت بتقييم زيارة عيادة الأطفال", Icon = "star" },
                    new() { Date = "2026-06-01", Text = "قامت بحجز أول موعد", Icon = "calendar" },
                }
            },
            5 => new MockUserOverview
            {
                Id = 5, Name = "عبد الله السعيد", Initials = "عس", Email = "abdullah@email.com", Phone = "+966 50 999 0000",
                RegistrationDate = "10 فبراير 2025", Status = "غير نشط", StatusClass = "badge-warning",
                TotalVisits = 2, AvgRating = 3.0, TotalSpent = "400",
                Activity = new()
                {
                    new() { Date = "2026-03-01", Text = "آخر زيارة - عيادة القلب", Icon = "calendar" },
                    new() { Date = "2025-12-15", Text = "قام بتقييم زيارة عيادة القلب", Icon = "star" },
                }
            },
            _ => new MockUserOverview
            {
                Id = 1, Name = "محمد عمر", Initials = "مع", Email = "mohamed@email.com", Phone = "+966 50 111 2222",
                RegistrationDate = "15 يناير 2025", Status = "نشط", StatusClass = "badge-success",
                TotalVisits = 24, AvgRating = 4.5, TotalSpent = "12,450",
                Activity = new()
                {
                    new() { Date = "2026-07-03", Text = "قام بتقييم زيارة عيادة القلب", Icon = "star" },
                    new() { Date = "2026-07-01", Text = "قام بحجز موعد في عيادة العظام", Icon = "calendar" },
                    new() { Date = "2026-06-28", Text = "قام بالتعليق على منشور العيادة", Icon = "comment" },
                    new() { Date = "2026-06-20", Text = "قام بإلغاء موعد - عيادة الجلدية", Icon = "cancel" },
                    new() { Date = "2026-06-15", Text = "قام بتحديث الملف الشخصي", Icon = "edit" },
                    new() { Date = "2026-06-10", Text = "تم تأكيد موعد في عيادة الأعصاب", Icon = "calendar" },
                    new() { Date = "2026-06-05", Text = "قام بدفع فاتورة الاشتراك", Icon = "payment" },
                }
            },
        };

        public static List<MockUserVisit> GetUserVisits(int id) => id switch
        {
            2 => new()
            {
                new() { Id = 1, Clinic = "عيادة القلب", Doctor = "د. سارة أحمد", Date = "2026-07-02", Diagnosis = "فحص دوري للقلب", Notes = "النتائج مطمئنة", DoctorBehavior = 5, ReceptionBehavior = 5, ClinicCleanliness = 5, Comment = "تعامل رائع من الطبيب والاستقبال" },
                new() { Id = 2, Clinic = "عيادة الجلدية", Doctor = "د. عمار السيد", Date = "2026-06-20", Diagnosis = "حساسية جلدية", Notes = "تم وصف كريم موضعي", DoctorBehavior = 5, ReceptionBehavior = 4, ClinicCleanliness = 5 },
                new() { Id = 3, Clinic = "عيادة العيون", Doctor = "د. علي الناصر", Date = "2026-05-10", Diagnosis = "فحص نظر", Notes = "نظر سليم", DoctorBehavior = 4, ReceptionBehavior = 4, ClinicCleanliness = 5 },
                new() { Id = 4, Clinic = "عيادة الأعصاب", Doctor = "د. عبد الله ناصر", Date = "2026-04-15", Diagnosis = "صداع نصفي", Notes = "تحسن بعد العلاج", DoctorBehavior = 5, ReceptionBehavior = 5, ClinicCleanliness = 5, Comment = "أطباء ممتازين" },
                new() { Id = 5, Clinic = "عيادة العظام", Doctor = "د. خالد الزهراني", Date = "2026-03-05", Diagnosis = "آلام الركبة", Notes = "", DoctorBehavior = 5, ReceptionBehavior = 5, ClinicCleanliness = 4 },
            },
            3 => new()
            {
                new() { Id = 1, Clinic = "عيادة العظام", Doctor = "د. خالد الزهراني", Date = "2026-07-01", Diagnosis = "متابعة كسر", Notes = "التئام جيد", DoctorBehavior = 4, ReceptionBehavior = 3, ClinicCleanliness = 4, Comment = "الاستقبال كان بطيئًا" },
                new() { Id = 2, Clinic = "عيادة الأعصاب", Doctor = "د. عبد الله ناصر", Date = "2026-06-25", Diagnosis = "تنميل الأطراف", Notes = "طلب فحوصات إضافية", DoctorBehavior = 3, ReceptionBehavior = 3, ClinicCleanliness = 4 },
                new() { Id = 3, Clinic = "عيادة القلب", Doctor = "د. سارة أحمد", Date = "2026-05-20", Diagnosis = "فحص ضغط الدم", Notes = "الضغط مرتفع قليلاً", DoctorBehavior = 4, ReceptionBehavior = 4, ClinicCleanliness = 4 },
            },
            4 => new()
            {
                new() { Id = 1, Clinic = "عيادة الأطفال", Doctor = "د. أحمد علي", Date = "2026-06-15", Diagnosis = "متابعة نمو", Notes = "الطفل بصحة جيدة", DoctorBehavior = 5, ReceptionBehavior = 5, ClinicCleanliness = 5, Comment = "ممتاز جدًا" },
                new() { Id = 2, Clinic = "عيادة القلب", Doctor = "د. سارة أحمد", Date = "2026-06-01", Diagnosis = "فحص أولي", Notes = "", DoctorBehavior = 5, ReceptionBehavior = 5, ClinicCleanliness = 5 },
                new() { Id = 3, Clinic = "عيادة الجلدية", Doctor = "د. عمار السيد", Date = "2026-04-20", Diagnosis = "طفح جلدي", Notes = "تم وصف علاج", DoctorBehavior = 5, ReceptionBehavior = 5, ClinicCleanliness = 5 },
            },
            5 => new()
            {
                new() { Id = 1, Clinic = "عيادة القلب", Doctor = "د. سارة أحمد", Date = "2026-03-01", Diagnosis = "فحص قلب", Notes = "", DoctorBehavior = 3, ReceptionBehavior = 3, ClinicCleanliness = 3 },
                new() { Id = 2, Clinic = "عيادة العظام", Doctor = "د. خالد الزهراني", Date = "2025-10-10", Diagnosis = "آلام الظهر", Notes = "تم تحويله للعلاج الطبيعي", DoctorBehavior = 3, ReceptionBehavior = 3, ClinicCleanliness = 3 },
            },
            _ => new()
            {
                new() { Id = 1, Clinic = "عيادة القلب", Doctor = "د. سارة أحمد", Date = "2026-07-03", Diagnosis = "فحص دوري للقلب", Notes = "نتائج سليمة", DoctorBehavior = 5, ReceptionBehavior = 5, ClinicCleanliness = 5, Comment = "تجربة ممتازة" },
                new() { Id = 2, Clinic = "عيادة العظام", Doctor = "د. خالد الزهراني", Date = "2026-06-28", Diagnosis = "آلام الرقبة", Notes = "تم وصف مسكنات", DoctorBehavior = 4, ReceptionBehavior = 4, ClinicCleanliness = 4 },
                new() { Id = 3, Clinic = "عيادة الجلدية", Doctor = "د. عمار السيد", Date = "2026-06-15", Diagnosis = "فحص شامة", Notes = "حميدة", DoctorBehavior = 5, ReceptionBehavior = 5, ClinicCleanliness = 5 },
                new() { Id = 4, Clinic = "عيادة الأعصاب", Doctor = "د. عبد الله ناصر", Date = "2026-05-20", Diagnosis = "صداع", Notes = "إجهاد", DoctorBehavior = 4, ReceptionBehavior = 4, ClinicCleanliness = 4 },
                new() { Id = 5, Clinic = "عيادة العيون", Doctor = "د. علي الناصر", Date = "2026-04-10", Diagnosis = "التهاب العين", Notes = "قطرات مضاد حيوي", DoctorBehavior = 5, ReceptionBehavior = 5, ClinicCleanliness = 4 },
                new() { Id = 6, Clinic = "عيادة القلب", Doctor = "د. سارة أحمد", Date = "2026-03-15", Diagnosis = "متابعة ضغط الدم", Notes = "", DoctorBehavior = 4, ReceptionBehavior = 4, ClinicCleanliness = 4 },
            },
        };

        public static List<MockTableRow> GetUserRequests(int id)
        {
            var baseRequests = new List<MockTableRow>
            {
                new() { Cells = new() { { "type", "حجز موعد" }, { "status", "مؤكد" }, { "statusClass", "badge-success" }, { "date", "2026-07-01" }, { "notes", "عيادة العظام - د. خالد الزهراني" } } },
                new() { Cells = new() { { "type", "إلغاء موعد" }, { "status", "مكتمل" }, { "statusClass", "badge-info" }, { "date", "2026-06-20" }, { "notes", "عيادة الجلدية - تعارض مواعيد" } } },
                new() { Cells = new() { { "type", "إعادة جدولة" }, { "status", "مؤكد" }, { "statusClass", "badge-success" }, { "date", "2026-06-10" }, { "notes", "تم تغيير الموعد من 15/6 إلى 20/6" } } },
                new() { Cells = new() { { "type", "طلب تقرير طبي" }, { "status", "قيد المعالجة" }, { "statusClass", "badge-warning" }, { "date", "2026-06-05" }, { "notes", "تقرير فحص القلب" } } },
                new() { Cells = new() { { "type", "شكوى" }, { "status", "تم الحل" }, { "statusClass", "badge-success" }, { "date", "2026-05-15" }, { "notes", "تأخر الموعد" } } },
            };
            return baseRequests;
        }

        public static List<MockPayment> GetUserPayments(int userId) => new()
        {
            new() { Id = 100, Code = "#P-010", Payer = "محمد عمر", Type = "موعد مريض", TypeClass = "badge-success", Amount = "200", Method = "Paymob - محفظة", Status = "ناجح", StatusClass = "badge-success", Date = "2026-07-03" },
            new() { Id = 101, Code = "#P-009", Payer = "محمد عمر", Type = "موعد مريض", TypeClass = "badge-success", Amount = "200", Method = "Paymob - بطاقة", Status = "ناجح", StatusClass = "badge-success", Date = "2026-06-28" },
            new() { Id = 102, Code = "#P-008", Payer = "محمد عمر", Type = "موعد مريض", TypeClass = "badge-success", Amount = "200", Method = "نقدي", Status = "ناجح", StatusClass = "badge-success", Date = "2026-06-15" },
            new() { Id = 103, Code = "#P-007", Payer = "محمد عمر", Type = "خدمة إعلانية", TypeClass = "badge-danger", Amount = "500", Method = "Paymob - بطاقة", Status = "مسترد", StatusClass = "badge-info", Date = "2026-06-10" },
            new() { Id = 104, Code = "#P-006", Payer = "محمد عمر", Type = "موعد مريض", TypeClass = "badge-success", Amount = "200", Method = "تحويل بنكي", Status = "معلق", StatusClass = "badge-warning", Date = "2026-06-05" },
        };

        // ========== Clinics ==========
        public static List<MockClinic> GetClinics() => new()
        {
            new() { Id = 1, Name = "مركز القلب التخصصي", Specialty = "القلب والأوعية الدموية", Specializations = new() { "القلب والأوعية الدموية", "جراحة القلب", "قسطرة القلب" }, Description = "مركز متخصص في تشخيص وعلاج أمراض القلب والشرايين، يضم أحدث الأجهزة الطبية وفريقاً من أطباء القلب المتميزين.", ResponsibleDoctor = "د. سارة أحمد", ManagerName = "أ. محمد عبد الرحمن", Location = "الطابق الثاني - غرفة 201", Latitude = 31.0435, Longitude = 31.3752, OwnerUserId = 6, Phone = "+966 11 234 5678", ImageUrl = "", AvgRating = 4.5, RatingsCount = 28, IsActive = true },
            new() { Id = 2, Name = "مركز الأعصاب والعمود الفقري", Specialty = "الأمراض العصبية", Specializations = new() { "الأمراض العصبية", "جراحة المخ والأعصاب", "العلاج الطبيعي" }, Description = "مركز رائد في تشخيص وعلاج اضطرابات الجهاز العصبي وجراحات العمود الفقري المتقدمة.", ResponsibleDoctor = "د. عبد الله ناصر", ManagerName = "د. هشام فؤاد", Location = "الطابق الثالث - غرفة 305", Latitude = 31.0382, Longitude = 31.3801, OwnerUserId = 7, Phone = "+966 11 345 6789", ImageUrl = "", AvgRating = 4.2, RatingsCount = 19, IsActive = true },
            new() { Id = 3, Name = "عيادة العظام والعلاج الطبيعي", Specialty = "جراحة العظام", Specializations = new() { "جراحة العظام", "العلاج الطبيعي", "طب الرياضة" }, Description = "عيادة متكاملة لجراحة العظام والمفاصل وإصابات الرياضة مع أحدث تقنيات العلاج الطبيعي.", ResponsibleDoctor = "د. خالد الزهراني", ManagerName = "د. خالد الزهراني", Location = "الطابق الأول - غرفة 104", Latitude = 31.0420, Longitude = 31.3820, OwnerUserId = 8, Phone = "+966 11 456 7890", ImageUrl = "", AvgRating = 4.8, RatingsCount = 42, IsActive = true },
            new() { Id = 4, Name = "عيادة الجلدية والتجميل", Specialty = "الأمراض الجلدية", Specializations = new() { "الأمراض الجلدية", "جراحة التجميل", "الليزر والعناية بالبشرة" }, Description = "عيادة متخصصة في الأمراض الجلدية وعلاجات التجميل بالليزر مع أحدث التقنيات العالمية.", ResponsibleDoctor = "د. عمار السيد", ManagerName = "د. عمار السيد", Location = "الطابق الثاني - غرفة 210", Latitude = 31.0395, Longitude = 31.3760, OwnerUserId = 9, Phone = "+966 11 567 8901", ImageUrl = "", AvgRating = 4.6, RatingsCount = 35, IsActive = true },
            new() { Id = 5, Name = "عيادة الأطفال", Specialty = "طب الأطفال", Specializations = new() { "طب الأطفال", "حديثي الولادة" }, Description = "عيادة متخصصة في رعاية الأطفال من حديثي الولادة حتى سن المراهقة مع متابعة دقيقة للنمو والتطور.", ResponsibleDoctor = "—", ManagerName = "—", Location = "الطابق الرابع - غرفة 402", Latitude = 31.0418, Longitude = 31.3798, OwnerUserId = 10, Phone = "+966 11 678 9012", ImageUrl = "", AvgRating = 3.8, RatingsCount = 7, IsActive = false },
        };

        public static MockClinic? GetClinicById(int id) => GetClinics().FirstOrDefault(c => c.Id == id);

        public static List<MockDoctorClinicConfig> GetClinicDoctors(int clinicId)
        {
            var allDoctors = new Dictionary<int, List<MockDoctorClinicConfig>>
            {
                [1] = new()
                {
                    new()
                    {
                        DoctorId = 2, DoctorName = "د. سارة أحمد", Specialty = "أمراض القلب", Degree = "أخصائي", Photo = "", IsActive = true, IsPrimary = true,
                        WorkingDays = new()
                        {
                            new() { Day = "Sunday", DayAr = "الأحد", StartTime = "09:00", EndTime = "15:00", IsAvailable = true },
                            new() { Day = "Monday", DayAr = "الإثنين", StartTime = "09:00", EndTime = "17:00", IsAvailable = true },
                            new() { Day = "Tuesday", DayAr = "الثلاثاء", StartTime = "09:00", EndTime = "17:00", IsAvailable = true },
                            new() { Day = "Wednesday", DayAr = "الأربعاء", StartTime = "09:00", EndTime = "15:00", IsAvailable = true },
                            new() { Day = "Thursday", DayAr = "الخميس", StartTime = "09:00", EndTime = "13:00", IsAvailable = true },
                            new() { Day = "Friday", DayAr = "الجمعة", IsAvailable = false },
                            new() { Day = "Saturday", DayAr = "السبت", IsAvailable = false },
                        },
                        ExaminationFee = 300, FollowUpFee = 150, CommissionPercent = 25, CommissionFixed = 0,
                        RoomAssignment = "غرفة 201", ContractType = "FullTime", ContractStart = "2025-01-01", SessionLimitPerDay = 20
                    },
                    new()
                    {
                        DoctorId = 7, DoctorName = "د. محمود حسن", Specialty = "جراحة القلب", Degree = "استشاري", Photo = "", IsActive = true, IsPrimary = false,
                        WorkingDays = new()
                        {
                            new() { Day = "Sunday", DayAr = "الأحد", StartTime = "10:00", EndTime = "14:00", IsAvailable = true },
                            new() { Day = "Monday", DayAr = "الإثنين", StartTime = "10:00", EndTime = "14:00", IsAvailable = true },
                            new() { Day = "Tuesday", DayAr = "الثلاثاء", IsAvailable = false },
                            new() { Day = "Wednesday", DayAr = "الأربعاء", StartTime = "10:00", EndTime = "14:00", IsAvailable = true },
                            new() { Day = "Thursday", DayAr = "الخميس", IsAvailable = false },
                            new() { Day = "Friday", DayAr = "الجمعة", IsAvailable = false },
                            new() { Day = "Saturday", DayAr = "السبت", IsAvailable = false },
                        },
                        ExaminationFee = 500, FollowUpFee = 250, CommissionPercent = 30, CommissionFixed = 0,
                        RoomAssignment = "غرفة 203", ContractType = "Visits", ContractStart = "2026-03-01", SessionLimitPerDay = 10
                    },
                },
                [2] = new()
                {
                    new()
                    {
                        DoctorId = 4, DoctorName = "د. عبد الله ناصر", Specialty = "الأمراض العصبية", Degree = "أخصائي", Photo = "", IsActive = false, IsPrimary = true,
                        WorkingDays = new()
                        {
                            new() { Day = "Sunday", DayAr = "الأحد", IsAvailable = false },
                            new() { Day = "Monday", DayAr = "الإثنين", StartTime = "09:00", EndTime = "17:00", IsAvailable = true },
                            new() { Day = "Tuesday", DayAr = "الثلاثاء", StartTime = "09:00", EndTime = "17:00", IsAvailable = true },
                            new() { Day = "Wednesday", DayAr = "الأربعاء", StartTime = "09:00", EndTime = "15:00", IsAvailable = true },
                            new() { Day = "Thursday", DayAr = "الخميس", StartTime = "09:00", EndTime = "15:00", IsAvailable = true },
                            new() { Day = "Friday", DayAr = "الجمعة", IsAvailable = false },
                            new() { Day = "Saturday", DayAr = "السبت", IsAvailable = false },
                        },
                        ExaminationFee = 250, FollowUpFee = 120, CommissionPercent = 20, CommissionFixed = 0,
                        RoomAssignment = "غرفة 305", ContractType = "PartTime", ContractStart = "2025-06-01", SessionLimitPerDay = 15
                    },
                },
                [3] = new()
                {
                    new()
                    {
                        DoctorId = 3, DoctorName = "د. خالد الزهراني", Specialty = "جراحة العظام", Degree = "استشاري", Photo = "", IsActive = true, IsPrimary = true,
                        WorkingDays = new()
                        {
                            new() { Day = "Sunday", DayAr = "الأحد", StartTime = "10:00", EndTime = "18:00", IsAvailable = true },
                            new() { Day = "Monday", DayAr = "الإثنين", StartTime = "10:00", EndTime = "18:00", IsAvailable = true },
                            new() { Day = "Tuesday", DayAr = "الثلاثاء", StartTime = "10:00", EndTime = "18:00", IsAvailable = true },
                            new() { Day = "Wednesday", DayAr = "الأربعاء", StartTime = "10:00", EndTime = "18:00", IsAvailable = true },
                            new() { Day = "Thursday", DayAr = "الخميس", StartTime = "10:00", EndTime = "14:00", IsAvailable = true },
                            new() { Day = "Friday", DayAr = "الجمعة", IsAvailable = false },
                            new() { Day = "Saturday", DayAr = "السبت", IsAvailable = false },
                        },
                        ExaminationFee = 400, FollowUpFee = 200, CommissionPercent = 35, CommissionFixed = 0,
                        RoomAssignment = "غرفة 104", ContractType = "FullTime", ContractStart = "2024-01-01", SessionLimitPerDay = 25
                    },
                    new()
                    {
                        DoctorId = 8, DoctorName = "د. نورة السعيد", Specialty = "العلاج الطبيعي", Degree = "أخصائي", Photo = "", IsActive = true, IsPrimary = false,
                        WorkingDays = new()
                        {
                            new() { Day = "Sunday", DayAr = "الأحد", StartTime = "09:00", EndTime = "15:00", IsAvailable = true },
                            new() { Day = "Monday", DayAr = "الإثنين", IsAvailable = false },
                            new() { Day = "Tuesday", DayAr = "الثلاثاء", StartTime = "09:00", EndTime = "15:00", IsAvailable = true },
                            new() { Day = "Wednesday", DayAr = "الأربعاء", IsAvailable = false },
                            new() { Day = "Thursday", DayAr = "الخميس", StartTime = "09:00", EndTime = "15:00", IsAvailable = true },
                            new() { Day = "Friday", DayAr = "الجمعة", IsAvailable = false },
                            new() { Day = "Saturday", DayAr = "السبت", IsAvailable = false },
                        },
                        ExaminationFee = 150, FollowUpFee = 75, CommissionPercent = 40, CommissionFixed = 0,
                        RoomAssignment = "غرفة 106 - العلاج الطبيعي", ContractType = "RevenueShare", ContractStart = "2026-02-01", SessionLimitPerDay = 12
                    },
                },
                [4] = new()
                {
                    new()
                    {
                        DoctorId = 1, DoctorName = "د. عمار السيد", Specialty = "الأمراض الجلدية", Degree = "استشاري", Photo = "", IsActive = true, IsPrimary = true,
                        WorkingDays = new()
                        {
                            new() { Day = "Sunday", DayAr = "الأحد", StartTime = "10:00", EndTime = "17:00", IsAvailable = true },
                            new() { Day = "Monday", DayAr = "الإثنين", StartTime = "10:00", EndTime = "17:00", IsAvailable = true },
                            new() { Day = "Tuesday", DayAr = "الثلاثاء", StartTime = "10:00", EndTime = "17:00", IsAvailable = true },
                            new() { Day = "Wednesday", DayAr = "الأربعاء", StartTime = "10:00", EndTime = "14:00", IsAvailable = true },
                            new() { Day = "Thursday", DayAr = "الخميس", StartTime = "10:00", EndTime = "17:00", IsAvailable = true },
                            new() { Day = "Friday", DayAr = "الجمعة", IsAvailable = false },
                            new() { Day = "Saturday", DayAr = "السبت", IsAvailable = false },
                        },
                        ExaminationFee = 350, FollowUpFee = 175, CommissionPercent = 0, CommissionFixed = 0,
                        RoomAssignment = "غرفة 210", ContractType = "FullTime", ContractStart = "2024-06-01", SessionLimitPerDay = 20
                    },
                },
                [5] = new(),
            };
            return allDoctors.TryGetValue(clinicId, out var doctors) ? doctors : new();
        }

        public static List<MockPatientRating> GetClinicRatings(int clinicId)
        {
            var allRatings = new Dictionary<int, List<MockPatientRating>>
            {
                [1] = new()
                {
                    new() { Id = 1, ClinicId = 1, DoctorName = "د. سارة أحمد", PatientName = "أحمد عبد الله", PatientInitial = "أ", Rating = 5, Comment = "دكتورة ممتازة جداً، شرح وافي للحالة وعلاج فعال. أنصح بزيارتها.", Date = "2026-07-03" },
                    new() { Id = 2, ClinicId = 1, DoctorName = "د. سارة أحمد", PatientName = "فاطمة الزهراء", PatientInitial = "ف", Rating = 4, Comment = "عيادة مرتبة ونظيفة، المواعيد دقيقة. تجربة جيدة.", Date = "2026-07-01" },
                    new() { Id = 3, ClinicId = 1, DoctorName = "د. محمود حسن", PatientName = "سامي خالد", PatientInitial = "س", Rating = 5, Comment = "دكتور محمود استشاري رائع، العملية كانت ناجحة ولله الحمد.", Date = "2026-06-28" },
                    new() { Id = 4, ClinicId = 1, DoctorName = "د. سارة أحمد", PatientName = "نورة علي", PatientInitial = "ن", Rating = 4, Comment = "التجربة جيدة، لكن الانتظار كان طويلاً بعض الشيء.", Date = "2026-06-25" },
                },
                [2] = new()
                {
                    new() { Id = 5, ClinicId = 2, DoctorName = "د. عبد الله ناصر", PatientName = "محمد عمر", PatientInitial = "م", Rating = 4, Comment = "دكتور ممتاز ومتابعة جيدة. شكراً على الاهتمام.", Date = "2026-07-02" },
                    new() { Id = 6, ClinicId = 2, DoctorName = "د. عبد الله ناصر", PatientName = "سارة أحمد", PatientInitial = "س", Rating = 3, Comment = "المواعيد تحتاج تحسين، لكن التشخيص كان دقيقاً.", Date = "2026-06-30" },
                },
                [3] = new()
                {
                    new() { Id = 7, ClinicId = 3, DoctorName = "د. خالد الزهراني", PatientName = "عمر حسن", PatientInitial = "ع", Rating = 5, Comment = "أفضل دكتور عظام زرته. خبرة كبيرة وأسلوب رائع.", Date = "2026-07-04" },
                    new() { Id = 8, ClinicId = 3, DoctorName = "د. خالد الزهراني", PatientName = "ليلى محمود", PatientInitial = "ل", Rating = 5, Comment = "الحمد لله تعافيت تماماً بعد العلاج. دكتور خالد متمكن جداً.", Date = "2026-07-02" },
                    new() { Id = 9, ClinicId = 3, DoctorName = "د. نورة السعيد", PatientName = "هدى ناصر", PatientInitial = "ه", Rating = 4, Comment = "جلسات العلاج الطبيعي مفيدة جداً. دكتورة نورة محترفة.", Date = "2026-06-29" },
                    new() { Id = 10, ClinicId = 3, DoctorName = "د. خالد الزهراني", PatientName = "أحمد رضا", PatientInitial = "أ", Rating = 5, Comment = "والله ما تقصرون، يديكم العافية على المجهود الرائع.", Date = "2026-06-27" },
                },
                [4] = new()
                {
                    new() { Id = 11, ClinicId = 4, DoctorName = "د. عمار السيد", PatientName = "نورة عبد الله", PatientInitial = "ن", Rating = 5, Comment = "دكتور عمار مبدع في عمله. نتائج الليزر رائعة جداً.", Date = "2026-07-01" },
                    new() { Id = 12, ClinicId = 4, DoctorName = "د. عمار السيد", PatientName = "منى سعيد", PatientInitial = "م", Rating = 4, Comment = "عيادة نظيفة وفريق عمل محترف. سعيدة بالنتيجة.", Date = "2026-06-26" },
                },
                [5] = new()
                {
                    new() { Id = 13, ClinicId = 5, DoctorName = "—", PatientName = "خالد إبراهيم", PatientInitial = "خ", Rating = 3, Comment = "العيادة مغلقة معظم الأوقات، لكن الخدمة جيدة عند الزيارة.", Date = "2026-05-15" },
                },
            };
            return allRatings.TryGetValue(clinicId, out var ratings) ? ratings : new();
        }

        public static double GetClinicAvgRating(int clinicId)
        {
            var ratings = GetClinicRatings(clinicId);
            return ratings.Any() ? Math.Round(ratings.Average(r => r.Rating), 1) : 0;
        }

        public static decimal GetClinicTotalFees(int clinicId)
        {
            var doctors = GetClinicDoctors(clinicId);
            return doctors.Sum(d => d.ExaminationFee + d.FollowUpFee);
        }

        // ========== Doctors ==========
        public static List<MockDoctor> GetDoctors() => new()
        {
            new() { Id = 1, Name = "د. عمار السيد", SyndicateId = "123456", TaxRegistry = "789-123-456", Degree = "استشاري", Specialty = "الأمراض الجلدية", DoctorType = DoctorEmploymentType.OwnClinic, ClinicId = 4, WorkplaceName = "عيادة الجلدية", Phone = "+966 55 123 4567", Email = "ammar@clinic.com", Photo = "", Documents = new() { "syndicate_card_1.png", "cert_1.png" }, IsActive = true },
            new() { Id = 2, Name = "د. سارة أحمد", SyndicateId = "234567", TaxRegistry = "789-234-567", Degree = "أخصائي", Specialty = "أمراض القلب", DoctorType = DoctorEmploymentType.InCenter, ClinicId = 1, WorkplaceName = "عيادة القلب", Phone = "+966 55 234 5678", Email = "sara@clinic.com", Photo = "", Documents = new() { "syndicate_card_2.png", "cert_2.png" }, IsActive = true },
            new() { Id = 3, Name = "د. خالد الزهراني", SyndicateId = "345678", TaxRegistry = "789-345-678", Degree = "استشاري", Specialty = "جراحة العظام", DoctorType = DoctorEmploymentType.OwnClinic, ClinicId = 3, WorkplaceName = "عيادة العظام", Phone = "+966 55 345 6789", Email = "khalid@clinic.com", Photo = "", Documents = new() { "syndicate_card_3.png" }, IsActive = true },
            new() { Id = 4, Name = "د. عبد الله ناصر", SyndicateId = "456789", TaxRegistry = "789-456-789", Degree = "أخصائي", Specialty = "الأمراض العصبية", DoctorType = DoctorEmploymentType.InCenter, ClinicId = 2, WorkplaceName = "عيادة الأعصاب", Phone = "+966 55 456 7890", Email = "abdullah@clinic.com", Photo = "", Documents = new(), IsActive = false },
            new() { Id = 5, Name = "د. أحمد علي", SyndicateId = "567890", TaxRegistry = "789-567-890", Degree = "طبيب", Specialty = "طب الأطفال", DoctorType = DoctorEmploymentType.Freelance, Phone = "+966 55 567 8901", Email = "ahmed@clinic.com", Photo = "", Documents = new() { "syndicate_card_5.png" }, IsActive = true },
            new() { Id = 6, Name = "د. ليلى محمود", SyndicateId = "678901", TaxRegistry = "789-678-901", Degree = "أخصائي", Specialty = "النساء والولادة", DoctorType = DoctorEmploymentType.Freelance, Phone = "+966 55 678 9012", Email = "layla@clinic.com", Photo = "", Documents = new() { "syndicate_card_6.png", "cert_6.png" }, IsActive = false },
        };

        public static MockDoctor? GetDoctorById(int id) => GetDoctors().FirstOrDefault(d => d.Id == id);

        public static List<MockClinicStaff> GetClinicStaff(int clinicId) => clinicId switch
        {
            1 => new() { new() { Id = 1, Name = "محمد عمر", Role = ClinicStaffRole.Reception, Phone = "+966 50 111 2222", IsActive = true }, new() { Id = 2, Name = "نورة حسن", Role = ClinicStaffRole.Nurse, Phone = "+966 50 333 4444", IsActive = true } },
            2 => new() { new() { Id = 3, Name = "علياء سعيد", Role = ClinicStaffRole.Reception, Phone = "+966 50 555 6666", IsActive = true }, new() { Id = 4, Name = "سامي خالد", Role = ClinicStaffRole.Cleaner, Phone = "+966 50 777 8888", IsActive = false } },
            3 => new() { new() { Id = 5, Name = "فاطمة أحمد", Role = ClinicStaffRole.Reception, Phone = "+966 50 999 0000", IsActive = true }, new() { Id = 6, Name = "هدى ناصر", Role = ClinicStaffRole.Helper, Phone = "+966 51 111 2222", IsActive = true }, new() { Id = 7, Name = "مصطفى كريم", Role = ClinicStaffRole.Nurse, Phone = "+966 51 333 4444", IsActive = true } },
            4 => new() { new() { Id = 8, Name = "سارة علي", Role = ClinicStaffRole.Reception, Phone = "+966 51 555 6666", IsActive = true }, new() { Id = 9, Name = "أحمد رضا", Role = ClinicStaffRole.Cleaner, Phone = "+966 51 777 8888", IsActive = true } },
            _ => new(),
        };

        public static List<MockVerificationRequest> GetPendingVerifications() => new()
        {
            new() { Id = 1, DoctorName = "د. أحمد علي", Phone = "+966 55 567 8901", Email = "ahmed@clinic.com", SyndicateId = "567890", TaxRegistry = "789-567-890", Degree = "طبيب", Specialty = "طب الأطفال", Photo = "", Documents = new() { "syndicate_card_5.png", "cert_5.png" }, Status = "pending", RequestDate = "2026-07-04" },
            new() { Id = 2, DoctorName = "د. ليلى محمود", Phone = "+966 55 678 9012", Email = "layla@clinic.com", SyndicateId = "678901", TaxRegistry = "789-678-901", Degree = "أخصائي", Specialty = "النساء والولادة", Photo = "", Documents = new() { "syndicate_card_6.png" }, Status = "pending", RequestDate = "2026-07-03" },
        };

        // ========== Support Tickets ==========
        public static List<MockSupportTicket> GetSupportTickets() => new()
        {
            new() { Id = 1, Code = "#1024", Subject = "تعطل نظام تسجيل الدخول", Reporter = "عيادة السلام", Priority = "عالية", PriorityClass = "badge-danger", Status = "مفتوح", StatusClass = "badge-info", Date = "2026-07-03 09:15", HasAttachment = true, Description = "نواجه مشكلة في تسجيل الدخول إلى النظام منذ صباح اليوم. تظهر رسالة خطأ \"تعذر الاتصال بالخادم\" عند محاولة الدخول. الرجاء المساعدة في أقرب وقت ممكن حيث أن جميع العمليات متوقفة.", Attachments = new() { "screenshot_error.png", "error_log.txt" } },
            new() { Id = 2, Code = "#1023", Subject = "استفسار عن فاتورة شهر يونيو", Reporter = "د. سارة أحمد", Priority = "متوسطة", PriorityClass = "badge-warning", Status = "قيد المعالجة", StatusClass = "badge-warning", Date = "2026-07-02 14:30", HasAttachment = false, Description = "أود الاستفسار عن تفاصيل فاتورة شهر يونيو الخاصة بعمليات الطبيب. يظهر مبلغ مختلف عن المتوقع. أرجو توضيح البنود.", Attachments = new() },
            new() { Id = 3, Code = "#1022", Subject = "طلب إضافة تخصص جديد (جراحة التجميل)", Reporter = "مستشفى النور", Priority = "متوسطة", PriorityClass = "badge-warning", Status = "مفتوح", StatusClass = "badge-info", Date = "2026-07-02 10:00", HasAttachment = true, Description = "نرغب في إضافة تخصص جراحة التجميل إلى قائمة التخصصات المتاحة في النظام. المرفقات تحتوي على المستندات المطلوبة والموافقات.", Attachments = new() { "طلب_اضافة_تخصص.pdf" } },
            new() { Id = 4, Code = "#1021", Subject = "مشكلة في عرض التقارير", Reporter = "عيادات التخصصات الدقيقة", Priority = "عالية", PriorityClass = "badge-danger", Status = "قيد المعالجة", StatusClass = "badge-warning", Date = "2026-07-01 16:45", HasAttachment = false, Description = "لا تظهر التقارير الإحصائية بشكل صحيح منذ تحديث النظام الأخير. بعض البيانات مفقودة والرسوم البيانية لا تعمل.", Attachments = new() },
            new() { Id = 5, Code = "#1020", Subject = "اقتراح تحسين واجهة المستخدم", Reporter = "د. خالد الزهراني", Priority = "منخفضة", PriorityClass = "badge-success", Status = "تم الحل", StatusClass = "badge-success", Date = "2026-06-28 11:00", HasAttachment = false, Description = "اقتراح بإضافة زر للتبديل بين الوضع الفاتح والداكن في لوحة التحكم، وإمكانية تخصيص الألوان حسب كل عيادة.", Attachments = new() },
        };
    }

    // ========== User Models ==========
    public class MockUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Initials { get; set; } = "";
        public string RegistrationDate { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusClass { get; set; } = "badge-success";
        public UserRole Role { get; set; } = UserRole.Patient;
        public int TotalVisits { get; set; }
        public double AvgRating { get; set; }
        public string TotalSpent { get; set; } = "";
    }

    public class MockUserOverview
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Initials { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string RegistrationDate { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusClass { get; set; } = "badge-success";
        public UserRole Role { get; set; } = UserRole.Patient;
        public int TotalVisits { get; set; }
        public double AvgRating { get; set; }
        public string TotalSpent { get; set; } = "";
        public List<MockActivity> Activity { get; set; } = new();
    }

    public class MockActivity
    {
        public string Date { get; set; } = "";
        public string Text { get; set; } = "";
        public string Icon { get; set; } = "calendar";
    }

    public class MockUserVisit
    {
        public int Id { get; set; }
        public string Clinic { get; set; } = "";
        public string Doctor { get; set; } = "";
        public string Date { get; set; } = "";
        public string Diagnosis { get; set; } = "";
        public string Notes { get; set; } = "";
        public int DoctorBehavior { get; set; }
        public int ReceptionBehavior { get; set; }
        public int ClinicCleanliness { get; set; }
        public double OverallRating => Math.Round((DoctorBehavior + ReceptionBehavior + ClinicCleanliness) / 3.0, 1);
        public string Comment { get; set; } = "";
    }



    // ========== Clinics Models ==========
    public class MockClinic
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Specialty { get; set; } = "";
        public List<string> Specializations { get; set; } = new();
        public string Description { get; set; } = "";
        public string ResponsibleDoctor { get; set; } = "";
        public string ManagerName { get; set; } = "";
        public string Location { get; set; } = "";
        public double Latitude { get; set; } = 31.0409;
        public double Longitude { get; set; } = 31.3785;
        public int OwnerUserId { get; set; }
        public string Phone { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public double AvgRating { get; set; }
        public int RatingsCount { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class MockPatientRating
    {
        public int Id { get; set; }
        public int ClinicId { get; set; }
        public string DoctorName { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string PatientInitial { get; set; } = "";
        public int Rating { get; set; }
        public string Comment { get; set; } = "";
        public string Date { get; set; } = "";
    }

    public class MockWorkingDay
    {
        public string Day { get; set; } = "";
        public string DayAr { get; set; } = "";
        public string StartTime { get; set; } = "09:00";
        public string EndTime { get; set; } = "17:00";
        public bool IsAvailable { get; set; } = true;
    }

    public class MockDoctorClinicConfig
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = "";
        public string Specialty { get; set; } = "";
        public string Degree { get; set; } = "";
        public string Photo { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public bool IsPrimary { get; set; }
        public List<MockWorkingDay> WorkingDays { get; set; } = new();
        public decimal ExaminationFee { get; set; }
        public decimal FollowUpFee { get; set; }
        public double CommissionPercent { get; set; }
        public decimal CommissionFixed { get; set; }
        public string RoomAssignment { get; set; } = "";
        public string ContractType { get; set; } = ""; // FullTime, PartTime, FixedContract, Visits, RevenueShare
        public string ContractStart { get; set; } = "";
        public string ContractEnd { get; set; } = "";
        public int SessionLimitPerDay { get; set; }
    }

    // ========== Doctors Models ==========
    public class MockDoctor
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string SyndicateId { get; set; } = "";
        public string TaxRegistry { get; set; } = "";
        public string Degree { get; set; } = "";
        public string Specialty { get; set; } = "";
        public DoctorEmploymentType DoctorType { get; set; }
        public int? ClinicId { get; set; } // if OwnClinic -> the clinic they own; if InCenter -> the center they work at
        public string WorkplaceName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Photo { get; set; } = "";
        public List<string> Documents { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    public class MockClinicStaff
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public ClinicStaffRole Role { get; set; }
        public string Phone { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class MockVerificationRequest
    {
        public int Id { get; set; }
        public string DoctorName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string SyndicateId { get; set; } = "";
        public string TaxRegistry { get; set; } = "";
        public string Degree { get; set; } = "";
        public string Specialty { get; set; } = "";
        public string Photo { get; set; } = "";
        public List<string> Documents { get; set; } = new();
        public string Status { get; set; } = "pending"; // pending / approved / rejected
        public string RequestDate { get; set; } = "";
    }

    // ========== Support Tickets Models ==========
    public class MockSupportTicket
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Reporter { get; set; } = "";
        public string Priority { get; set; } = "";
        public string PriorityClass { get; set; } = "badge-warning";
        public string Status { get; set; } = "";
        public string StatusClass { get; set; } = "badge-info";
        public string Date { get; set; } = "";
        public bool HasAttachment { get; set; }
        public string Description { get; set; } = "";
        public List<string> Attachments { get; set; } = new();
    }

}
