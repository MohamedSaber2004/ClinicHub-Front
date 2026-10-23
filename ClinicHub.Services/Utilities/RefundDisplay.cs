namespace ClinicHub.Services.Utilities
{
    /// <summary>
    /// Arabic labels for refund state. Backend <c>PaymentRefundStatus</c> is sent as a
    /// number: 0=None 1=Pending 2=Processing 3=Completed 4=Failed 5=NeedsReview.
    /// </summary>
    public static class RefundDisplay
    {
        public static string StatusText(int status) => status switch
        {
            1 => "قيد الانتظار",
            2 => "قيد التنفيذ",
            3 => "مكتمل",
            4 => "فشل",
            5 => "يحتاج مراجعة يدوية",
            _ => "لا يوجد"
        };

        public static string StatusClass(int status) => status switch
        {
            3 => "badge-success",
            1 or 2 => "badge-warning",
            4 or 5 => "badge-danger",
            _ => "badge-info"
        };

        /// <summary>Cancellation source: 0=ClinicOwner 1=Admin 2=System.</summary>
        public static string SourceText(int? source) => source switch
        {
            1 => "إلغاء من الإدارة",
            2 => "إلغاء تلقائي من النظام",
            0 => "إلغاء من صاحب العيادة",
            _ => "غير محدد"
        };
    }
}
