using ClinicHub.Services.ReponseModels;

namespace ClinicHub.Services.Utilities
{
    /// <summary>
    /// Turns a cancellation result into Arabic copy for the confirmation modal.
    /// Cancellation itself always succeeds once the API returns 200; only the money
    /// can be pending, failed or awaiting manual review, so the wording tracks
    /// <c>RefundStatus</c> (0=None 1=Pending 2=Processing 3=Completed 4=Failed 5=NeedsReview).
    /// </summary>
    public static class CancellationMessages
    {
        public static string ForResult(CancelSubscriptionResponseDto result)
        {
            if (result == null) return "تم إلغاء الاشتراك.";

            var currency = string.IsNullOrWhiteSpace(result.Currency) ? "EGP" : result.Currency;
            // No explicit culture: match the formatting the views already use.
            var amount = result.RefundAmount.ToString("N2");

            if (result.RefundAmount <= 0)
                return "تم إلغاء الاشتراك دون استرداد أي مبلغ.";

            return result.RefundStatus switch
            {
                3 => $"تم إلغاء الاشتراك واسترداد {amount} {currency}.",
                1 or 2 => $"تم إلغاء الاشتراك، وجارٍ استرداد {amount} {currency}. سيتم تحديث الحالة بعد تأكيد بوابة الدفع.",
                4 => $"تم إلغاء الاشتراك، لكن تعذّر استرداد {amount} {currency} تلقائياً. برجاء المتابعة مع الدعم الفني.",
                5 => $"تم إلغاء الاشتراك، واسترداد {amount} {currency} قيد المراجعة اليدوية.",
                _ => $"تم إلغاء الاشتراك. المبلغ المستحق للاسترداد {amount} {currency}."
            };
        }
    }
}
