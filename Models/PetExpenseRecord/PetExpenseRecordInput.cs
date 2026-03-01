namespace CarCareTracker.Models
{
    /// <summary>
    /// Phase 6 – Input/form model for PetExpenseRecord.
    /// Mirrors the VetVisitRecordInput / MedicationRecordInput pattern.
    /// </summary>
    public class PetExpenseRecordInput
    {
        public int Id { get; set; }
        /// <summary>VehicleId == PetId (compatibility shim; not renamed this phase).</summary>
        public int VehicleId { get; set; }
        public string Date { get; set; } = DateTime.Now.ToShortDateString();
        public PetExpenseCategory Category { get; set; } = PetExpenseCategory.Other;
        public string Vendor { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public bool IsRecurring { get; set; } = false;
        public int LinkedHealthRecordId { get; set; } = 0;
        public string Notes { get; set; } = string.Empty;
        public List<UploadedFiles> Files { get; set; } = new List<UploadedFiles>();
        public List<string> Tags { get; set; } = new List<string>();
        public List<ExtraField> ExtraFields { get; set; } = new List<ExtraField>();

        public PetExpenseRecord ToPetExpenseRecord()
        {
            return new PetExpenseRecord
            {
                Id = Id,
                VehicleId = VehicleId,
                Date = string.IsNullOrWhiteSpace(Date) ? DateTime.Now : DateTime.Parse(Date),
                Category = Category,
                Vendor = Vendor,
                Description = Description,
                Cost = Cost,
                IsRecurring = IsRecurring,
                LinkedHealthRecordId = LinkedHealthRecordId,
                Notes = Notes,
                Files = Files,
                Tags = Tags,
                ExtraFields = ExtraFields
            };
        }
    }
}
