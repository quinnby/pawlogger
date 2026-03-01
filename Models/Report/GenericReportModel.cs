namespace CarCareTracker.Models
{
    /// <summary>
    /// Generic Model used for vehicle/pet history report.
    /// </summary>
    public class GenericReportModel
    {
        public ImportMode DataType { get; set; }
        public DateTime Date { get; set; }
        public int Odometer { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public List<UploadedFiles> Files { get; set; } = new List<UploadedFiles>();
        public List<ExtraField> ExtraFields { get; set; } = new List<ExtraField>();
        public List<SupplyUsageHistory> RequisitionHistory { get; set; } = new List<SupplyUsageHistory>();

        // Phase reporting – pet-specific fields (only populated for HealthRecord rows)
        /// <summary>Numeric weight measurement; 0 means not recorded.</summary>
        public decimal WeightValue { get; set; } = 0;
        /// <summary>Unit for the weight value (e.g. "lbs", "kg").</summary>
        public string WeightUnit { get; set; } = string.Empty;
        /// <summary>Vet / clinic / groomer / provider name (HealthRecord only).</summary>
        public string Provider { get; set; } = string.Empty;
        /// <summary>Human-readable category label (HealthRecord only, e.g. "Weight Check").</summary>
        public string Category { get; set; } = string.Empty;
    }
}
