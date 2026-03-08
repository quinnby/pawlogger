using CarCareTracker.Filter;
using CarCareTracker.Helper;
using CarCareTracker.Models;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers
{
    public partial class APIController
    {
        [HttpGet]
        [Route("/api/vehicle/medicationrecords/all")]
        public IActionResult AllMedicationRecords(MethodParameter parameters)
        {
            MarkContractUsage("/api/vehicle/medicationrecords/all");
            List<int> vehicleIds = new List<int>();
            var vehicles = _dataAccess.GetVehicles();
            if (!User.IsInRole(nameof(UserData.IsRootUser)))
            {
                vehicles = _userLogic.FilterUserVehicles(vehicles, GetUserID());
            }

            vehicleIds.AddRange(vehicles.Select(x => x.Id));
            List<MedicationRecord> vehicleRecords = new List<MedicationRecord>();
            foreach (int vehicleId in vehicleIds)
            {
                vehicleRecords.AddRange(_medicationRecordDataAccess.GetMedicationRecordsByVehicleId(vehicleId));
            }

            if (parameters.Id != default)
            {
                vehicleRecords.RemoveAll(x => x.Id != parameters.Id);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Tags))
            {
                var tagsFilter = parameters.Tags.Split(' ').Distinct();
                vehicleRecords.RemoveAll(x => !x.Tags.Any(y => tagsFilter.Contains(y)));
            }

            if (_config.GetInvariantApi() || Request.Headers.ContainsKey("culture-invariant"))
            {
                return Json(vehicleRecords, StaticHelper.GetInvariantOption());
            }
            else
            {
                return Json(vehicleRecords);
            }
        }

        [HttpGet]
        [Route("/api/v2/profiles/medicationrecords/all")]
        public IActionResult AllMedicationRecordsV2(MethodParameter parameters) => AllMedicationRecords(parameters);

        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        [Route("/api/vehicle/medicationrecords")]
        public IActionResult MedicationRecords(int vehicleId = default, MethodParameter? parameters = null, int petProfileId = default)
        {
            MarkContractUsage("/api/vehicle/medicationrecords");
            parameters ??= new MethodParameter();
            var resolvedVehicleId = ResolveVehicleIdAlias(vehicleId, petProfileId, "/api/vehicle/medicationrecords");
            if (resolvedVehicleId == -1)
            {
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Input object invalid, vehicleId and petProfileId do not match."));
            }

            if (resolvedVehicleId == default)
            {
                var response = OperationResponse.Failed("Must provide a valid vehicle id");
                Response.StatusCode = 400;
                return Json(response);
            }

            var vehicleRecords = _medicationRecordDataAccess.GetMedicationRecordsByVehicleId(resolvedVehicleId);
            if (parameters.Id != default)
            {
                vehicleRecords.RemoveAll(x => x.Id != parameters.Id);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Tags))
            {
                var tagsFilter = parameters.Tags.Split(' ').Distinct();
                vehicleRecords.RemoveAll(x => !x.Tags.Any(y => tagsFilter.Contains(y)));
            }

            if (_config.GetInvariantApi() || Request.Headers.ContainsKey("culture-invariant"))
            {
                return Json(vehicleRecords, StaticHelper.GetInvariantOption());
            }
            else
            {
                return Json(vehicleRecords);
            }
        }

        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        [Route("/api/v2/profiles/medicationrecords")]
        public IActionResult MedicationRecordsV2(int petProfileId = default, MethodParameter? parameters = null, int vehicleId = default)
            => MedicationRecords(vehicleId, parameters, petProfileId);
    }
}
