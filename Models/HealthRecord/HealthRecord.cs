using System.Text.Json.Serialization;

namespace CarCareTracker.Models
{
    /// <summary>
    /// Phase 3 - HealthRecord: generic health and care event tied to a Pet (VehicleId == PetId).
    /// Extends GenericRecord to reuse Date, Cost, Notes, Files, Tags, ExtraFields infrastructure.
    /// Mileage inherited from GenericRecord is unused / kept at 0 for pets.
    /// </summary>
    public class HealthRecord : GenericRecord
    {
        // ---------------------------------------------------------------
        // Phase 3 – VehicleId compatibility shim
        // VehicleId is the underlying DB column (from GenericRecord) and
        // must NOT be renamed in this pass to avoid schema migrations.
        // PetId is the semantic alias for all new HealthRecord-domain code.
        // Use PetId in Phase 4+ read-path code; assign via VehicleId until
        // the full rename + migration is done.
        // PetId is read-only so LiteDB/Postgres do not map it as a separate column.
        // ---------------------------------------------------------------
        [JsonIgnore]
        public int PetId => VehicleId;

        /// <summary>Short name of the health event (e.g. "Annual Wellness Exam").</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Category of the health event.</summary>
        public HealthRecordCategory Category { get; set; } = HealthRecordCategory.MiscellaneousCare;

        /// <summary>Vet, clinic, groomer, or vendor who provided care.</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>Whether follow-up action is needed.</summary>
        public bool FollowUpRequired { get; set; } = false;

        /// <summary>Date of required follow-up (optional).</summary>
        [JsonConverter(typeof(FromDateOptional))]
        public string FollowUpDate { get; set; } = string.Empty;

        /// <summary>Completion / informational status of this record.</summary>
        public HealthRecordStatus Status { get; set; } = HealthRecordStatus.Completed;

        // Phase 7 – Weight tracking (used when Category == WeightCheck)
        /// <summary>Numeric weight measurement (0 = not set).</summary>
        public decimal WeightValue { get; set; } = 0;
        /// <summary>Unit for the weight value (e.g. "lbs", "kg").</summary>
        public string WeightUnit { get; set; } = string.Empty;

        // Phase 7 – Allergy tracking (used when Category == AllergyReaction)
        /// <summary>Severity of the allergic reaction (Mild / Moderate / Severe / Life-threatening).</summary>
        public string Severity { get; set; } = string.Empty;
        /// <summary>Category of allergy (Food / Medication / Environmental / Contact / Unknown).</summary>
        public string AllergyType { get; set; } = string.Empty;
        /// <summary>Specific allergen or trigger (e.g. "Chicken", "Penicillin", "Pollen").</summary>
        public string Trigger { get; set; } = string.Empty;

        // Phase 7 – Preventive care reminder (used when Category == PreventiveCare)
        /// <summary>Whether a date-based reminder should be created / synced for this record.</summary>
        public bool ReminderEnabled { get; set; } = false;
        /// <summary>Date when next preventive care is due (optional).</summary>
        [JsonConverter(typeof(FromDateOptional))]
        public string ReminderDueDate { get; set; } = string.Empty;

        // Phase 4 integration – specialized record link
        /// <summary>
        /// Id of the specialized record (VaccinationRecord, MedicationRecord, VetVisitRecord,
        /// LicensingRecord) that generated this HealthRecord entry.
        /// 0 means this is a standalone HealthRecord (not backed by a specialized record).
        /// </summary>
        public int LinkedSpecializedRecordId { get; set; } = 0;

        /// <summary>
        /// Discriminator for LinkedSpecializedRecordId.
        /// Values: "Vaccination" | "Medication" | "VetVisit" | "Licensing" | "" (standalone).
        /// Used by the UI to show the linked-record indicator badge.
        /// </summary>
        public string LinkedSpecializedRecordType { get; set; } = string.Empty;
    }
}
