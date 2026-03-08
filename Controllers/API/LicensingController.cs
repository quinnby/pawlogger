using CarCareTracker.Filter;
using CarCareTracker.Helper;
using CarCareTracker.Models;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers
{
    public partial class APIController
    {
        [HttpGet]
        [Route("/api/vehicle/licensingrecords/all")]
        public IActionResult AllLicensingRecords(MethodParameter parameters)
        {
            MarkContractUsage("/api/vehicle/licensingrecords/all");
            List<int> vehicleIds = new List<int>();
            var vehicles = _dataAccess.GetVehicles();
            if (!User.IsInRole(nameof(UserData.IsRootUser)))
            {
                vehicles = _userLogic.FilterUserVehicles(vehicles, GetUserID());
            }

            vehicleIds.AddRange(vehicles.Select(x => x.Id));
            List<LicensingRecord> vehicleRecords = new List<LicensingRecord>();
            foreach (int vehicleId in vehicleIds)
            {
                vehicleRecords.AddRange(_licensingRecordDataAccess.GetLicensingRecordsByVehicleId(vehicleId));
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
        [Route("/api/v2/profiles/licensingrecords/all")]
        public IActionResult AllLicensingRecordsV2(MethodParameter parameters) => AllLicensingRecords(parameters);

        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        [Route("/api/vehicle/licensingrecords")]
        public IActionResult LicensingRecords(int vehicleId = default, MethodParameter? parameters = null, int petProfileId = default)
        {
            MarkContractUsage("/api/vehicle/licensingrecords");
            parameters ??= new MethodParameter();
            var resolvedVehicleId = ResolveVehicleIdAlias(vehicleId, petProfileId, "/api/vehicle/licensingrecords");
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

            var vehicleRecords = _licensingRecordDataAccess.GetLicensingRecordsByVehicleId(resolvedVehicleId);
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
        [Route("/api/v2/profiles/licensingrecords")]
        public IActionResult LicensingRecordsV2(int petProfileId = default, MethodParameter? parameters = null, int vehicleId = default)
            => LicensingRecords(vehicleId, parameters, petProfileId);
    }
}
