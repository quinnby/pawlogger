using CarCareTracker.Filter;
using CarCareTracker.Helper;
using CarCareTracker.Models;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers
{
    public partial class APIController
    {
        [HttpGet]
        [Route("/api/vehicle/healthrecords/all")]
        public IActionResult AllHealthRecords(MethodParameter parameters)
        {
            MarkContractUsage("/api/vehicle/healthrecords/all");
            List<int> vehicleIds = new List<int>();
            var vehicles = _dataAccess.GetVehicles();
            if (!User.IsInRole(nameof(UserData.IsRootUser)))
            {
                vehicles = _userLogic.FilterUserVehicles(vehicles, GetUserID());
            }

            vehicleIds.AddRange(vehicles.Select(x => x.Id));
            List<HealthRecord> vehicleRecords = new List<HealthRecord>();
            foreach (int vehicleId in vehicleIds)
            {
                vehicleRecords.AddRange(_healthRecordDataAccess.GetHealthRecordsByVehicleId(vehicleId));
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
        [Route("/api/v2/profiles/healthrecords/all")]
        public IActionResult AllHealthRecordsV2(MethodParameter parameters) => AllHealthRecords(parameters);

        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        [Route("/api/vehicle/healthrecords")]
        public IActionResult HealthRecords(int vehicleId = default, MethodParameter? parameters = null, int petProfileId = default)
        {
            MarkContractUsage("/api/vehicle/healthrecords");
            parameters ??= new MethodParameter();
            var resolvedVehicleId = ResolveVehicleIdAlias(vehicleId, petProfileId, "/api/vehicle/healthrecords");
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

            var vehicleRecords = _healthRecordDataAccess.GetHealthRecordsByVehicleId(resolvedVehicleId);
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
        [Route("/api/v2/profiles/healthrecords")]
        public IActionResult HealthRecordsV2(int petProfileId = default, MethodParameter? parameters = null, int vehicleId = default)
            => HealthRecords(vehicleId, parameters, petProfileId);
    }
}
