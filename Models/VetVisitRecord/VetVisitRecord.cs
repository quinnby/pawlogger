using System.Text.Json.Serialization;

namespace CarCareTracker.Models
{
    /// <summary>
    /// Phase 4 – Structured vet visit record for a pet.
    /// Date from GenericRecord is the visit date.
    /// </summary>
    public class VetVisitRecord : GenericRecord
    {
        /// <summary>Name of the clinic or veterinary practice.</summary>
        public string Clinic { get; set; } = string.Empty;

        /// <summary>Name of the attending veterinarian.</summary>
        public string Veterinarian { get; set; } = string.Empty;

        /// <summary>Why the pet was brought in (e.g. "Annual exam", "Limping").</summary>
        public string ReasonForVisit { get; set; } = string.Empty;

        /// <summary>Symptoms reported by the owner at the time of the visit.</summary>
        public string SymptomsReported { get; set; } = string.Empty;

        /// <summary>Diagnosis made by the vet.</summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>Treatment provided during the visit.</summary>
        public string TreatmentProvided { get; set; } = string.Empty;

        /// <summary>Whether a follow-up appointment is recommended.</summary>
        public bool FollowUpNeeded { get; set; } = false;

        /// <summary>Suggested follow-up date (optional).</summary>
        [JsonConverter(typeof(FromDateOptional))]
        public string FollowUpDate { get; set; } = string.Empty;

        /// <summary>
        /// Optional reference back to a generic HealthRecord on the main timeline.
        /// 0 means no linked record.
        /// </summary>
        public int LinkedHealthRecordId { get; set; } = 0;

        // Phase 5.1 – Follow-up reminder
        /// <summary>When true and FollowUpDate is set, creates/updates a date-based reminder for this visit's follow-up.</summary>
        public bool ReminderEnabled { get; set; } = false;
    }
}
