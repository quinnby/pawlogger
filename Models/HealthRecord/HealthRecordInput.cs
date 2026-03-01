namespace CarCareTracker.Models
{
    public class HealthRecordInput
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string Date { get; set; } = DateTime.Now.ToShortDateString();
        public HealthRecordCategory Category { get; set; } = HealthRecordCategory.MiscellaneousCare;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public bool FollowUpRequired { get; set; } = false;
        public string FollowUpDate { get; set; } = string.Empty;
        public HealthRecordStatus Status { get; set; } = HealthRecordStatus.Completed;
        public List<UploadedFiles> Files { get; set; } = new List<UploadedFiles>();
        public List<string> Tags { get; set; } = new List<string>();
        public List<ExtraField> ExtraFields { get; set; } = new List<ExtraField>();

        public HealthRecord ToHealthRecord()
        {
            return new HealthRecord
            {
                Id = Id,
                VehicleId = VehicleId,
                Date = string.IsNullOrWhiteSpace(Date) ? DateTime.Now : DateTime.Parse(Date),
                Category = Category,
                Title = Title,
                Description = Description,
                Cost = Cost,
                Notes = Notes,
                Provider = Provider,
                FollowUpRequired = FollowUpRequired,
                FollowUpDate = FollowUpDate,
                Status = Status,
                Files = Files,
                Tags = Tags,
                ExtraFields = ExtraFields
            };
        }
    }
}
