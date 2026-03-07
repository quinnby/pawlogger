using CarCareTracker.Filter;
using CarCareTracker.Helper;
using CarCareTracker.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;

namespace CarCareTracker.Controllers
{
    public partial class VehicleController
    {
        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        public IActionResult GetReportPartialView(int vehicleId)
        {
            //get records
            var vehicleData = _dataAccess.GetVehicleById(vehicleId);
            var vehicleRecords = _vehicleLogic.GetVehicleRecords(vehicleId);
            var serviceRecords = vehicleRecords.ServiceRecords;
            var gasRecords = vehicleRecords.GasRecords;
            var collisionRecords = vehicleRecords.CollisionRecords;
            var taxRecords = vehicleRecords.TaxRecords;
            var upgradeRecords = vehicleRecords.UpgradeRecords;
            var odometerRecords = vehicleRecords.OdometerRecords;
            var userConfig = _config.GetUserConfig(User);
            var viewModel = new ReportViewModel() { ReportHeaderForVehicle = new ReportHeader() };
            //check if vehicle map exists
            viewModel.HasVehicleImageMap = !string.IsNullOrWhiteSpace(vehicleData.MapLocation);
            //check if custom widgets are configured
            viewModel.CustomWidgetsConfigured = _fileHelper.WidgetsExist();
            //get totalCostMakeUp
            viewModel.CostMakeUpForVehicle = new CostMakeUpForVehicle
            {
                ServiceRecordSum = serviceRecords.Sum(x => x.Cost),
                GasRecordSum = gasRecords.Sum(x => x.Cost),
                CollisionRecordSum = collisionRecords.Sum(x => x.Cost),
                TaxRecordSum = taxRecords.Sum(x => x.Cost),
                UpgradeRecordSum = upgradeRecords.Sum(x => x.Cost)
            };
            //get costbymonth
            List<CostForVehicleByMonth> allCosts = StaticHelper.GetBaseLineCosts();
            allCosts.AddRange(_reportHelper.GetServiceRecordSum(serviceRecords, 0));
            allCosts.AddRange(_reportHelper.GetRepairRecordSum(collisionRecords, 0));
            allCosts.AddRange(_reportHelper.GetUpgradeRecordSum(upgradeRecords, 0));
            allCosts.AddRange(_reportHelper.GetGasRecordSum(gasRecords, 0));
            allCosts.AddRange(_reportHelper.GetTaxRecordSum(taxRecords, 0));
            allCosts.AddRange(_reportHelper.GetOdometerRecordSum(odometerRecords, 0));
            viewModel.CostForVehicleByMonth = allCosts.GroupBy(x => new { x.MonthName, x.MonthId }).OrderBy(x => x.Key.MonthId).Select(x => new CostForVehicleByMonth
            {
                MonthName = x.Key.MonthName,
                Cost = x.Sum(y => y.Cost),
                DistanceTraveled = x.Max(y => y.DistanceTraveled)
            }).ToList();

            //set available metrics
            var visibleTabs = userConfig.VisibleTabs;
            if (visibleTabs.Contains(ImportMode.OdometerRecord) || odometerRecords.Any())
            {
                viewModel.AvailableMetrics.Add(ImportMode.OdometerRecord);
            }
            if (visibleTabs.Contains(ImportMode.ServiceRecord) || serviceRecords.Any())
            {
                viewModel.AvailableMetrics.Add(ImportMode.ServiceRecord);
            }
            if (visibleTabs.Contains(ImportMode.RepairRecord) || collisionRecords.Any())
            {
                viewModel.AvailableMetrics.Add(ImportMode.RepairRecord);
            }
            if (visibleTabs.Contains(ImportMode.UpgradeRecord) || upgradeRecords.Any())
            {
                viewModel.AvailableMetrics.Add(ImportMode.UpgradeRecord);
            }
            if (visibleTabs.Contains(ImportMode.GasRecord) || gasRecords.Any())
            {
                viewModel.AvailableMetrics.Add(ImportMode.GasRecord);
            }
            if (visibleTabs.Contains(ImportMode.TaxRecord) || taxRecords.Any())
            {
                viewModel.AvailableMetrics.Add(ImportMode.TaxRecord);
            }

            //get reminders
            var reminders = GetRemindersAndUrgency(vehicleId, DateTime.Now);
            viewModel.ReminderMakeUpForVehicle = new ReminderMakeUpForVehicle
            {
                NotUrgentCount = reminders.Where(x => x.Urgency == ReminderUrgency.NotUrgent).Count(),
                UrgentCount = reminders.Where(x => x.Urgency == ReminderUrgency.Urgent).Count(),
                VeryUrgentCount = reminders.Where(x => x.Urgency == ReminderUrgency.VeryUrgent).Count(),
                PastDueCount = reminders.Where(x => x.Urgency == ReminderUrgency.PastDue).Count()
            };
            //populate year dropdown.
            var numbersArray = new List<int>();
            if (serviceRecords.Any())
            {
                numbersArray.Add(serviceRecords.Min(x => x.Date.Year));
            }
            if (collisionRecords.Any())
            {
                numbersArray.Add(collisionRecords.Min(x => x.Date.Year));
            }
            if (gasRecords.Any())
            {
                numbersArray.Add(gasRecords.Min(x => x.Date.Year));
            }
            if (upgradeRecords.Any())
            {
                numbersArray.Add(upgradeRecords.Min(x => x.Date.Year));
            }
            if (odometerRecords.Any())
            {
                numbersArray.Add(odometerRecords.Min(x => x.Date.Year));
            }
            var minYear = numbersArray.Any() ? numbersArray.Min() : DateTime.Now.AddYears(-5).Year;
            var yearDifference = DateTime.Now.Year - minYear + 1;
            for (int i = 0; i < yearDifference; i++)
            {
                viewModel.Years.Add(DateTime.Now.AddYears(i * -1).Year);
            }
            //get collaborators
            var collaborators = _userLogic.GetCollaboratorsForVehicle(vehicleId);
            var userCanModify = _userLogic.UserCanDirectlyEditVehicle(GetUserID(), vehicleId);
            viewModel.Collaborators = new VehicleCollaboratorViewModel { CanModifyCollaborators = userCanModify, Collaborators = collaborators};
            //get MPG per month.
            var mileageData = _gasHelper.GetGasRecordViewModels(gasRecords, userConfig.UseMPG, !vehicleData.IsElectric && userConfig.UseUKMPG);
            string preferredFuelMileageUnit = _config.GetUserConfig(User).PreferredGasMileageUnit;
            var fuelEconomyMileageUnit = StaticHelper.GetFuelEconomyUnit(vehicleData.IsElectric, vehicleData.UseHours, userConfig.UseMPG, userConfig.UseUKMPG);
            var averageMPG = _gasHelper.GetAverageGasMileage(mileageData, userConfig.UseMPG);
            mileageData.RemoveAll(x => x.MilesPerGallon == default);
            bool invertedFuelMileageUnit = fuelEconomyMileageUnit == "l/100km" && preferredFuelMileageUnit == "km/l";
            var monthlyMileageData = StaticHelper.GetBaseLineCostsNoMonthName();
            monthlyMileageData.AddRange(mileageData.GroupBy(x => x.MonthId).Select(x => new CostForVehicleByMonth
            {
                MonthId = x.Key,
                Cost = x.Average(y => y.MilesPerGallon)
            }));
            monthlyMileageData = monthlyMileageData.GroupBy(x => x.MonthId).OrderBy(x => x.Key).Select(x => new CostForVehicleByMonth
            {
                MonthId = x.Key,
                MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(x.Key),
                Cost = x.Sum(y => y.Cost)
            }).ToList();
            if (invertedFuelMileageUnit)
            {
                foreach (CostForVehicleByMonth monthMileage in monthlyMileageData)
                {
                    if (monthMileage.Cost != default)
                    {
                        monthMileage.Cost = 100 / monthMileage.Cost;
                    }
                }
                var newAverageMPG = decimal.Parse(averageMPG, NumberStyles.Any);
                if (newAverageMPG != 0)
                {
                    newAverageMPG = 100 / newAverageMPG;
                }
                averageMPG = newAverageMPG.ToString("F");
            }
            var mpgViewModel = new MPGForVehicleByMonth {
                CostData = monthlyMileageData,
                Unit = invertedFuelMileageUnit ? preferredFuelMileageUnit : fuelEconomyMileageUnit,
                SortedCostData = (userConfig.UseMPG || invertedFuelMileageUnit) ? monthlyMileageData.OrderByDescending(x => x.Cost).ToList() : monthlyMileageData.OrderBy(x => x.Cost).ToList()
            };
            viewModel.FuelMileageForVehicleByMonth = mpgViewModel;
            //report header

            var maxMileage = _vehicleLogic.GetMaxMileage(vehicleRecords);

            viewModel.ReportHeaderForVehicle.TotalCost = _vehicleLogic.GetVehicleTotalCost(vehicleRecords);
            viewModel.ReportHeaderForVehicle.AverageMPG = $"{averageMPG} {mpgViewModel.Unit}";
            viewModel.ReportHeaderForVehicle.MaxOdometer = maxMileage;
            viewModel.ReportHeaderForVehicle.DistanceTraveled = odometerRecords.Sum(x => x.DistanceTraveled);
            return PartialView("Report/_Report", viewModel);
        }
        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        public IActionResult GetCollaboratorsForVehicle(int vehicleId)
        {
            var result = _userLogic.GetCollaboratorsForVehicle(vehicleId);
            var userCanModify = _userLogic.UserCanDirectlyEditVehicle(GetUserID(), vehicleId);
            var viewModel = new VehicleCollaboratorViewModel
            {
                Collaborators = result,
                CanModifyCollaborators = userCanModify
            };
            return PartialView("Report/_Collaborators", viewModel);
        }
        [TypeFilter(typeof(StrictCollaboratorFilter), Arguments = new object[] {false, true})]
        [HttpPost]
        public IActionResult AddCollaboratorsToVehicle(int vehicleId, string username)
        {
            var result = _userLogic.AddCollaboratorToVehicle(vehicleId, username);
            return Json(result);
        }
        [TypeFilter(typeof(StrictCollaboratorFilter), Arguments = new object[] { false, true })]
        [HttpPost]
        public IActionResult DeleteCollaboratorFromVehicle(int userId, int vehicleId)
        {
            var result = _userLogic.DeleteCollaboratorFromVehicle(userId, vehicleId);
            return Json(result);
        }
        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpPost]
        public IActionResult GetSummaryForVehicle(int vehicleId, int year = 0)
        {
            var vehicleData = _dataAccess.GetVehicleById(vehicleId);
            var vehicleRecords = _vehicleLogic.GetVehicleRecords(vehicleId);

            var serviceRecords = vehicleRecords.ServiceRecords;
            var gasRecords = vehicleRecords.GasRecords;
            var collisionRecords = vehicleRecords.CollisionRecords;
            var taxRecords = vehicleRecords.TaxRecords;
            var upgradeRecords = vehicleRecords.UpgradeRecords;
            var odometerRecords = vehicleRecords.OdometerRecords;

            if (year != default)
            {
                serviceRecords.RemoveAll(x => x.Date.Year != year);
                gasRecords.RemoveAll(x => x.Date.Year != year);
                collisionRecords.RemoveAll(x => x.Date.Year != year);
                taxRecords.RemoveAll(x => x.Date.Year != year);
                upgradeRecords.RemoveAll(x => x.Date.Year != year);
                odometerRecords.RemoveAll(x => x.Date.Year != year);
            }

            var userConfig = _config.GetUserConfig(User);

            var mileageData = _gasHelper.GetGasRecordViewModels(gasRecords, userConfig.UseMPG, !vehicleData.IsElectric && userConfig.UseUKMPG);
            string preferredFuelMileageUnit = _config.GetUserConfig(User).PreferredGasMileageUnit;
            var fuelEconomyMileageUnit = StaticHelper.GetFuelEconomyUnit(vehicleData.IsElectric, vehicleData.UseHours, userConfig.UseMPG, userConfig.UseUKMPG);
            var averageMPG = _gasHelper.GetAverageGasMileage(mileageData, userConfig.UseMPG);
            bool invertedFuelMileageUnit = fuelEconomyMileageUnit == "l/100km" && preferredFuelMileageUnit == "km/l";

            if (invertedFuelMileageUnit)
            {
                var newAverageMPG = decimal.Parse(averageMPG, NumberStyles.Any);
                if (newAverageMPG != 0)
                {
                    newAverageMPG = 100 / newAverageMPG;
                }
                averageMPG = newAverageMPG.ToString("F");
            }

            var mpgUnit = invertedFuelMileageUnit ? preferredFuelMileageUnit : fuelEconomyMileageUnit;

            var maxMileage = _vehicleLogic.GetMaxMileage(vehicleRecords);

            var viewModel = new ReportHeader()
            {
                TotalCost = _vehicleLogic.GetVehicleTotalCost(vehicleRecords),
                AverageMPG = $"{averageMPG} {mpgUnit}",
                MaxOdometer = maxMileage,
                DistanceTraveled = odometerRecords.Sum(x => x.DistanceTraveled)
            };

            return PartialView("Report/_ReportHeader", viewModel);
        }
        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        public IActionResult GetCostMakeUpForVehicle(int vehicleId, int year = 0)
        {
            var serviceRecords = _serviceRecordDataAccess.GetServiceRecordsByVehicleId(vehicleId);
            var gasRecords = _gasRecordDataAccess.GetGasRecordsByVehicleId(vehicleId);
            var collisionRecords = _collisionRecordDataAccess.GetCollisionRecordsByVehicleId(vehicleId);
            var taxRecords = _taxRecordDataAccess.GetTaxRecordsByVehicleId(vehicleId);
            var upgradeRecords = _upgradeRecordDataAccess.GetUpgradeRecordsByVehicleId(vehicleId);
            if (year != default)
            {
                serviceRecords.RemoveAll(x => x.Date.Year != year);
                gasRecords.RemoveAll(x => x.Date.Year != year);
                collisionRecords.RemoveAll(x => x.Date.Year != year);
                taxRecords.RemoveAll(x => x.Date.Year != year);
                upgradeRecords.RemoveAll(x => x.Date.Year != year);
            }
            var viewModel = new CostMakeUpForVehicle
            {
                ServiceRecordSum = serviceRecords.Sum(x => x.Cost),
                GasRecordSum = gasRecords.Sum(x => x.Cost),
                CollisionRecordSum = collisionRecords.Sum(x => x.Cost),
                TaxRecordSum = taxRecords.Sum(x => x.Cost),
                UpgradeRecordSum = upgradeRecords.Sum(x => x.Cost)
            };
            return PartialView("Report/_CostMakeUpReport", viewModel);
        }
        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        public IActionResult GetCostTableForVehicle(int vehicleId, int year = 0)
        {
            var vehicleRecords = _vehicleLogic.GetVehicleRecords(vehicleId);
            var serviceRecords = vehicleRecords.ServiceRecords;
            var gasRecords = vehicleRecords.GasRecords;
            var collisionRecords = vehicleRecords.CollisionRecords;
            var taxRecords = vehicleRecords.TaxRecords;
            var upgradeRecords = vehicleRecords.UpgradeRecords;
            var odometerRecords = vehicleRecords.OdometerRecords;
            if (year != default)
            {
                serviceRecords.RemoveAll(x => x.Date.Year != year);
                gasRecords.RemoveAll(x => x.Date.Year != year);
                collisionRecords.RemoveAll(x => x.Date.Year != year);
                taxRecords.RemoveAll(x => x.Date.Year != year);
                upgradeRecords.RemoveAll(x => x.Date.Year != year);
                odometerRecords.RemoveAll(x => x.Date.Year != year);
            }
            var maxMileage = _vehicleLogic.GetMaxMileage(vehicleRecords);
            var minMileage = _vehicleLogic.GetMinMileage(vehicleRecords);
            var vehicleData = _dataAccess.GetVehicleById(vehicleId);
            var userConfig = _config.GetUserConfig(User);
            var totalDistanceTraveled = maxMileage - minMileage;
            var totalDays = _vehicleLogic.GetOwnershipDays(vehicleData.PurchaseDate, vehicleData.SoldDate, year, serviceRecords, collisionRecords, gasRecords, upgradeRecords, odometerRecords, taxRecords);
            var viewModel = new CostTableForVehicle
            {
                ServiceRecordSum = serviceRecords.Sum(x => x.Cost),
                GasRecordSum = gasRecords.Sum(x => x.Cost),
                CollisionRecordSum = collisionRecords.Sum(x => x.Cost),
                TaxRecordSum = taxRecords.Sum(x => x.Cost),
                UpgradeRecordSum = upgradeRecords.Sum(x => x.Cost),
                TotalDistance = totalDistanceTraveled,
                DistanceUnit = vehicleData.UseHours ? "Cost Per Hour" : userConfig.UseMPG ? "Cost Per Mile" : "Cost Per Kilometer",
                NumberOfDays = totalDays
            };
            return PartialView("Report/_CostTableReport", viewModel);
        }
        [TypeFilter(typeof(CollaboratorFilter))]
        public IActionResult GetVehicleImageMap(int vehicleId)
        {
            var vehicleData = _dataAccess.GetVehicleById(vehicleId);
            VehicleImageMap imageMap = new VehicleImageMap();
            if (!string.IsNullOrWhiteSpace(vehicleData.MapLocation))
            {
                var fullFilePath = _fileHelper.GetFullFilePath(vehicleData.MapLocation);
                if (!string.IsNullOrWhiteSpace(fullFilePath))
                {
                    var fullFileText = _fileHelper.GetFileText(fullFilePath);
                    imageMap = JsonSerializer.Deserialize<VehicleImageMap>(fullFileText) ?? new VehicleImageMap();
                }
            }
            return PartialView("Report/_PetBodyMap", imageMap);
        }
        [TypeFilter(typeof(CollaboratorFilter))]
        public IActionResult GetReminderMakeUpByVehicle(int vehicleId, int daysToAdd)
        {
            var reminders = GetRemindersAndUrgency(vehicleId, DateTime.Now.AddDays(daysToAdd));
            var viewModel = new ReminderMakeUpForVehicle
            {
                NotUrgentCount = reminders.Where(x => x.Urgency == ReminderUrgency.NotUrgent).Count(),
                UrgentCount = reminders.Where(x => x.Urgency == ReminderUrgency.Urgent).Count(),
                VeryUrgentCount = reminders.Where(x => x.Urgency == ReminderUrgency.VeryUrgent).Count(),
                PastDueCount = reminders.Where(x => x.Urgency == ReminderUrgency.PastDue).Count()
            };
            return PartialView("Report/_ReminderMakeUpReport", viewModel);
        }
        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpPost]
        public IActionResult GetVehicleAttachments(int vehicleId, List<ImportMode> exportTabs)
        {
            List<GenericReportModel> attachmentData = new List<GenericReportModel>();
            if (exportTabs.Contains(ImportMode.ServiceRecord))
            {
                var records = _serviceRecordDataAccess.GetServiceRecordsByVehicleId(vehicleId).Where(x => x.Files.Any());
                attachmentData.AddRange(records.Select(x => new GenericReportModel
                {
                    DataType = ImportMode.ServiceRecord,
                    Date = x.Date,
                    Odometer = x.Mileage,
                    Files = x.Files
                }));
            }
            if (exportTabs.Contains(ImportMode.RepairRecord))
            {
                var records = _collisionRecordDataAccess.GetCollisionRecordsByVehicleId(vehicleId).Where(x => x.Files.Any());
                attachmentData.AddRange(records.Select(x => new GenericReportModel
                {
                    DataType = ImportMode.RepairRecord,
                    Date = x.Date,
                    Odometer = x.Mileage,
                    Files = x.Files
                }));
            }
            if (exportTabs.Contains(ImportMode.UpgradeRecord))
            {
                var records = _upgradeRecordDataAccess.GetUpgradeRecordsByVehicleId(vehicleId).Where(x => x.Files.Any());
                attachmentData.AddRange(records.Select(x => new GenericReportModel
                {
                    DataType = ImportMode.UpgradeRecord,
                    Date = x.Date,
                    Odometer = x.Mileage,
                    Files = x.Files
                }));
            }
            if (exportTabs.Contains(ImportMode.GasRecord))
            {
                var records = _gasRecordDataAccess.GetGasRecordsByVehicleId(vehicleId).Where(x => x.Files.Any());
                attachmentData.AddRange(records.Select(x => new GenericReportModel
                {
                    DataType = ImportMode.GasRecord,
                    Date = x.Date,
                    Odometer = x.Mileage,
                    Files = x.Files
                }));
            }
            if (exportTabs.Contains(ImportMode.TaxRecord))
            {
                var records = _taxRecordDataAccess.GetTaxRecordsByVehicleId(vehicleId).Where(x => x.Files.Any());
                attachmentData.AddRange(records.Select(x => new GenericReportModel
                {
                    DataType = ImportMode.TaxRecord,
                    Date = x.Date,
                    Odometer = 0,
                    Files = x.Files
                }));
            }
            if (exportTabs.Contains(ImportMode.OdometerRecord))
            {
                var records = _odometerRecordDataAccess.GetOdometerRecordsByVehicleId(vehicleId).Where(x => x.Files.Any());
                attachmentData.AddRange(records.Select(x => new GenericReportModel
                {
                    DataType = ImportMode.OdometerRecord,
                    Date = x.Date,
                    Odometer = x.Mileage,
                    Files = x.Files
                }));
            }
            if (exportTabs.Contains(ImportMode.NoteRecord))
            {
                var records = _noteDataAccess.GetNotesByVehicleId(vehicleId).Where(x => x.Files.Any());
                attachmentData.AddRange(records.Select(x => new GenericReportModel
                {
                    DataType = ImportMode.NoteRecord,
                    Date = DateTime.Now,
                    Odometer = 0,
                    Files = x.Files
                }));
            }
            if (exportTabs.Contains(ImportMode.PlanRecord))
            {
                var records = _planRecordDataAccess.GetPlanRecordsByVehicleId(vehicleId).Where(x => x.Files.Any());
                attachmentData.AddRange(records.Select(x => new GenericReportModel
                {
                    DataType = ImportMode.PlanRecord,
                    Date = x.DateCreated,
                    Odometer = 0,
                    Files = x.Files
                }));
            }
            if (exportTabs.Contains(ImportMode.SupplyRecord))
            {
                var records = _supplyRecordDataAccess.GetSupplyRecordsByVehicleId(vehicleId).Where(x => x.Files.Any());
                attachmentData.AddRange(records.Select(x => new GenericReportModel
                {
                    DataType = ImportMode.SupplyRecord,
                    Date = x.Date,
                    Odometer = 0,
                    Files = x.Files
                }));
            }
            if (exportTabs.Contains(ImportMode.InspectionRecord))
            {
                var records = _inspectionRecordDataAccess.GetInspectionRecordsByVehicleId(vehicleId).Where(x => x.Files.Any());
                attachmentData.AddRange(records.Select(x => new GenericReportModel
                {
                    DataType = ImportMode.InspectionRecord,
                    Date = x.Date,
                    Odometer = x.Mileage,
                    Files = x.Files
                }));
            }
            if (exportTabs.Contains(ImportMode.EquipmentRecord))
            {
                var records = _equipmentRecordDataAccess.GetEquipmentRecordsByVehicleId(vehicleId).Where(x => x.Files.Any());
                attachmentData.AddRange(records.Select(x => new GenericReportModel
                {
                    DataType = ImportMode.EquipmentRecord,
                    Date = DateTime.Now,
                    Odometer = 0,
                    Files = x.Files
                }));
            }
            if (attachmentData.Any())
            {
                attachmentData = attachmentData.OrderBy(x => x.Date).ThenBy(x => x.Odometer).ToList();
                var result = _fileHelper.MakeAttachmentsExport(attachmentData);
                if (string.IsNullOrWhiteSpace(result))
                {
                    return Json(OperationResponse.Failed());
                }
                return Json(OperationResponse.Succeed(result));
            }
            else
            {
                return Json(OperationResponse.Failed("No Attachments Found"));
            }
        }
        public IActionResult GetReportParameters(int vehicleId = 0)
        {
            bool isPetProfile = false;
            if (vehicleId > 0)
            {
                var vehicle = _dataAccess.GetVehicleById(vehicleId);
                isPetProfile = vehicle != null && !string.IsNullOrWhiteSpace(vehicle.PetName);
            }

            var viewModel = new ReportParameter() { 
                VisibleColumns = new List<string> {
                    nameof(GenericReportModel.DataType),
                    nameof(GenericReportModel.Date),
                    nameof(GenericReportModel.Odometer),
                    nameof(GenericReportModel.Description),
                    nameof(GenericReportModel.Provider),
                    nameof(GenericReportModel.Cost),
                    nameof(GenericReportModel.Notes),
                    nameof(GenericReportModel.WeightValue)
                }
            };

            if (isPetProfile)
            {
                viewModel.VisibleColumns.Remove(nameof(GenericReportModel.Odometer));
            }

            //get all extra fields from service records, repairs, upgrades, and tax records.
            var recordTypes = new List<int>() { 0, 1, 3, 4 };
            var extraFields = new List<string>();
            foreach(int recordType in recordTypes)
            {
                extraFields.AddRange(_extraFieldDataAccess.GetExtraFieldsById(recordType).ExtraFields.Select(x => x.Name));
            }
            viewModel.ExtraFields = extraFields.Distinct().ToList();

            return PartialView("Report/_ReportParameters", viewModel);
        }
        [TypeFilter(typeof(CollaboratorFilter))]
        public IActionResult GetVehicleHistory(int vehicleId, ReportParameter reportParameter)
        {
            var careHistory = new CareHistoryViewModel();
            careHistory.ReportParameters = reportParameter;
            careHistory.VehicleData = _dataAccess.GetVehicleById(vehicleId);
            bool isPetProfile = !string.IsNullOrWhiteSpace(careHistory.VehicleData.PetName);
            if (isPetProfile)
            {
                careHistory.ReportParameters.VisibleColumns.Remove(nameof(GenericReportModel.Odometer));
            }
            var vehicleRecords = _vehicleLogic.GetVehicleRecords(vehicleId);
            bool useMPG = _config.GetUserConfig(User).UseMPG;
            bool useUKMPG = !careHistory.VehicleData.IsElectric && _config.GetUserConfig(User).UseUKMPG;
            var gasViewModels = _gasHelper.GetGasRecordViewModels(vehicleRecords.GasRecords, useMPG, useUKMPG);
            //filter by tags
            if (reportParameter.Tags.Any())
            {
                if (reportParameter.TagFilter == TagFilter.Exclude)
                {
                    vehicleRecords.OdometerRecords.RemoveAll(x => x.Tags.Any(y => reportParameter.Tags.Contains(y)));
                    vehicleRecords.ServiceRecords.RemoveAll(x => x.Tags.Any(y => reportParameter.Tags.Contains(y)));
                    vehicleRecords.CollisionRecords.RemoveAll(x => x.Tags.Any(y => reportParameter.Tags.Contains(y)));
                    vehicleRecords.UpgradeRecords.RemoveAll(x => x.Tags.Any(y => reportParameter.Tags.Contains(y)));
                    vehicleRecords.TaxRecords.RemoveAll(x => x.Tags.Any(y => reportParameter.Tags.Contains(y)));
                    gasViewModels.RemoveAll(x => x.Tags.Any(y => reportParameter.Tags.Contains(y)));
                    vehicleRecords.GasRecords.RemoveAll(x => x.Tags.Any(y => reportParameter.Tags.Contains(y)));
                }
                else if (reportParameter.TagFilter == TagFilter.IncludeOnly)
                {
                    vehicleRecords.OdometerRecords.RemoveAll(x => !x.Tags.Any(y => reportParameter.Tags.Contains(y)));
                    vehicleRecords.ServiceRecords.RemoveAll(x => !x.Tags.Any(y => reportParameter.Tags.Contains(y)));
                    vehicleRecords.CollisionRecords.RemoveAll(x => !x.Tags.Any(y => reportParameter.Tags.Contains(y)));
                    vehicleRecords.UpgradeRecords.RemoveAll(x => !x.Tags.Any(y => reportParameter.Tags.Contains(y)));
                    vehicleRecords.TaxRecords.RemoveAll(x => !x.Tags.Any(y => reportParameter.Tags.Contains(y)));
                    gasViewModels.RemoveAll(x => !x.Tags.Any(y => reportParameter.Tags.Contains(y)));
                    vehicleRecords.GasRecords.RemoveAll(x => !x.Tags.Any(y => reportParameter.Tags.Contains(y)));
                }
            }
            //filter by date range.
            if (reportParameter.FilterByDateRange && !string.IsNullOrWhiteSpace(reportParameter.StartDate) && !string.IsNullOrWhiteSpace(reportParameter.EndDate))
            {
                var startDate = DateTime.Parse(reportParameter.StartDate).Date;
                var endDate = DateTime.Parse(reportParameter.EndDate).Date;
                //validate date range
                if (endDate >= startDate) //allow for same day.
                {
                    careHistory.StartDate = reportParameter.StartDate;
                    careHistory.EndDate = reportParameter.EndDate;
                    //remove all records with dates after the end date and dates before the start date.
                    vehicleRecords.OdometerRecords.RemoveAll(x => x.Date.Date > endDate || x.Date.Date < startDate);
                    vehicleRecords.ServiceRecords.RemoveAll(x => x.Date.Date > endDate || x.Date.Date < startDate);
                    vehicleRecords.CollisionRecords.RemoveAll(x => x.Date.Date > endDate || x.Date.Date < startDate);
                    vehicleRecords.UpgradeRecords.RemoveAll(x => x.Date.Date > endDate || x.Date.Date < startDate);
                    vehicleRecords.TaxRecords.RemoveAll(x => x.Date.Date > endDate || x.Date.Date < startDate);
                    gasViewModels.RemoveAll(x => DateTime.Parse(x.Date).Date > endDate || DateTime.Parse(x.Date).Date < startDate);
                    vehicleRecords.GasRecords.RemoveAll(x => x.Date.Date > endDate || x.Date.Date < startDate);
                }
            }
            var maxMileage = _vehicleLogic.GetMaxMileage(vehicleRecords);
            careHistory.Odometer = maxMileage.ToString("N0");
            var minMileage = _vehicleLogic.GetMinMileage(vehicleRecords);
            var distanceTraveled = maxMileage - minMileage;
            if (!string.IsNullOrWhiteSpace(careHistory.VehicleData.PurchaseDate))
            {
                var endDate = careHistory.VehicleData.SoldDate;
                int daysOwned = 0;
                if (string.IsNullOrWhiteSpace(endDate))
                {
                    endDate = DateTime.Now.ToShortDateString();
                }
                try
                {
                    daysOwned = (DateTime.Parse(endDate) - DateTime.Parse(careHistory.VehicleData.PurchaseDate)).Days;
                    careHistory.DaysOwned = daysOwned.ToString("N0");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    careHistory.DaysOwned = string.Empty;
                }
                //calculate depreciation
                var totalDepreciation = careHistory.VehicleData.PurchasePrice - careHistory.VehicleData.SoldPrice;
                //we only calculate depreciation if a sold price is provided.
                if (totalDepreciation != default && careHistory.VehicleData.SoldPrice != default)
                {
                    careHistory.TotalDepreciation = totalDepreciation;
                    if (daysOwned != default)
                    {
                        careHistory.DepreciationPerDay = Math.Abs(totalDepreciation / daysOwned);
                    }
                    if (distanceTraveled != default)
                    {
                        careHistory.DepreciationPerMile = Math.Abs(totalDepreciation / distanceTraveled);
                    }
                }
            }
            List<GenericReportModel> reportData = new List<GenericReportModel>();
            string preferredFuelMileageUnit = _config.GetUserConfig(User).PreferredGasMileageUnit;
            careHistory.DistanceUnit = careHistory.VehicleData.UseHours ? "h" : useMPG ? "mi." : "km";
            careHistory.TotalGasCost = gasViewModels.Sum(x => x.Cost);
            careHistory.TotalCost = vehicleRecords.ServiceRecords.Sum(x => x.Cost) + vehicleRecords.CollisionRecords.Sum(x => x.Cost) + vehicleRecords.UpgradeRecords.Sum(x => x.Cost) + vehicleRecords.TaxRecords.Sum(x => x.Cost);
            if (distanceTraveled != default)
            {
                careHistory.DistanceTraveled = distanceTraveled.ToString("N0");
                careHistory.TotalCostPerMile = careHistory.TotalCost / distanceTraveled;
                careHistory.TotalGasCostPerMile = careHistory.TotalGasCost / distanceTraveled;
            }
            var averageMPG = "0";
            if (gasViewModels.Any())
            {
                averageMPG = _gasHelper.GetAverageGasMileage(gasViewModels, useMPG);
            }
            var fuelEconomyMileageUnit = StaticHelper.GetFuelEconomyUnit(careHistory.VehicleData.IsElectric, careHistory.VehicleData.UseHours, useMPG, useUKMPG);
            if (fuelEconomyMileageUnit == "l/100km" && preferredFuelMileageUnit == "km/l")
            {
                //conversion needed.
                var newAverageMPG = decimal.Parse(averageMPG, NumberStyles.Any);
                if (newAverageMPG != 0)
                {
                    newAverageMPG = 100 / newAverageMPG;
                }
                averageMPG = newAverageMPG.ToString("F");
                fuelEconomyMileageUnit = preferredFuelMileageUnit;
            }
            careHistory.MPG = $"{averageMPG} {fuelEconomyMileageUnit}";
            //insert servicerecords
            reportData.AddRange(vehicleRecords.ServiceRecords.Select(x => new GenericReportModel
            {
                Date = x.Date,
                Odometer = x.Mileage,
                Description = x.Description,
                Notes = x.Notes,
                Cost = x.Cost,
                DataType = ImportMode.ServiceRecord,
                ExtraFields = x.ExtraFields,
                RequisitionHistory = x.RequisitionHistory
            }));
            //repair records
            reportData.AddRange(vehicleRecords.CollisionRecords.Select(x => new GenericReportModel
            {
                Date = x.Date,
                Odometer = x.Mileage,
                Description = x.Description,
                Notes = x.Notes,
                Cost = x.Cost,
                DataType = ImportMode.RepairRecord,
                ExtraFields = x.ExtraFields,
                RequisitionHistory = x.RequisitionHistory
            }));
            reportData.AddRange(vehicleRecords.UpgradeRecords.Select(x => new GenericReportModel
            {
                Date = x.Date,
                Odometer = x.Mileage,
                Description = x.Description,
                Notes = x.Notes,
                Cost = x.Cost,
                DataType = ImportMode.UpgradeRecord,
                ExtraFields = x.ExtraFields,
                RequisitionHistory = x.RequisitionHistory
            }));
            reportData.AddRange(vehicleRecords.TaxRecords.Select(x => new GenericReportModel
            {
                Date = x.Date,
                Odometer = 0,
                Description = x.Description,
                Notes = x.Notes,
                Cost = x.Cost,
                DataType = ImportMode.TaxRecord,
                ExtraFields = x.ExtraFields
            }));
            // Phase 7 – include HealthRecords in the per-pet timeline
            var healthRecords = _healthRecordDataAccess.GetHealthRecordsByVehicleId(vehicleId);
            if (reportParameter.FilterByDateRange && !string.IsNullOrWhiteSpace(reportParameter.StartDate) && !string.IsNullOrWhiteSpace(reportParameter.EndDate))
            {
                if (DateTime.TryParse(reportParameter.StartDate, out DateTime hrStart) &&
                    DateTime.TryParse(reportParameter.EndDate, out DateTime hrEnd) && hrEnd >= hrStart)
                {
                    healthRecords.RemoveAll(x => x.Date.Date > hrEnd.Date || x.Date.Date < hrStart.Date);
                }
            }
            reportData.AddRange(healthRecords.Select(x => new GenericReportModel
            {
                Date = x.Date,
                Odometer = 0,
                Description = string.IsNullOrWhiteSpace(x.Title) ? x.Description : x.Title,
                Notes = x.Notes,
                Cost = x.Cost,
                DataType = ImportMode.HealthRecord,
                ExtraFields = x.ExtraFields,
                // Pet-specific fields restored for report/print output
                WeightValue = x.WeightValue,
                WeightUnit = x.WeightUnit,
                Provider = x.Provider,
                Category = System.Text.RegularExpressions.Regex.Replace(x.Category.ToString(), "(?<=[a-z])([A-Z])", " $1")
            }));
            careHistory.CareHistory = reportData.OrderBy(x => x.Date).ThenBy(x => x.Odometer).ToList();
            return PartialView("Report/_CareHistory", careHistory);
        }

        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        public IActionResult GetWeightTrendChartPartialView(int vehicleId)
        {
            return PartialView("Report/_WeightTrendChart");
        }

        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        public IActionResult GetPetSummaryData(int vehicleId)
        {
            var pet = _dataAccess.GetVehicleById(vehicleId);
            var cutoffDate = DateTime.Now.AddYears(-1);
            var reminderCutoff = DateTime.Now.AddDays(90);

            var vaccinations = _vaccinationRecordDataAccess
                .GetVaccinationRecordsByVehicleId(vehicleId)
                .OrderByDescending(x => x.Date)
                .ToList();

            var activeMedications = _medicationRecordDataAccess
                .GetMedicationRecordsByVehicleId(vehicleId)
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.Date)
                .ToList();

            var allHealthRecords = _healthRecordDataAccess.GetHealthRecordsByVehicleId(vehicleId);

            var knownAllergies = allHealthRecords
                .Where(x => x.Category == HealthRecordCategory.AllergyReaction)
                .OrderByDescending(x => x.Date)
                .ToList();

            var recentHealthRecords = allHealthRecords
                .Where(x => x.Date >= cutoffDate && x.Category != HealthRecordCategory.AllergyReaction)
                .OrderByDescending(x => x.Date)
                .ToList();

            var weightHistory = allHealthRecords
                .Where(x => x.Category == HealthRecordCategory.WeightCheck && x.WeightValue > 0)
                .OrderByDescending(x => x.Date)
                .Take(10)
                .ToList();

            var upcomingReminders = _reminderRecordDataAccess
                .GetReminderRecordsByVehicleId(vehicleId)
                .Where(x => x.Date <= reminderCutoff && x.Date >= DateTime.Now.AddDays(-1))
                .OrderBy(x => x.Date)
                .ToList();

            var viewModel = new PetSummaryViewModel
            {
                PetData = pet,
                Vaccinations = vaccinations,
                ActiveMedications = activeMedications,
                KnownAllergies = knownAllergies,
                RecentHealthRecords = recentHealthRecords,
                WeightHistory = weightHistory,
                UpcomingReminders = upcomingReminders,
                GeneratedDate = DateTime.Now.ToShortDateString()
            };

            return PartialView("Report/_PetSummary", viewModel);
        }

        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpPost]
        public IActionResult GetMonthMPGByVehicle(int vehicleId, int year = 0)
        {
            var vehicleData = _dataAccess.GetVehicleById(vehicleId);
            var gasRecords = _gasRecordDataAccess.GetGasRecordsByVehicleId(vehicleId);
            var userConfig = _config.GetUserConfig(User);
            string preferredFuelMileageUnit = _config.GetUserConfig(User).PreferredGasMileageUnit;
            var fuelEconomyMileageUnit = StaticHelper.GetFuelEconomyUnit(vehicleData.IsElectric, vehicleData.UseHours, userConfig.UseMPG, userConfig.UseUKMPG);
            bool invertedFuelMileageUnit = fuelEconomyMileageUnit == "l/100km" && preferredFuelMileageUnit == "km/l";
            var mileageData = _gasHelper.GetGasRecordViewModels(gasRecords, userConfig.UseMPG, !vehicleData.IsElectric && userConfig.UseUKMPG);
            if (year != 0)
            {
                mileageData.RemoveAll(x => DateTime.Parse(x.Date).Year != year);
            }
            mileageData.RemoveAll(x => x.MilesPerGallon == default);
            var monthlyMileageData = StaticHelper.GetBaseLineCostsNoMonthName();
            monthlyMileageData.AddRange(mileageData.GroupBy(x => x.MonthId).Select(x => new CostForVehicleByMonth
            {
                MonthId = x.Key,
                Cost = x.Average(y => y.MilesPerGallon)
            }));
            monthlyMileageData = monthlyMileageData.GroupBy(x => x.MonthId).OrderBy(x => x.Key).Select(x => new CostForVehicleByMonth
            {
                MonthId = x.Key,
                MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(x.Key),
                Cost = x.Sum(y => y.Cost)
            }).ToList();
            if (invertedFuelMileageUnit)
            {
                foreach (CostForVehicleByMonth monthMileage in monthlyMileageData)
                {
                    if (monthMileage.Cost != default)
                    {
                        monthMileage.Cost = 100 / monthMileage.Cost;
                    }
                }
            }
            var mpgViewModel = new MPGForVehicleByMonth
            {
                CostData = monthlyMileageData,
                Unit = invertedFuelMileageUnit ? preferredFuelMileageUnit : fuelEconomyMileageUnit,
                SortedCostData = (userConfig.UseMPG || invertedFuelMileageUnit) ? monthlyMileageData.OrderByDescending(x => x.Cost).ToList() : monthlyMileageData.OrderBy(x => x.Cost).ToList()
            };
            return PartialView("Report/_MPGByMonthReport", mpgViewModel);
        }
        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpPost]
        public IActionResult GetCostByMonthByVehicle(int vehicleId, List<ImportMode> selectedMetrics, int year = 0)
        {
            List<CostForVehicleByMonth> allCosts = StaticHelper.GetBaseLineCosts();
            if (selectedMetrics.Contains(ImportMode.ServiceRecord))
            {
                var serviceRecords = _serviceRecordDataAccess.GetServiceRecordsByVehicleId(vehicleId);
                allCosts.AddRange(_reportHelper.GetServiceRecordSum(serviceRecords, year));
            }
            if (selectedMetrics.Contains(ImportMode.RepairRecord))
            {
                var repairRecords = _collisionRecordDataAccess.GetCollisionRecordsByVehicleId(vehicleId);
                allCosts.AddRange(_reportHelper.GetRepairRecordSum(repairRecords, year));
            }
            if (selectedMetrics.Contains(ImportMode.UpgradeRecord))
            {
                var upgradeRecords = _upgradeRecordDataAccess.GetUpgradeRecordsByVehicleId(vehicleId);
                allCosts.AddRange(_reportHelper.GetUpgradeRecordSum(upgradeRecords, year));
            }
            if (selectedMetrics.Contains(ImportMode.GasRecord))
            {
                var gasRecords = _gasRecordDataAccess.GetGasRecordsByVehicleId(vehicleId);
                allCosts.AddRange(_reportHelper.GetGasRecordSum(gasRecords, year));
            }
            if (selectedMetrics.Contains(ImportMode.TaxRecord))
            {
                var taxRecords = _taxRecordDataAccess.GetTaxRecordsByVehicleId(vehicleId);
                allCosts.AddRange(_reportHelper.GetTaxRecordSum(taxRecords, year));
            }
            if (selectedMetrics.Contains(ImportMode.OdometerRecord))
            {
                var odometerRecords = _odometerRecordDataAccess.GetOdometerRecordsByVehicleId(vehicleId);
                allCosts.AddRange(_reportHelper.GetOdometerRecordSum(odometerRecords, year));
            }
            var groupedRecord = allCosts.GroupBy(x => new { x.MonthName, x.MonthId }).OrderBy(x => x.Key.MonthId).Select(x => new CostForVehicleByMonth
            {
                MonthName = x.Key.MonthName,
                Cost = x.Sum(y => y.Cost),
                DistanceTraveled = x.Max(y => y.DistanceTraveled)
            }).ToList();
            return PartialView("Report/_GasCostByMonthReport", groupedRecord);
        }
        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpPost]
        public IActionResult GetCostByMonthAndYearByVehicle(int vehicleId, List<ImportMode> selectedMetrics, int year = 0)
        {
            List<CostForVehicleByMonth> allCosts = StaticHelper.GetBaseLineCosts();
            if (selectedMetrics.Contains(ImportMode.ServiceRecord))
            {
                var serviceRecords = _serviceRecordDataAccess.GetServiceRecordsByVehicleId(vehicleId);
                allCosts.AddRange(_reportHelper.GetServiceRecordSum(serviceRecords, year, true));
            }
            if (selectedMetrics.Contains(ImportMode.RepairRecord))
            {
                var repairRecords = _collisionRecordDataAccess.GetCollisionRecordsByVehicleId(vehicleId);
                allCosts.AddRange(_reportHelper.GetRepairRecordSum(repairRecords, year, true));
            }
            if (selectedMetrics.Contains(ImportMode.UpgradeRecord))
            {
                var upgradeRecords = _upgradeRecordDataAccess.GetUpgradeRecordsByVehicleId(vehicleId);
                allCosts.AddRange(_reportHelper.GetUpgradeRecordSum(upgradeRecords, year, true));
            }
            if (selectedMetrics.Contains(ImportMode.GasRecord))
            {
                var gasRecords = _gasRecordDataAccess.GetGasRecordsByVehicleId(vehicleId);
                allCosts.AddRange(_reportHelper.GetGasRecordSum(gasRecords, year, true));
            }
            if (selectedMetrics.Contains(ImportMode.TaxRecord))
            {
                var taxRecords = _taxRecordDataAccess.GetTaxRecordsByVehicleId(vehicleId);
                allCosts.AddRange(_reportHelper.GetTaxRecordSum(taxRecords, year, true));
            }
            if (selectedMetrics.Contains(ImportMode.OdometerRecord))
            {
                var odometerRecords = _odometerRecordDataAccess.GetOdometerRecordsByVehicleId(vehicleId);
                allCosts.AddRange(_reportHelper.GetOdometerRecordSum(odometerRecords, year, true));
            }
            var groupedRecord = allCosts.GroupBy(x => new { x.MonthName, x.MonthId, x.Year }).OrderByDescending(x=>x.Key.Year).Select(x => new CostForVehicleByMonth
            {
                Year = x.Key.Year,
                MonthName = x.Key.MonthName,
                Cost = x.Sum(y => y.Cost),
                DistanceTraveled = x.Max(y => y.DistanceTraveled),
                MonthId = x.Key.MonthId
            }).ToList();
            var vehicleData = _dataAccess.GetVehicleById(vehicleId);
            var userConfig = _config.GetUserConfig(User);
            var viewModel = new CostDistanceTableForVehicle { CostData = groupedRecord };
            viewModel.DistanceUnit = vehicleData.UseHours ? "h" : userConfig.UseMPG ? "mi." : "km";
            return PartialView("Report/_CostDistanceTableReport", viewModel);
        }
        [HttpGet]
        public IActionResult GetAdditionalWidgets()
        {
            var widgets = _fileHelper.GetWidgets();
            return PartialView("Report/_ReportWidgets", widgets);
        }
        [HttpGet]
        public IActionResult GetImportModeSelector()
        {
            return PartialView("Report/_ImportModeSelector");
        }
    }
}
