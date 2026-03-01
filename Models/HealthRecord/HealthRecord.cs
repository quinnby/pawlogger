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
    }
}
