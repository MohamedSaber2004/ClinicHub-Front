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

    public static class MockData
    {
        public const string CurrencySymbol = "ج.م";
        // ========== Dashboard ==========
        public static List<MockStat> GetDashboardStats() => new()
        {
            new() { Value = "156", Label = "إجمالي الأطباء", IconColor = "primary", SvgPath = "M12 2C9.243 2 7 4.243 7 7v2c0 2.206 1.794 4 4 4v2.184c-2.804.418-4.994 2.617-4.994 5.316V22h2v-1.5c0-2.206 1.794-4 4-4s4 1.794 4 4V22h2v-1.5c0-2.699-2.19-4.898-4.994-5.316V13c2.206 0 4-1.794 4-4V7c0-2.757-2.243-5-5-5zM9 7c0-1.654 1.346-3 3-3s3 1.346 3 3v2c0 1.654-1.346 3-3 3s-3-1.346-3-3V7z" },
            new() { Value = "24", Label = "العيادات النشطة", IconColor = "blue", SvgPath = "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V5h14v14zm-7-2h2v-4h4v-2h-4V7h-2v4H8v2h4z" },
            new() { Value = "6", Label = "التخصصات الطبية", IconColor = "green", SvgPath = "M21.41 11.58l-9-9C12.05 2.22 11.55 2 11 2H4c-1.1 0-2 .9-2 2v7c0 .55.22 1.05.59 1.42l9 9c.36.36.86.58 1.41.58.55 0 1.05-.22 1.41-.59l7-7c.37-.36.59-.86.59-1.41 0-.55-.23-1.06-.59-1.42zM5.5 7C4.67 7 4 6.33 4 5.5S4.67 4 5.5 4 7 4.67 7 5.5 6.33 7 5.5 7z" },
            new() { Value = "48", Label = "المشتركين", IconColor = "amber", SvgPath = "M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5s-3 1.34-3 3 1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 2 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z" },
            new() { Value = "2,847", Label = "إجمالي المرضى", IconColor = "primary", SvgPath = "M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5s-3 1.34-3 3 1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 2 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z" },
            new() { Value = "3", Label = "تذاكر مفتوحة", IconColor = "primary", SvgPath = "M19 12v-2c0-3.866-3.134-7-7-7S5 6.134 5 10v2H3v7h3v-9c0-3.313 2.687-6 6-6s6 2.687 6 6v9h3v-7h-2zM7 15v2c0 1.103.897 2 2 2h2v-6H7v2zm8 0h-2v2h2c1.103 0 2-.897 2-2v-2h-2v2z" },
        };

        public static List<MockQuickStatus> GetQuickStatuses() => new()
        {
            new() { Text = "12 تخصص نشط", Color = "success" },
            new() { Text = "3 تذاكر قيد المعالجة", Color = "warning" },
            new() { Text = "5 إعلانات مجدولة", Color = "info" },
            new() { Text = "2 اشتراك منتهي", Color = "danger" },
            new() { Text = "4 أطباء مناوبون", Color = "success" },
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
    }
}
