using CarCareTracker.Filter;
using CarCareTracker.Helper;
using CarCareTracker.Models;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers
{
    public partial class VehicleController
    {
        private List<ReminderRecordViewModel> GetRemindersAndUrgency(int vehicleId, DateTime dateCompare)
        {
            var currentMileage = _vehicleLogic.GetMaxMileage(vehicleId);
            var reminders = _reminderRecordDataAccess.GetReminderRecordsByVehicleId(vehicleId);
            List<ReminderRecordViewModel> results = _reminderHelper.GetReminderRecordViewModels(reminders, currentMileage, dateCompare);
            return results;
        }
        private bool GetAndUpdateVehicleUrgentOrPastDueReminders(int vehicleId)
        {
            var result = GetRemindersAndUrgency(vehicleId, DateTime.Now);
            //check if user wants auto-refresh past-due reminders
            if (_config.GetUserConfig(User).EnableAutoReminderRefresh && _userLogic.UserCanEditVehicle(GetUserID(), vehicleId, HouseholdPermission.Edit))
            {
                //check for past due reminders that are eligible for recurring.
                var pastDueAndRecurring = result.Where(x => x.Urgency == ReminderUrgency.PastDue && x.IsRecurring);
                if (pastDueAndRecurring.Any())
                {
                    foreach (ReminderRecordViewModel reminderRecord in pastDueAndRecurring)
                    {
                        //update based on recurring intervals.
                        //pull reminderRecord based on ID
                        var existingReminder = _reminderRecordDataAccess.GetReminderRecordById(reminderRecord.Id);
                        existingReminder = _reminderHelper.GetUpdatedRecurringReminderRecord(existingReminder, null, null);
                        //save to db.
                        _reminderRecordDataAccess.SaveReminderRecordToVehicle(existingReminder);
                        //set urgency to not urgent so it gets excluded in count.
                        reminderRecord.Urgency = ReminderUrgency.NotUrgent;
                    }
                }
            }
            //check for very urgent or past due reminders that were not eligible for recurring.
            var pastDueAndUrgentReminders = result.Where(x => x.Urgency == ReminderUrgency.VeryUrgent || x.Urgency == ReminderUrgency.PastDue);
            if (pastDueAndUrgentReminders.Any())
            {
                return true;
            }
            return false;
        }
        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        public IActionResult GetVehicleHaveUrgentOrPastDueReminders(int vehicleId)
        {
            var result = GetAndUpdateVehicleUrgentOrPastDueReminders(vehicleId);
            return Json(result);
        }
        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        public IActionResult GetReminderRecordsByVehicleId(int vehicleId)
        {
            var result = GetRemindersAndUrgency(vehicleId, DateTime.Now);
            result = result.OrderByDescending(x => x.Urgency).ToList();
            return PartialView("Reminder/_ReminderRecords", result);
        }
        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        public IActionResult GetRecurringReminderRecordsByVehicleId(int vehicleId)
        {
            var result = GetRemindersAndUrgency(vehicleId, DateTime.Now);
            result.RemoveAll(x => !x.IsRecurring);
            result = result.OrderByDescending(x => x.Urgency).ThenBy(x => x.Description).ToList();
            return PartialView("_RecurringReminderSelector", result);
        }
        [HttpPost]
        public IActionResult PushbackRecurringReminderRecord(int reminderRecordId)
        {
            var result = PushbackRecurringReminderRecordWithChecks(reminderRecordId, null, null);
            return Json(result);
        }
        private OperationResponse PushbackRecurringReminderRecordWithChecks(int reminderRecordId, DateTime? currentDate, int? currentMileage)
        {
            try
            {
                var existingReminder = _reminderRecordDataAccess.GetReminderRecordById(reminderRecordId);
                if (existingReminder is not null && existingReminder.Id != default && existingReminder.IsRecurring)
                {
                    //security check
                    if (!_userLogic.UserCanEditVehicle(GetUserID(), existingReminder.VehicleId, HouseholdPermission.Edit))
                    {
                        return OperationResponse.Failed("Access Denied");
                    }
                    existingReminder = _reminderHelper.GetUpdatedRecurringReminderRecord(existingReminder, currentDate, currentMileage);
                    //save to db.
                    var reminderUpdateResult = _reminderRecordDataAccess.SaveReminderRecordToVehicle(existingReminder);
                    if (!reminderUpdateResult)
                    {
                        _logger.LogError("Unable to update reminder either because the reminder no longer exists or is no longer recurring");
                        return OperationResponse.Failed("Unable to update reminder either because the reminder no longer exists or is no longer recurring");
                    }
                    return OperationResponse.Succeed();
                }
                else
                {
                    _logger.LogError("Unable to update reminder because it no longer exists.");
                    return OperationResponse.Failed("Unable to update reminder because it no longer exists.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return OperationResponse.Failed(StaticHelper.GenericErrorMessage);
            }
        }
        [HttpPost]
        public IActionResult SaveReminderRecordToVehicleId(ReminderRecordInput reminderRecord)
        {
            //security check.
            if (!_userLogic.UserCanEditVehicle(GetUserID(), reminderRecord.VehicleId, HouseholdPermission.Edit))
            {
                return Json(OperationResponse.Failed("Access Denied"));
            }
            var result = _reminderRecordDataAccess.SaveReminderRecordToVehicle(reminderRecord.ToReminderRecord());
            if (result)
            {
                _eventLogic.PublishEvent(WebHookPayload.FromReminderRecord(reminderRecord.ToReminderRecord(), reminderRecord.Id == default ? "reminderrecord.add" : "reminderrecord.update", User.Identity?.Name ?? string.Empty));
            }
            return Json(OperationResponse.Conditional(result, string.Empty, StaticHelper.GenericErrorMessage));
        }
        [HttpPost]
        public IActionResult GetAddReminderRecordPartialView(ReminderRecordInput? reminderModel)
        {
            if (reminderModel is not null)
            {
                return PartialView("Reminder/_ReminderRecordModal", reminderModel);
            }
            else
            {
                return PartialView("Reminder/_ReminderRecordModal", new ReminderRecordInput());
            }
        }
        [HttpGet]
        public IActionResult GetReminderRecordForEditById(int reminderRecordId)
        {
            var result = _reminderRecordDataAccess.GetReminderRecordById(reminderRecordId);
            //security check.
            if (!_userLogic.UserCanEditVehicle(GetUserID(), result.VehicleId, HouseholdPermission.View))
            {
                return Redirect("/Error/Unauthorized");
            }
            //convert to Input object.
            var convertedResult = new ReminderRecordInput
            {
                Id = result.Id,
                Date = result.Date.ToShortDateString(),
                Description = result.Description,
                Notes = result.Notes,
                VehicleId = result.VehicleId,
                Mileage = result.Mileage,
                Metric = result.Metric,
                IsRecurring = result.IsRecurring,
                FixedIntervals = result.FixedIntervals,
                UseCustomThresholds = result.UseCustomThresholds,
                CustomThresholds = result.CustomThresholds,
                ReminderMileageInterval = result.ReminderMileageInterval,
                ReminderMonthInterval = result.ReminderMonthInterval,
                CustomMileageInterval = result.CustomMileageInterval,
                CustomMonthInterval = result.CustomMonthInterval,
                CustomMonthIntervalUnit = result.CustomMonthIntervalUnit,
                Tags = result.Tags,
                // Phase 5 – pet care reminder fields
                PetReminderType = result.PetReminderType,
                LinkedRecordType = result.LinkedRecordType,
                LinkedRecordId = result.LinkedRecordId
            };
            return PartialView("Reminder/_ReminderRecordModal", convertedResult);
        }
        private OperationResponse DeleteReminderRecordWithChecks(int reminderRecordId)
        {
            var existingRecord = _reminderRecordDataAccess.GetReminderRecordById(reminderRecordId);
            //security check.
            if (!_userLogic.UserCanEditVehicle(GetUserID(), existingRecord.VehicleId, HouseholdPermission.Delete))
            {
                return OperationResponse.Failed("Access Denied");
            }
            var result = _reminderRecordDataAccess.DeleteReminderRecordById(existingRecord.Id);
            if (result)
            {
                _eventLogic.PublishEvent(WebHookPayload.FromReminderRecord(existingRecord, "reminderrecord.delete", User.Identity?.Name ?? string.Empty));
            }
            return OperationResponse.Conditional(result, string.Empty, StaticHelper.GenericErrorMessage);
        }
        [HttpPost]
        public IActionResult DeleteReminderRecordById(int reminderRecordId)
        {
            var result = DeleteReminderRecordWithChecks(reminderRecordId);
            return Json(result);
        }

        // Phase 5 – Sync a reminder from a linked pet-health record (vaccination, medication, or licensing).
        // Called after a save when the source record has ReminderEnabled / RenewalReminderEnabled = true.
        // If reminderEnabled is false, any previously linked reminder is deleted.
        //
        // Phase 5.1 discriminator: the search now also matches petReminderType so that a single source
        // record (e.g. a HealthRecord) can hold two distinct reminders (PreventiveCare + FollowUp)
        // without one overwriting the other.  If a user manually changes the PetReminderType on an
        // auto-generated reminder it will no longer match the sync lookup; the next save of the source
        // record will create a fresh auto-generated reminder and the manually-edited one becomes
        // standalone.  This is the intentional, documented behaviour for this phase.
        private void SyncReminderFromLinkedRecord(
            int petId,
            bool reminderEnabled,
            string dueDateString,
            string description,
            PetReminderType petReminderType,
            ReminderLinkedRecordType linkedRecordType,
            int linkedRecordId)
        {
            // Find any existing reminder linked to this source record AND of the same type
            var existing = _reminderRecordDataAccess
                .GetReminderRecordsByVehicleId(petId)
                .FirstOrDefault(r =>
                    r.LinkedRecordType == linkedRecordType &&
                    r.LinkedRecordId == linkedRecordId &&
                    r.LinkedRecordId != 0 &&
                    r.PetReminderType == petReminderType);

            if (!reminderEnabled)
            {
                // Remove stale linked reminder if present
                if (existing != null && existing.Id != default)
                    _reminderRecordDataAccess.DeleteReminderRecordById(existing.Id);
                return;
            }

            // Parse the due date; bail out silently if none provided
            if (!DateTime.TryParse(dueDateString, out DateTime dueDate))
                return;

            if (existing != null && existing.Id != default)
            {
                // Update in place so the user doesn’t lose any custom settings they applied
                existing.Date = dueDate;
                existing.Description = description;
                existing.PetReminderType = petReminderType;
                _reminderRecordDataAccess.SaveReminderRecordToVehicle(existing);
            }
            else
            {
                var newReminder = new ReminderRecord
                {
                    VehicleId = petId,
                    Date = dueDate,
                    Description = description,
                    Metric = ReminderMetric.Date,   // pet reminders are always date-based by default
                    IsRecurring = false,
                    PetReminderType = petReminderType,
                    LinkedRecordType = linkedRecordType,
                    LinkedRecordId = linkedRecordId
                };
                _reminderRecordDataAccess.SaveReminderRecordToVehicle(newReminder);
            }
        }

        // Phase 5.1 – Delete every reminder that was auto-linked to the given source record.
        // Used before deleting a source record to prevent orphaned reminders.
        // Matching is intentionally broad (no PetReminderType filter) so that ALL auto-generated
        // reminders for this record are removed, even if the user manually changed their type.
        private void DeleteAllLinkedReminders(int petId, ReminderLinkedRecordType linkedRecordType, int linkedRecordId)
        {
            if (linkedRecordId <= 0) return;
            var linked = _reminderRecordDataAccess
                .GetReminderRecordsByVehicleId(petId)
                .Where(r =>
                    r.LinkedRecordType == linkedRecordType &&
                    r.LinkedRecordId == linkedRecordId &&
                    r.LinkedRecordId != 0)
                .ToList();
            foreach (var r in linked)
                _reminderRecordDataAccess.DeleteReminderRecordById(r.Id);
        }
    }
}
