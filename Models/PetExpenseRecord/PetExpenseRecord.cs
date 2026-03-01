namespace CarCareTracker.Models
{
    /// <summary>
    /// Phase 6 – Centralized pet expense record.
    /// Extends GenericRecord to reuse Date, Cost, Notes, Files, Tags, ExtraFields infrastructure.
    /// VehicleId (from GenericRecord) == PetId; not renamed to preserve schema compatibility.
    /// Mileage inherited from GenericRecord is unused for pet expenses and kept at 0.
    /// </summary>
    public class PetExpenseRecord : GenericRecord
    {
        /// <summary>Category of the expense (Vet, Medication, Grooming, etc.).</summary>
        public PetExpenseCategory Category { get; set; } = PetExpenseCategory.Other;

        /// <summary>Vendor, store, clinic, or service provider where the expense was incurred.</summary>
        public string Vendor { get; set; } = string.Empty;

        /// <summary>Whether this expense recurs on a regular basis.</summary>
        public bool IsRecurring { get; set; } = false;

        /// <summary>
        /// Optional loose reference to a related HealthRecord timeline entry.
        /// 0 means no linked record.
        /// Deleting an expense does NOT cascade-delete the linked HealthRecord.
        /// </summary>
        public int LinkedHealthRecordId { get; set; } = 0;
    }
}
