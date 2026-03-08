using CarCareTracker.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Xunit;

namespace CarCareTracker.Tests;

public class ProfileV2RouteContractTests
{
    [Theory]
    [InlineData(nameof(APIController.AllServiceRecords), "/api/vehicle/servicerecords/all")]
    [InlineData(nameof(APIController.AllServiceRecordsV2), "/api/v2/profiles/servicerecords/all")]
    [InlineData(nameof(APIController.ServiceRecords), "/api/vehicle/servicerecords")]
    [InlineData(nameof(APIController.ServiceRecordsV2), "/api/v2/profiles/servicerecords")]
    [InlineData(nameof(APIController.AllGasRecords), "/api/vehicle/gasrecords/all")]
    [InlineData(nameof(APIController.AllGasRecordsV2), "/api/v2/profiles/gasrecords/all")]
    [InlineData(nameof(APIController.GasRecords), "/api/vehicle/gasrecords")]
    [InlineData(nameof(APIController.GasRecordsV2), "/api/v2/profiles/gasrecords")]
    [InlineData(nameof(APIController.AllReminders), "/api/vehicle/reminders/all")]
    [InlineData(nameof(APIController.AllRemindersV2), "/api/v2/profiles/reminders/all")]
    [InlineData(nameof(APIController.Reminders), "/api/vehicle/reminders")]
    [InlineData(nameof(APIController.RemindersV2), "/api/v2/profiles/reminders")]
    [InlineData(nameof(APIController.AllOdometerRecords), "/api/vehicle/odometerrecords/all")]
    [InlineData(nameof(APIController.AllOdometerRecordsV2), "/api/v2/profiles/odometerrecords/all")]
    [InlineData(nameof(APIController.OdometerRecords), "/api/vehicle/odometerrecords")]
    [InlineData(nameof(APIController.OdometerRecordsV2), "/api/v2/profiles/odometerrecords")]
    [InlineData(nameof(APIController.LastOdometer), "/api/vehicle/odometerrecords/latest")]
    [InlineData(nameof(APIController.LastOdometerV2), "/api/v2/profiles/odometerrecords/latest")]
    [InlineData(nameof(APIController.AllPlanRecords), "/api/vehicle/planrecords/all")]
    [InlineData(nameof(APIController.AllPlanRecordsV2), "/api/v2/profiles/planrecords/all")]
    [InlineData(nameof(APIController.PlanRecords), "/api/vehicle/planrecords")]
    [InlineData(nameof(APIController.PlanRecordsV2), "/api/v2/profiles/planrecords")]
    [InlineData(nameof(APIController.AllTaxRecords), "/api/vehicle/taxrecords/all")]
    [InlineData(nameof(APIController.AllTaxRecordsV2), "/api/v2/profiles/taxrecords/all")]
    [InlineData(nameof(APIController.TaxRecords), "/api/vehicle/taxrecords")]
    [InlineData(nameof(APIController.TaxRecordsV2), "/api/v2/profiles/taxrecords")]
    [InlineData(nameof(APIController.AllRepairRecords), "/api/vehicle/repairrecords/all")]
    [InlineData(nameof(APIController.AllRepairRecordsV2), "/api/v2/profiles/repairrecords/all")]
    [InlineData(nameof(APIController.RepairRecords), "/api/vehicle/repairrecords")]
    [InlineData(nameof(APIController.RepairRecordsV2), "/api/v2/profiles/repairrecords")]
    [InlineData(nameof(APIController.AllNotes), "/api/vehicle/notes/all")]
    [InlineData(nameof(APIController.AllNotesV2), "/api/v2/profiles/notes/all")]
    [InlineData(nameof(APIController.Notes), "/api/vehicle/notes")]
    [InlineData(nameof(APIController.NotesV2), "/api/v2/profiles/notes")]
    [InlineData(nameof(APIController.AllUpgradeRecords), "/api/vehicle/upgraderecords/all")]
    [InlineData(nameof(APIController.AllUpgradeRecordsV2), "/api/v2/profiles/upgraderecords/all")]
    [InlineData(nameof(APIController.UpgradeRecords), "/api/vehicle/upgraderecords")]
    [InlineData(nameof(APIController.UpgradeRecordsV2), "/api/v2/profiles/upgraderecords")]
    [InlineData(nameof(APIController.AllEquipmentRecords), "/api/vehicle/equipmentrecords/all")]
    [InlineData(nameof(APIController.AllEquipmentRecordsV2), "/api/v2/profiles/equipmentrecords/all")]
    [InlineData(nameof(APIController.EquipmentRecords), "/api/vehicle/equipmentrecords")]
    [InlineData(nameof(APIController.EquipmentRecordsV2), "/api/v2/profiles/equipmentrecords")]
    [InlineData(nameof(APIController.AllSupplyRecords), "/api/vehicle/supplyrecords/all")]
    [InlineData(nameof(APIController.AllSupplyRecordsV2), "/api/v2/profiles/supplyrecords/all")]
    [InlineData(nameof(APIController.SupplyRecords), "/api/vehicle/supplyrecords")]
    [InlineData(nameof(APIController.SupplyRecordsV2), "/api/v2/profiles/supplyrecords")]
    [InlineData(nameof(APIController.AllHealthRecords), "/api/vehicle/healthrecords/all")]
    [InlineData(nameof(APIController.AllHealthRecordsV2), "/api/v2/profiles/healthrecords/all")]
    [InlineData(nameof(APIController.HealthRecords), "/api/vehicle/healthrecords")]
    [InlineData(nameof(APIController.HealthRecordsV2), "/api/v2/profiles/healthrecords")]
    [InlineData(nameof(APIController.AllVetVisitRecords), "/api/vehicle/vetvisitrecords/all")]
    [InlineData(nameof(APIController.AllVetVisitRecordsV2), "/api/v2/profiles/vetvisitrecords/all")]
    [InlineData(nameof(APIController.VetVisitRecords), "/api/vehicle/vetvisitrecords")]
    [InlineData(nameof(APIController.VetVisitRecordsV2), "/api/v2/profiles/vetvisitrecords")]
    [InlineData(nameof(APIController.AllVaccinationRecords), "/api/vehicle/vaccinationrecords/all")]
    [InlineData(nameof(APIController.AllVaccinationRecordsV2), "/api/v2/profiles/vaccinationrecords/all")]
    [InlineData(nameof(APIController.VaccinationRecords), "/api/vehicle/vaccinationrecords")]
    [InlineData(nameof(APIController.VaccinationRecordsV2), "/api/v2/profiles/vaccinationrecords")]
    [InlineData(nameof(APIController.AllMedicationRecords), "/api/vehicle/medicationrecords/all")]
    [InlineData(nameof(APIController.AllMedicationRecordsV2), "/api/v2/profiles/medicationrecords/all")]
    [InlineData(nameof(APIController.MedicationRecords), "/api/vehicle/medicationrecords")]
    [InlineData(nameof(APIController.MedicationRecordsV2), "/api/v2/profiles/medicationrecords")]
    [InlineData(nameof(APIController.AllLicensingRecords), "/api/vehicle/licensingrecords/all")]
    [InlineData(nameof(APIController.AllLicensingRecordsV2), "/api/v2/profiles/licensingrecords/all")]
    [InlineData(nameof(APIController.LicensingRecords), "/api/vehicle/licensingrecords")]
    [InlineData(nameof(APIController.LicensingRecordsV2), "/api/v2/profiles/licensingrecords")]
    [InlineData(nameof(APIController.AllPetExpenseRecords), "/api/vehicle/petexpenserecords/all")]
    [InlineData(nameof(APIController.AllPetExpenseRecordsV2), "/api/v2/profiles/petexpenserecords/all")]
    [InlineData(nameof(APIController.PetExpenseRecords), "/api/vehicle/petexpenserecords")]
    [InlineData(nameof(APIController.PetExpenseRecordsV2), "/api/v2/profiles/petexpenserecords")]
    public void Route_ShouldContainExpectedTemplate(string actionName, string expectedTemplate)
    {
        var method = typeof(APIController).GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        var routeAttributes = method!.GetCustomAttributes<RouteAttribute>();
        Assert.Contains(routeAttributes, x => string.Equals(x.Template, expectedTemplate, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(nameof(APIController.ServiceRecords))]
    [InlineData(nameof(APIController.GasRecords))]
    [InlineData(nameof(APIController.Reminders))]
    [InlineData(nameof(APIController.OdometerRecords))]
    [InlineData(nameof(APIController.LastOdometer))]
    [InlineData(nameof(APIController.PlanRecords))]
    [InlineData(nameof(APIController.TaxRecords))]
    [InlineData(nameof(APIController.RepairRecords))]
    [InlineData(nameof(APIController.Notes))]
    [InlineData(nameof(APIController.UpgradeRecords))]
    [InlineData(nameof(APIController.EquipmentRecords))]
    [InlineData(nameof(APIController.SupplyRecords))]
    [InlineData(nameof(APIController.HealthRecords))]
    [InlineData(nameof(APIController.VetVisitRecords))]
    [InlineData(nameof(APIController.VaccinationRecords))]
    [InlineData(nameof(APIController.MedicationRecords))]
    [InlineData(nameof(APIController.LicensingRecords))]
    [InlineData(nameof(APIController.PetExpenseRecords))]
    public void LegacyReadRoutes_ShouldAcceptPetProfileAliasParameter(string actionName)
    {
        var method = typeof(APIController).GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        var hasPetProfileId = method!.GetParameters().Any(p => p.Name == "petProfileId" && p.ParameterType == typeof(int));
        Assert.True(hasPetProfileId);
    }
}
