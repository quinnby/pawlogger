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
        public IActionResult GetVetVisitRecordsByVehicleId(int vehicleId)
        {
            var result = _vetVisitRecordDataAccess.GetVetVisitRecordsByVehicleId(vehicleId);
            bool _useDescending = _config.GetUserConfig(User).UseDescending;
            if (_useDescending)
            {
                result = result.OrderByDescending(x => x.Date).ToList();
            }
            else
            {
                result = result.OrderBy(x => x.Date).ToList();
            }
            return PartialView("VetVisit/_VetVisitRecords", result);
        }

        [HttpGet]
        public IActionResult GetAddVetVisitRecordPartialView()
        {
            return PartialView("VetVisit/_VetVisitRecordModal", new VetVisitRecordInput());
        }

        [HttpGet]
        public IActionResult GetVetVisitRecordForEditById(int vetVisitRecordId)
        {
            var result = _vetVisitRecordDataAccess.GetVetVisitRecordById(vetVisitRecordId);
            if (!_userLogic.UserCanEditVehicle(GetUserID(), result.VehicleId, HouseholdPermission.View))
            {
                return Redirect("/Error/Unauthorized");
            }
            var convertedResult = new VetVisitRecordInput
            {
                Id = result.Id,
                VehicleId = result.VehicleId,
                Date = result.Date.ToShortDateString(),
                Clinic = result.Clinic,
                Veterinarian = result.Veterinarian,
                ReasonForVisit = result.ReasonForVisit,
                SymptomsReported = result.SymptomsReported,
                Diagnosis = result.Diagnosis,
                TreatmentProvided = result.TreatmentProvided,
                FollowUpNeeded = result.FollowUpNeeded,
                FollowUpDate = result.FollowUpDate,
                LinkedHealthRecordId = result.LinkedHealthRecordId,
                ReminderEnabled = result.ReminderEnabled,
                Cost = result.Cost,
                Notes = result.Notes,
                Description = result.Description,
                Files = result.Files,
                Tags = result.Tags,
                ExtraFields = result.ExtraFields
            };
            return PartialView("VetVisit/_VetVisitRecordModal", convertedResult);
        }

        [HttpPost]
        public IActionResult SaveVetVisitRecordToVehicleId(VetVisitRecordInput vetVisitRecord)
        {
            if (!_userLogic.UserCanEditVehicle(GetUserID(), vetVisitRecord.VehicleId, HouseholdPermission.Edit))
            {
                return Json(OperationResponse.Failed("Access Denied"));
            }
            vetVisitRecord.Files = vetVisitRecord.Files
                .Select(x => new UploadedFiles
                {
                    Name = x.Name,
                    Location = _fileHelper.MoveFileFromTemp(x.Location, "documents/")
                }).ToList();

            var convertedRecord = vetVisitRecord.ToVetVisitRecord();
            var result = _vetVisitRecordDataAccess.SaveVetVisitRecord(convertedRecord);
            if (result)
            {
                // Phase 4 – Create or update the linked HealthRecord timeline entry.
                int priorLinkedHealthId = vetVisitRecord.LinkedHealthRecordId;
                var noteParts = new[]
                {
                    string.IsNullOrWhiteSpace(vetVisitRecord.SymptomsReported) ? null : $"Symptoms: {vetVisitRecord.SymptomsReported}",
                    string.IsNullOrWhiteSpace(vetVisitRecord.Diagnosis) ? null : $"Diagnosis: {vetVisitRecord.Diagnosis}",
                    string.IsNullOrWhiteSpace(vetVisitRecord.TreatmentProvided) ? null : $"Treatment: {vetVisitRecord.TreatmentProvided}",
                    vetVisitRecord.Notes
                }.Where(s => !string.IsNullOrWhiteSpace(s));
                var visitTitle = !string.IsNullOrWhiteSpace(vetVisitRecord.ReasonForVisit)
                    ? vetVisitRecord.ReasonForVisit
                    : "Vet Visit";
                var provider = !string.IsNullOrWhiteSpace(vetVisitRecord.Clinic)
                    ? vetVisitRecord.Clinic
                    : vetVisitRecord.Veterinarian;
                var projectedHealthRecord = new HealthRecord
                {
                    VehicleId = convertedRecord.VehicleId,
                    Date = convertedRecord.Date,
                    Category = HealthRecordCategory.VetVisit,
                    Title = visitTitle,
                    Provider = provider,
                    Notes = string.Join("\n", noteParts),
                    Cost = vetVisitRecord.Cost,
                    Status = HealthRecordStatus.Completed,
                    FollowUpRequired = vetVisitRecord.FollowUpNeeded,
                    FollowUpDate = vetVisitRecord.FollowUpDate
                };
                int linkedHealthId = SyncLinkedHealthRecord(
                    projectedHealthRecord, priorLinkedHealthId, "VetVisit", convertedRecord.Id);
                if (linkedHealthId > 0 && linkedHealthId != priorLinkedHealthId)
                {
                    convertedRecord.LinkedHealthRecordId = linkedHealthId;
                    _vetVisitRecordDataAccess.SaveVetVisitRecord(convertedRecord);
                }

                // Phase 5.1 – Sync a follow-up reminder when ReminderEnabled is toggled and a date is set.
                // Uses ReminderLinkedRecordType.VetVisit so this reminder slot is independent from any
                // reminders auto-linked to the projected HealthRecord timeline entry.
                // When ReminderEnabled is false or FollowUpDate is cleared, the linked reminder is deleted.
                SyncReminderFromLinkedRecord(
                    petId: convertedRecord.VehicleId,
                    reminderEnabled: vetVisitRecord.ReminderEnabled,
                    dueDateString: vetVisitRecord.FollowUpDate,
                    description: !string.IsNullOrWhiteSpace(vetVisitRecord.ReasonForVisit)
                        ? $"Vet Visit Follow-up: {vetVisitRecord.ReasonForVisit}"
                        : "Vet Visit Follow-up",
                    petReminderType: PetReminderType.FollowUpReminder,
                    linkedRecordType: ReminderLinkedRecordType.VetVisit,
                    linkedRecordId: convertedRecord.Id);
            }
            return Json(OperationResponse.Conditional(result, string.Empty, StaticHelper.GenericErrorMessage));
        }

        [HttpPost]
        public IActionResult DeleteVetVisitRecordById(int vetVisitRecordId)
        {
            var existingRecord = _vetVisitRecordDataAccess.GetVetVisitRecordById(vetVisitRecordId);
            if (!_userLogic.UserCanEditVehicle(GetUserID(), existingRecord.VehicleId, HouseholdPermission.Delete))
            {
                return Json(OperationResponse.Failed("Access Denied"));
            }
            // Phase 5.1 – Clean up the VetVisit's own follow-up reminder before deleting the record
            // so it does not become an orphaned reminder on the pet's reminder list.
            DeleteAllLinkedReminders(existingRecord.VehicleId, ReminderLinkedRecordType.VetVisit, existingRecord.Id);
            // Phase 4.2 – Cascade-delete the auto-projected HealthRecord timeline entry.
            // Phase 5.1 – Also clean up any reminders linked to that projected HealthRecord.
            if (existingRecord.LinkedHealthRecordId > 0)
            {
                DeleteAllLinkedReminders(existingRecord.VehicleId, ReminderLinkedRecordType.HealthRecord, existingRecord.LinkedHealthRecordId);
                _healthRecordDataAccess.DeleteHealthRecordById(existingRecord.LinkedHealthRecordId);
            }
            var result = _vetVisitRecordDataAccess.DeleteVetVisitRecordById(existingRecord.Id);
            return Json(OperationResponse.Conditional(result, string.Empty, StaticHelper.GenericErrorMessage));
        }
    }
}
