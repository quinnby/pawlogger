namespace CarCareTracker.Models
{
    /// <summary>
    /// Phase 5 – Classifies what kind of pet care a reminder represents.
    /// Used to group, filter, and label reminders on the pet care timeline.
    /// </summary>
    public enum PetReminderType
    {
        Custom = 0,
        VaccinationDue = 1,
        MedicationRefill = 2,
        MedicationDoseSchedule = 3,
        LicenseRenewal = 4,
        AnnualCheckup = 5,
        FleaTickPrevention = 6,
        HeartwormPrevention = 7,
        Deworming = 8,
        Grooming = 9,
        DentalCleaning = 10,
        WeightCheck = 11,
        /// <summary>Phase 5.1 – Reminder linked to a HealthRecord or VetVisit follow-up date.</summary>
        FollowUpReminder = 12
    }
}
