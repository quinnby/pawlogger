namespace CarCareTracker.Models
{
    public class UserConfig
    {
        public bool UseDarkMode { get; set; }
        public bool UseSystemColorMode { get; set; }
        public bool EnableCsvImports { get; set; }
        public bool UseMPG { get; set; }
        public bool UseDescending { get; set; }
        public bool EnableAuth { get; set; }
        public bool HideZero { get; set; }
        public bool UseUKMPG {get;set;}
        public bool UseThreeDecimalGasCost { get; set; }
        public bool UseThreeDecimalGasConsumption { get; set; }
        public bool UseMarkDownOnSavedNotes { get; set; }
        public bool EnableAutoReminderRefresh { get; set; }
        public bool EnableAutoOdometerInsert { get; set; }
        public bool EnableAutoFillOdometer { get; set; }
        public bool EnableShopSupplies { get; set; }
        public bool EnableExtraFieldColumns { get; set; }
        public bool HideSoldVehicles { get; set; }
        public bool AutomaticDecimalFormat { get; set; }
        public string PreferredGasUnit { get; set; } = string.Empty;
        public string PreferredGasMileageUnit { get; set; } = string.Empty;
        public bool UseUnitForFuelCost { get; set; }
        public bool ShowCalendar { get; set; }
        public bool ShowVehicleThumbnail { get; set; }
        public bool ShowSearch { get; set; }
        public bool DisableAutoZoom { get; set; }
        // Phase 7 - Pet measurement preferences
        public string PreferredWeightUnit { get; set; } = "lbs";
        /// <summary>
        /// User's preferred locale for currency and number formatting (e.g., "en_US", "en_GB", "fr_FR").
        /// Falls back to server default if empty.
        /// </summary>
        public string PreferredLocale { get; set; } = string.Empty;
        public List<UserColumnPreference> UserColumnPreferences { get; set; } = new List<UserColumnPreference>();
        public string UserNameHash { get; set; } = string.Empty;
        public string UserPasswordHash { get; set; } = string.Empty;
        public string UserLanguage { get; set; } = "en_US";
        public List<ImportMode> VisibleTabs { get; set; } = new List<ImportMode>() { 
            ImportMode.Dashboard,
            ImportMode.ServiceRecord, 
            ImportMode.RepairRecord, 
            ImportMode.TaxRecord, 
            ImportMode.ReminderRecord, 
            ImportMode.NoteRecord
        };
        public ImportMode DefaultTab { get; set; } = ImportMode.Dashboard;
        public List<ImportMode> TabOrder { get; set; } = new List<ImportMode>() {
            ImportMode.Dashboard,
            ImportMode.PlanRecord,
            ImportMode.ServiceRecord,
            ImportMode.RepairRecord,
            ImportMode.SupplyRecord,
            ImportMode.TaxRecord,
            ImportMode.NoteRecord,
            ImportMode.InspectionRecord,
            ImportMode.EquipmentRecord,
            ImportMode.ReminderRecord,
            // Keep legacy tabs in ordering for compatibility with existing saved settings.
            ImportMode.OdometerRecord,
            ImportMode.UpgradeRecord,
            ImportMode.GasRecord
        };
    }
}