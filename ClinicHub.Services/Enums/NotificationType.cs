namespace ClinicHub.Services.Enums
{
    public enum NotificationType
    {
        AppointmentReminder = 0,
        NewMessage = 1,
        PaymentConfirmation = 2,
        AppointmentConfirmation = 3,
        AppointmentCancellation = 4,
        SystemAnnouncement = 5,
        CancellationWindowClosed = 6,
        SubscriptionExpiring = 7,
        RefundProcessed = 8,
        AdExpiring = 9,
        AppointmentOutsideAvailability = 10,
        AppointmentOutsideWorkingHours = 11,
        NewBookingRequest = 12,
        ClinicRegistered = 13,
        ClinicApproved = 14,
        ClinicRejected = 15,
        SupportTicketUpdate = 16,
        PaymentReceived = 17,
        RevenueIncreased = 18
    }
}
