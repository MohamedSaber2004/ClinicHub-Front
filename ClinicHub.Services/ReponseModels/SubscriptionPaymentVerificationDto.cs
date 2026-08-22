using System;

namespace ClinicHub.Services.ReponseModels
{
    /// <summary>
    /// Result of asking the backend to verify the clinic's most recent subscription
    /// payment (local state first, then a direct Paymob inquiry).
    /// </summary>
    public class SubscriptionPaymentVerificationDto
    {
        /// <summary>"paid" | "pending" | "failed" | "none"</summary>
        public string Status { get; set; } = "none";

        public bool SubscriptionActive { get; set; }

        public DateTime? EndDate { get; set; }

        public string? PlanName { get; set; }
    }
}
