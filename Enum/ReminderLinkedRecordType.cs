namespace CarCareTracker.Models
{
    /// <summary>
    /// Phase 5 – Identifies which type of pet-health record automatically created this reminder,
    /// so that the reminder can be found and updated when the source record changes.
    /// </summary>
    public enum ReminderLinkedRecordType
    {
        None = 0,
        Vaccination = 1,
        Medication = 2,
        Licensing = 3,
        /// <summary>Phase 7 – Linked to a generic HealthRecord (e.g. PreventiveCare).</summary>
        HealthRecord = 4,
        /// <summary>Phase 5.1 – Linked to a VetVisitRecord follow-up date.</summary>
        VetVisit = 5
    }
}
