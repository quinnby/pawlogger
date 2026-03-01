namespace CarCareTracker.Models
{
    public class VetVisitRecordInput
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string Date { get; set; } = DateTime.Now.ToShortDateString();
        public string Clinic { get; set; } = string.Empty;
        public string Veterinarian { get; set; } = string.Empty;
        public string ReasonForVisit { get; set; } = string.Empty;
        public string SymptomsReported { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string TreatmentProvided { get; set; } = string.Empty;
        public bool FollowUpNeeded { get; set; } = false;
        public string FollowUpDate { get; set; } = string.Empty;
        public int LinkedHealthRecordId { get; set; } = 0;
        // Phase 5.1 – Follow-up reminder
        public bool ReminderEnabled { get; set; } = false;
        public decimal Cost { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<UploadedFiles> Files { get; set; } = new List<UploadedFiles>();
        public List<string> Tags { get; set; } = new List<string>();
        public List<ExtraField> ExtraFields { get; set; } = new List<ExtraField>();

        public VetVisitRecord ToVetVisitRecord()
        {
            return new VetVisitRecord
            {
                Id = Id,
                VehicleId = VehicleId,
                Date = string.IsNullOrWhiteSpace(Date) ? DateTime.Now : DateTime.Parse(Date),
                Clinic = Clinic,
                Veterinarian = Veterinarian,
                ReasonForVisit = ReasonForVisit,
                SymptomsReported = SymptomsReported,
                Diagnosis = Diagnosis,
                TreatmentProvided = TreatmentProvided,
                FollowUpNeeded = FollowUpNeeded,
                FollowUpDate = FollowUpDate,
                LinkedHealthRecordId = LinkedHealthRecordId,
                ReminderEnabled = ReminderEnabled,
                Cost = Cost,
                Notes = Notes,
                Description = Description,
                Files = Files,
                Tags = Tags,
                ExtraFields = ExtraFields
            };
        }
    }
}
