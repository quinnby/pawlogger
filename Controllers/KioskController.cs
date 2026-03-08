using CarCareTracker.External.Interfaces;
using CarCareTracker.Filter;
using CarCareTracker.Helper;
using CarCareTracker.Logic;
using CarCareTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarCareTracker.Controllers
{
    [Authorize]
    public class KioskController : Controller
    {
        private readonly ILogger<KioskController> _logger;
        private readonly IVehicleDataAccess _dataAccess;
        private readonly IPetProfileLogic _petProfileLogic;
        private readonly IProfileAccessLogic _profileAccessLogic;
        private readonly IConfigHelper _config;
        public KioskController(ILogger<KioskController> logger, 
            IVehicleDataAccess dataAccess, 
            IPetProfileLogic petProfileLogic, 
            IProfileAccessLogic profileAccessLogic, 
            IConfigHelper config)
        {
            _logger = logger;
            _dataAccess = dataAccess;
            _petProfileLogic = petProfileLogic;
            _profileAccessLogic = profileAccessLogic;
            _config = config;
        }
        private int GetUserID()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty);
        }
        public IActionResult Index(string exclusions, KioskMode kioskMode = KioskMode.Vehicle)
        {
            try
            {
                var viewModel = new KioskViewModel
                {
                    Exclusions = string.IsNullOrWhiteSpace(exclusions) ? new List<int>() : exclusions.Split(',').Select(x => int.Parse(x)).ToList(),
                    KioskMode = kioskMode
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return View(new KioskViewModel());
            }
        }
        [HttpPost]
        public IActionResult KioskContent(KioskViewModel kioskParameters)
        {
            var vehiclesStored = _dataAccess.GetVehicles();
            if (!User.IsInRole(nameof(UserData.IsRootUser)))
            {
                vehiclesStored = _profileAccessLogic.FilterUserPetProfiles(vehiclesStored, GetUserID());
            }
            vehiclesStored.RemoveAll(x => kioskParameters.Exclusions.Contains(x.Id));
            var userConfig = _config.GetUserConfig(User);
            if (userConfig.HideSoldVehicles)
            {
                vehiclesStored.RemoveAll(x => !string.IsNullOrWhiteSpace(x.SoldDate));
            }
            switch (kioskParameters.KioskMode)
            {
                case KioskMode.Vehicle:
                    {
                        var kioskResult = _petProfileLogic.GetPetProfileInfo(vehiclesStored);
                        return PartialView("_Kiosk", kioskResult);
                    }
                case KioskMode.Plan:
                    {
                        var kioskResult = _petProfileLogic.GetProfilePlansForKiosk(vehiclesStored, false);
                        return PartialView("_KioskPlan", kioskResult);
                    }
                case KioskMode.Reminder:
                    {
                        var kioskResult = _petProfileLogic.GetProfileRemindersForKiosk(vehiclesStored);
                        return PartialView("_KioskReminder", kioskResult);
                    }
            }
            var result = _petProfileLogic.GetPetProfileInfo(vehiclesStored);
            return PartialView("_Kiosk", result);
        }
        [TypeFilter(typeof(CollaboratorFilter))]
        public IActionResult GetKioskVehicleInfo(int vehicleId)
        {
            var result = _petProfileLogic.GetKioskPetProfileInfo(vehicleId);
            return PartialView("_KioskVehicleInfo", result);
        }
    }
}
