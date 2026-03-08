using System.Text.Json.Serialization;

namespace CarCareTracker.Models
{
    public class GenericRecord
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        // Internal alias only; storage/API compatibility stays on VehicleId.
        [JsonIgnore]
        public int PetProfileId
        {
            get => VehicleId;
            set => VehicleId = value;
        }
        public DateTime Date { get; set; }
        public int Mileage { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public string Notes { get; set; } = string.Empty;
        public List<UploadedFiles> Files { get; set; } = new List<UploadedFiles>();
        public List<string> Tags { get; set;} = new List<string>();
        public List<ExtraField> ExtraFields { get; set; } = new List<ExtraField>();
        public List<SupplyUsageHistory> RequisitionHistory { get; set; } = new List<SupplyUsageHistory>();
    }
}
