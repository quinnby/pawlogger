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
        public IActionResult GetLicensingRecordsByVehicleId(int vehicleId)
        {
            var result = _licensingRecordDataAccess.GetLicensingRecordsByVehicleId(vehicleId);
            bool _useDescending = _config.GetUserConfig(User).UseDescending;
            if (_useDescending)
            {
                result = result.OrderByDescending(x => x.Date).ToList();
            }
            else
            {
                result = result.OrderBy(x => x.Date).ToList();
            }
            return PartialView("Licensing/_LicensingRecords", result);
        }

        [HttpGet]
        public IActionResult GetAddLicensingRecordPartialView()
        {
            return PartialView("Licensing/_LicensingRecordModal", new LicensingRecordInput());
        }

        [HttpGet]
        public IActionResult GetLicensingRecordForEditById(int licensingRecordId)
        {
            var result = _licensingRecordDataAccess.GetLicensingRecordById(licensingRecordId);
            if (!_userLogic.UserCanEditVehicle(GetUserID(), result.VehicleId, HouseholdPermission.View))
            {
                return Redirect("/Error/Unauthorized");
            }
            var convertedResult = new LicensingRecordInput
            {
                Id = result.Id,
                VehicleId = result.VehicleId,
                Date = result.Date.ToShortDateString(),
                LicenseNumber = result.LicenseNumber,
                Issuer = result.Issuer,
                ExpiryDate = result.ExpiryDate,
                RenewalReminderEnabled = result.RenewalReminderEnabled,
                LinkedHealthRecordId = result.LinkedHealthRecordId,
                Cost = result.Cost,
                Notes = result.Notes,
                Description = result.Description,
                Files = result.Files,
                Tags = result.Tags,
                ExtraFields = result.ExtraFields
            };
            return PartialView("Licensing/_LicensingRecordModal", convertedResult);
        }

        [HttpPost]
        public IActionResult SaveLicensingRecordToVehicleId(LicensingRecordInput licensingRecord)
        {
            if (!_userLogic.UserCanEditVehicle(GetUserID(), licensingRecord.VehicleId, HouseholdPermission.Edit))
            {
                return Json(OperationResponse.Failed("Access Denied"));
            }
            licensingRecord.Files = licensingRecord.Files
                .Select(x => new UploadedFiles
                {
                    Name = x.Name,
                    Location = _fileHelper.MoveFileFromTemp(x.Location, "documents/")
                }).ToList();

            var convertedRecord = licensingRecord.ToLicensingRecord();
            var result = _licensingRecordDataAccess.SaveLicensingRecord(convertedRecord);
            if (result)
            {
                // Phase 4 – Create or update the linked HealthRecord timeline entry.
                int priorLinkedHealthId = licensingRecord.LinkedHealthRecordId;
                var licenseTitle = !string.IsNullOrWhiteSpace(licensingRecord.LicenseNumber)
                    ? $"License: {licensingRecord.LicenseNumber}"
                    : "License / Registration";
                var noteParts = new[]
                {
                    string.IsNullOrWhiteSpace(licensingRecord.ExpiryDate) ? null : $"Expires: {licensingRecord.ExpiryDate}",
                    licensingRecord.Notes
                }.Where(s => !string.IsNullOrWhiteSpace(s));
                var projectedHealthRecord = new HealthRecord
                {
                    VehicleId = convertedRecord.VehicleId,
                    Date = convertedRecord.Date,
                    Category = HealthRecordCategory.Licensing,
                    Title = licenseTitle,
                    Provider = licensingRecord.Issuer,
                    Notes = string.Join("\n", noteParts),
                    Cost = licensingRecord.Cost,
                    Status = HealthRecordStatus.Completed,
                    FollowUpRequired = !string.IsNullOrWhiteSpace(licensingRecord.ExpiryDate),
                    FollowUpDate = licensingRecord.ExpiryDate
                };
                int linkedHealthId = SyncLinkedHealthRecord(
                    projectedHealthRecord, priorLinkedHealthId, "Licensing", convertedRecord.Id);
                if (linkedHealthId > 0 && linkedHealthId != priorLinkedHealthId)
                {
                    convertedRecord.LinkedHealthRecordId = linkedHealthId;
                    _licensingRecordDataAccess.SaveLicensingRecord(convertedRecord);
                }

                // Phase 5 – Sync reminder when ExpiryDate is set and RenewalReminderEnabled is toggled
                var reminderDescription = !string.IsNullOrWhiteSpace(licensingRecord.LicenseNumber)
                    ? $"License Renewal: {licensingRecord.LicenseNumber}"
                    : "License Renewal";
                SyncReminderFromLinkedRecord(
                    petId: convertedRecord.VehicleId,
                    reminderEnabled: licensingRecord.RenewalReminderEnabled,
                    dueDateString: licensingRecord.ExpiryDate,
                    description: reminderDescription,
                    petReminderType: PetReminderType.LicenseRenewal,
                    linkedRecordType: ReminderLinkedRecordType.Licensing,
                    linkedRecordId: convertedRecord.Id);
            }
            return Json(OperationResponse.Conditional(result, string.Empty, StaticHelper.GenericErrorMessage));
        }

        [HttpPost]
        public IActionResult DeleteLicensingRecordById(int licensingRecordId)
        {
            var existingRecord = _licensingRecordDataAccess.GetLicensingRecordById(licensingRecordId);
            if (!_userLogic.UserCanEditVehicle(GetUserID(), existingRecord.VehicleId, HouseholdPermission.Delete))
            {
                return Json(OperationResponse.Failed("Access Denied"));
            }
            // Phase 5.1 – Clean up the Licensing's linked reminder before deleting the record,
            // and clean up any reminders linked to the projected HealthRecord entry.
            DeleteAllLinkedReminders(existingRecord.VehicleId, ReminderLinkedRecordType.Licensing, existingRecord.Id);
            // Phase 4.2 – Cascade-delete the auto-projected HealthRecord timeline entry.
            // The linked HealthRecord exists solely as a projection of this specialized record
            // and has no independent meaning without it. Delete it here so it does not become
            // an unmanaged orphan on the timeline.
            if (existingRecord.LinkedHealthRecordId > 0)
            {
                DeleteAllLinkedReminders(existingRecord.VehicleId, ReminderLinkedRecordType.HealthRecord, existingRecord.LinkedHealthRecordId);
                _healthRecordDataAccess.DeleteHealthRecordById(existingRecord.LinkedHealthRecordId);
            }
            var result = _licensingRecordDataAccess.DeleteLicensingRecordById(existingRecord.Id);
            return Json(OperationResponse.Conditional(result, string.Empty, StaticHelper.GenericErrorMessage));
        }
    }
}
