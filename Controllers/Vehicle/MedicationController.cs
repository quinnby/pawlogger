using CarCareTracker.Filter;
using CarCareTracker.Helper;
using CarCareTracker.Models;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers
{
    public partial class VehicleController
    {
        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        public IActionResult GetMedicationRecordsByVehicleId(int vehicleId)
        {
            var result = _medicationRecordDataAccess.GetMedicationRecordsByVehicleId(vehicleId);
            bool _useDescending = _config.GetUserConfig(User).UseDescending;
            if (_useDescending)
            {
                result = result.OrderByDescending(x => x.Date).ToList();
            }
            else
            {
                result = result.OrderBy(x => x.Date).ToList();
            }
            return PartialView("Medication/_MedicationRecords", result);
        }

        [HttpGet]
        public IActionResult GetAddMedicationRecordPartialView()
        {
            return PartialView("Medication/_MedicationRecordModal", new MedicationRecordInput());
        }

        [HttpGet]
        public IActionResult GetMedicationRecordForEditById(int medicationRecordId)
        {
            var result = _medicationRecordDataAccess.GetMedicationRecordById(medicationRecordId);
            if (!_userLogic.UserCanEditVehicle(GetUserID(), result.VehicleId, HouseholdPermission.View))
            {
                return Redirect("/Error/Unauthorized");
            }
            var convertedResult = new MedicationRecordInput
            {
                Id = result.Id,
                VehicleId = result.VehicleId,
                Date = result.Date.ToShortDateString(),
                MedicationName = result.MedicationName,
                Dosage = result.Dosage,
                Unit = result.Unit,
                Frequency = result.Frequency,
                Route = result.Route,
                EndDate = result.EndDate,
                PrescribingVet = result.PrescribingVet,
                Purpose = result.Purpose,
                RefillDate = result.RefillDate,
                ReminderEnabled = result.ReminderEnabled,
                IsActive = result.IsActive,
                LinkedHealthRecordId = result.LinkedHealthRecordId,
                Cost = result.Cost,
                Notes = result.Notes,
                Files = result.Files,
                Tags = result.Tags,
                ExtraFields = result.ExtraFields
            };
            return PartialView("Medication/_MedicationRecordModal", convertedResult);
        }

        [HttpPost]
        public IActionResult SaveMedicationRecordToVehicleId(MedicationRecordInput medicationRecord)
        {
            if (!_userLogic.UserCanEditVehicle(GetUserID(), medicationRecord.VehicleId, HouseholdPermission.Edit))
            {
                return Json(OperationResponse.Failed("Access Denied"));
            }
            medicationRecord.Files = medicationRecord.Files
                .Select(x => new UploadedFiles
                {
                    Name = x.Name,
                    Location = _fileHelper.MoveFileFromTemp(x.Location, "documents/")
                }).ToList();

            var convertedRecord = medicationRecord.ToMedicationRecord();
            var result = _medicationRecordDataAccess.SaveMedicationRecord(convertedRecord);
            if (result)
            {
                // Phase 4 – Create or update the linked HealthRecord timeline entry.
                int priorLinkedHealthId = medicationRecord.LinkedHealthRecordId;
                var noteParts = new[]
                {
                    string.IsNullOrWhiteSpace(medicationRecord.Dosage) ? null
                        : $"Dose: {medicationRecord.Dosage}{(string.IsNullOrWhiteSpace(medicationRecord.Unit) ? "" : " " + medicationRecord.Unit)}",
                    string.IsNullOrWhiteSpace(medicationRecord.Frequency) ? null : $"Frequency: {medicationRecord.Frequency}",
                    string.IsNullOrWhiteSpace(medicationRecord.Route) ? null : $"Route: {medicationRecord.Route}",
                    string.IsNullOrWhiteSpace(medicationRecord.Purpose) ? null : $"Purpose: {medicationRecord.Purpose}",
                    medicationRecord.Notes
                }.Where(s => !string.IsNullOrWhiteSpace(s));
                var projectedHealthRecord = new HealthRecord
                {
                    VehicleId = convertedRecord.VehicleId,
                    Date = convertedRecord.Date,
                    Category = HealthRecordCategory.Medication,
                    Title = !string.IsNullOrWhiteSpace(medicationRecord.MedicationName)
                        ? medicationRecord.MedicationName
                        : "Medication",
                    Provider = medicationRecord.PrescribingVet,
                    Notes = string.Join("\n", noteParts),
                    Cost = medicationRecord.Cost,
                    Status = medicationRecord.IsActive ? HealthRecordStatus.Open : HealthRecordStatus.Completed,
                    FollowUpRequired = !string.IsNullOrWhiteSpace(medicationRecord.RefillDate),
                    FollowUpDate = medicationRecord.RefillDate
                };
                int linkedHealthId = SyncLinkedHealthRecord(
                    projectedHealthRecord, priorLinkedHealthId, "Medication", convertedRecord.Id);
                if (linkedHealthId > 0 && linkedHealthId != priorLinkedHealthId)
                {
                    convertedRecord.LinkedHealthRecordId = linkedHealthId;
                    _medicationRecordDataAccess.SaveMedicationRecord(convertedRecord);
                }

                // Phase 5 – Sync reminder when RefillDate is set and ReminderEnabled is toggled
                SyncReminderFromLinkedRecord(
                    petId: convertedRecord.VehicleId,
                    reminderEnabled: medicationRecord.ReminderEnabled,
                    dueDateString: medicationRecord.RefillDate,
                    description: $"Medication Refill: {medicationRecord.MedicationName}",
                    petReminderType: PetReminderType.MedicationRefill,
                    linkedRecordType: ReminderLinkedRecordType.Medication,
                    linkedRecordId: convertedRecord.Id);
            }
            return Json(OperationResponse.Conditional(result, string.Empty, StaticHelper.GenericErrorMessage));
        }

        [HttpPost]
        public IActionResult DeleteMedicationRecordById(int medicationRecordId)
        {
            var existingRecord = _medicationRecordDataAccess.GetMedicationRecordById(medicationRecordId);
            if (!_userLogic.UserCanEditVehicle(GetUserID(), existingRecord.VehicleId, HouseholdPermission.Delete))
            {
                return Json(OperationResponse.Failed("Access Denied"));
            }
            // Phase 5.1 – Clean up the Medication's linked reminder before deleting the record,
            // and clean up any reminders linked to the projected HealthRecord entry.
            DeleteAllLinkedReminders(existingRecord.VehicleId, ReminderLinkedRecordType.Medication, existingRecord.Id);
            // Phase 4.2 – Cascade-delete the auto-projected HealthRecord timeline entry.
            // The linked HealthRecord exists solely as a projection of this specialized record
            // and has no independent meaning without it. Delete it here so it does not become
            // an unmanaged orphan on the timeline.
            if (existingRecord.LinkedHealthRecordId > 0)
            {
                DeleteAllLinkedReminders(existingRecord.VehicleId, ReminderLinkedRecordType.HealthRecord, existingRecord.LinkedHealthRecordId);
                _healthRecordDataAccess.DeleteHealthRecordById(existingRecord.LinkedHealthRecordId);
            }
            var result = _medicationRecordDataAccess.DeleteMedicationRecordById(existingRecord.Id);
            return Json(OperationResponse.Conditional(result, string.Empty, StaticHelper.GenericErrorMessage));
        }
    }
}
