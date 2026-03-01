using CarCareTracker.Filter;
using CarCareTracker.Helper;
using CarCareTracker.Models;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers
{
    /// <summary>
    /// Phase 6 – Centralized pet expense tracking CRUD controller.
    /// Follows the VetVisitController / MedicationController partial-class pattern.
    /// LinkedHealthRecordId is stored as a loose reference only; no cascade projection.
    /// </summary>
    public partial class VehicleController
    {
        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        public IActionResult GetPetExpenseRecordsByVehicleId(int vehicleId)
        {
            var result = _petExpenseRecordDataAccess.GetPetExpenseRecordsByVehicleId(vehicleId);
            bool _useDescending = _config.GetUserConfig(User).UseDescending;
            if (_useDescending)
            {
                result = result.OrderByDescending(x => x.Date).ToList();
            }
            else
            {
                result = result.OrderBy(x => x.Date).ToList();
            }
            return PartialView("Expense/_PetExpenseRecords", result);
        }

        [HttpGet]
        public IActionResult GetAddPetExpenseRecordPartialView()
        {
            return PartialView("Expense/_PetExpenseRecordModal", new PetExpenseRecordInput());
        }

        [HttpGet]
        public IActionResult GetPetExpenseRecordForEditById(int petExpenseRecordId)
        {
            var result = _petExpenseRecordDataAccess.GetPetExpenseRecordById(petExpenseRecordId);
            if (!_userLogic.UserCanEditVehicle(GetUserID(), result.VehicleId, HouseholdPermission.View))
            {
                return Redirect("/Error/Unauthorized");
            }
            var convertedResult = new PetExpenseRecordInput
            {
                Id = result.Id,
                VehicleId = result.VehicleId,
                Date = result.Date.ToShortDateString(),
                Category = result.Category,
                Vendor = result.Vendor,
                Description = result.Description,
                Cost = result.Cost,
                IsRecurring = result.IsRecurring,
                LinkedHealthRecordId = result.LinkedHealthRecordId,
                Notes = result.Notes,
                Files = result.Files,
                Tags = result.Tags,
                ExtraFields = result.ExtraFields
            };
            return PartialView("Expense/_PetExpenseRecordModal", convertedResult);
        }

        [HttpPost]
        public IActionResult SavePetExpenseRecordToVehicleId(PetExpenseRecordInput petExpenseRecord)
        {
            if (!_userLogic.UserCanEditVehicle(GetUserID(), petExpenseRecord.VehicleId, HouseholdPermission.Edit))
            {
                return Json(OperationResponse.Failed("Access Denied"));
            }
            petExpenseRecord.Files = petExpenseRecord.Files
                .Select(x => new UploadedFiles
                {
                    Name = x.Name,
                    Location = _fileHelper.MoveFileFromTemp(x.Location, "documents/")
                }).ToList();

            var convertedRecord = petExpenseRecord.ToPetExpenseRecord();
            var result = _petExpenseRecordDataAccess.SavePetExpenseRecord(convertedRecord);
            return Json(OperationResponse.Conditional(result, string.Empty, StaticHelper.GenericErrorMessage));
        }

        [HttpPost]
        public IActionResult DeletePetExpenseRecordById(int petExpenseRecordId)
        {
            var existingRecord = _petExpenseRecordDataAccess.GetPetExpenseRecordById(petExpenseRecordId);
            if (!_userLogic.UserCanEditVehicle(GetUserID(), existingRecord.VehicleId, HouseholdPermission.Delete))
            {
                return Json(OperationResponse.Failed("Access Denied"));
            }
            // Phase 6 – Loose-link policy: deleting an expense does NOT cascade-delete the
            // linked HealthRecord. The HealthRecord timeline is the source of truth and should
            // survive independently. The expense record is the secondary record here.
            var result = _petExpenseRecordDataAccess.DeletePetExpenseRecordById(existingRecord.Id);
            return Json(OperationResponse.Conditional(result, string.Empty, StaticHelper.GenericErrorMessage));
        }

        /// <summary>
        /// Phase 7 – Returns a lightweight list of HealthRecord entries for the given pet,
        /// suitable for populating the "Link to Health Record" selector in the expense modal.
        /// Returns only id, date, title and category so the payload stays small.
        /// Ordered most-recent first so the most relevant entries appear at the top.
        /// </summary>
        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        public IActionResult GetHealthRecordListForExpenseSelector(int vehicleId)
        {
            var records = _healthRecordDataAccess
                .GetHealthRecordsByVehicleId(vehicleId)
                .OrderByDescending(x => x.Date)
                .Select(x => new
                {
                    id       = x.Id,
                    date     = x.Date.ToShortDateString(),
                    title    = x.Title,
                    category = x.Category switch
                    {
                        HealthRecordCategory.VetVisit         => "Vet Visit",
                        HealthRecordCategory.Vaccination      => "Vaccination",
                        HealthRecordCategory.Medication       => "Medication",
                        HealthRecordCategory.IllnessSymptom   => "Illness/Symptom",
                        HealthRecordCategory.ProcedureSurgery => "Procedure/Surgery",
                        HealthRecordCategory.Dental           => "Dental",
                        HealthRecordCategory.Grooming         => "Grooming",
                        HealthRecordCategory.WeightCheck      => "Weight Check",
                        HealthRecordCategory.AllergyReaction  => "Allergy/Reaction",
                        HealthRecordCategory.LabResult        => "Lab Result",
                        HealthRecordCategory.Licensing        => "Licensing",
                        HealthRecordCategory.PreventiveCare   => "Preventive Care",
                        HealthRecordCategory.BehavioralNote   => "Behavioral Note",
                        _                                     => "Misc. Care"
                    }
                })
                .ToList();
            return Json(records);
        }
    }
}
