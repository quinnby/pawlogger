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
        public IActionResult GetHealthRecordsByVehicleId(int vehicleId)
        {
            var result = _healthRecordDataAccess.GetHealthRecordsByVehicleId(vehicleId);
            bool _useDescending = _config.GetUserConfig(User).UseDescending;
            if (_useDescending)
            {
                result = result.OrderByDescending(x => x.Date).ToList();
            }
            else
            {
                result = result.OrderBy(x => x.Date).ToList();
            }
            return PartialView("Health/_HealthRecords", result);
        }

        [HttpGet]
        public IActionResult GetAddHealthRecordPartialView()
        {
            return PartialView("Health/_HealthRecordModal", new HealthRecordInput());
        }

        [HttpGet]
        public IActionResult GetHealthRecordForEditById(int healthRecordId)
        {
            var result = _healthRecordDataAccess.GetHealthRecordById(healthRecordId);
            if (!_userLogic.UserCanEditVehicle(GetUserID(), result.VehicleId, HouseholdPermission.View))
            {
                return Redirect("/Error/Unauthorized");
            }
            var convertedResult = new HealthRecordInput
            {
                Id = result.Id,
                VehicleId = result.VehicleId,
                Date = result.Date.ToShortDateString(),
                Category = result.Category,
                Title = result.Title,
                Description = result.Description,
                Cost = result.Cost,
                Notes = result.Notes,
                Provider = result.Provider,
                FollowUpRequired = result.FollowUpRequired,
                FollowUpDate = result.FollowUpDate,
                Status = result.Status,
                Files = result.Files,
                Tags = result.Tags,
                ExtraFields = result.ExtraFields
            };
            return PartialView("Health/_HealthRecordModal", convertedResult);
        }

        [HttpPost]
        public IActionResult SaveHealthRecordToVehicleId(HealthRecordInput healthRecord)
        {
            if (!_userLogic.UserCanEditVehicle(GetUserID(), healthRecord.VehicleId, HouseholdPermission.Edit))
            {
                return Json(OperationResponse.Failed("Access Denied"));
            }
            // Move any newly uploaded files out of temp storage
            healthRecord.Files = healthRecord.Files
                .Select(x => new UploadedFiles
                {
                    Name = x.Name,
                    Location = _fileHelper.MoveFileFromTemp(x.Location, "documents/")
                }).ToList();

            var convertedRecord = healthRecord.ToHealthRecord();
            var result = _healthRecordDataAccess.SaveHealthRecord(convertedRecord);
            return Json(OperationResponse.Conditional(result, string.Empty, StaticHelper.GenericErrorMessage));
        }

        [HttpPost]
        public IActionResult DeleteHealthRecordById(int healthRecordId)
        {
            var existingRecord = _healthRecordDataAccess.GetHealthRecordById(healthRecordId);
            if (!_userLogic.UserCanEditVehicle(GetUserID(), existingRecord.VehicleId, HouseholdPermission.Delete))
            {
                return Json(OperationResponse.Failed("Access Denied"));
            }
            var result = _healthRecordDataAccess.DeleteHealthRecordById(existingRecord.Id);
            return Json(OperationResponse.Conditional(result, string.Empty, StaticHelper.GenericErrorMessage));
        }
    }
}
