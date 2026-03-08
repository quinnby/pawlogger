using System.Text.Json.Serialization;

namespace CarCareTracker.Models
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string ImageLocation { get; set; } = "/defaults/noimage.png";
        public string MapLocation { get; set; } = "";
        public int Year { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        [JsonConverter(typeof(FromDateOptional))]
        public string PurchaseDate { get; set; } = string.Empty;
        [JsonConverter(typeof(FromDateOptional))]
        public string SoldDate { get; set; } = string.Empty;
        public decimal PurchasePrice { get; set; }
        public decimal SoldPrice { get; set; }
        public bool IsElectric { get; set; } = false;
        public bool IsDiesel { get; set; } = false;
        public bool UseHours { get; set; } = false;
        public bool OdometerOptional { get; set; } = false;
        public List<ExtraField> ExtraFields { get; set; } = new List<ExtraField>();
        public List<string> Tags { get; set; } = new List<string>();
        public bool HasOdometerAdjustment { get; set; } = false;
        /// <summary>
        /// Primarily used for vehicles with odometer units different from user's settings.
        /// </summary>
        [JsonConverter(typeof(FromDecimalOptional))]
        public string OdometerMultiplier { get; set; } = "1";
        /// <summary>
        /// Primarily used for vehicles where the odometer does not reflect actual mileage.
        /// </summary>
        [JsonConverter(typeof(FromIntOptional))]
        public string OdometerDifference { get; set; } = "0";
        public List<DashboardMetric> DashboardMetrics { get; set; } = new List<DashboardMetric>();
        /// <summary>
        /// Determines what is displayed in place of the license plate.
        /// </summary>
        public string VehicleIdentifier { get; set; } = "LicensePlate";

        // Phase 2 - Pet profile fields
        public string PetName { get; set; } = string.Empty;
        public string Species { get; set; } = string.Empty;
        public string Breed { get; set; } = string.Empty;
        /// <summary>Male, Female, or Unknown</summary>
        public string PetSex { get; set; } = string.Empty;
        [JsonConverter(typeof(FromDateOptional))]
        public string DateOfBirth { get; set; } = string.Empty;
        public bool IsEstimatedAge { get; set; } = false;
        /// <summary>Free-form string so units (lbs/kg) can be included.</summary>
        public string CurrentWeight { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string MicrochipNumber { get; set; } = string.Empty;
        public bool IsSpayedNeutered { get; set; } = false;
        public PetStatus PetStatus { get; set; } = PetStatus.Active;

        // Phase 2 - Optional pet profile fields
        [JsonConverter(typeof(FromDateOptional))]
        public string AdoptionDate { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string PrimaryVet { get; set; } = string.Empty;
        public string EmergencyContact { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;

        // Internal alias only; persistence contract remains unchanged.
        [JsonIgnore]
        public string ProfileName => !string.IsNullOrWhiteSpace(PetName) ? PetName : $"{Year} {Make} {Model}".Trim();
    }
}
