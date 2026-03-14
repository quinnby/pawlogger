using CarCareTracker.Filter;
using CarCareTracker.Helper;
using CarCareTracker.Models;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers
{
    public partial class APIController
    {
        [HttpGet]
        [Route("/api/vehicle/notes/all")]
        public IActionResult AllNotes(MethodParameter parameters)
        {
            MarkContractUsage("/api/vehicle/notes/all");
            List<int> vehicleIds = new List<int>();
            var vehicles = _dataAccess.GetVehicles();
            if (!User.IsInRole(nameof(UserData.IsRootUser)))
            {
                vehicles = _userLogic.FilterUserVehicles(vehicles, GetUserID());
            }
            vehicleIds.AddRange(vehicles.Select(x => x.Id));
            List<Note> vehicleRecords = new List<Note>();
            foreach (int vehicleId in vehicleIds)
            {
                vehicleRecords.AddRange(_noteDataAccess.GetNotesByVehicleId(vehicleId));
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
            var result = vehicleRecords
                .Select(x => new NoteRecordExportModel
                {
                    VehicleId = x.VehicleId.ToString(),
                    Id = x.Id.ToString(),
                    Description = x.Description,
                    NoteText = x.NoteText,
                    Pinned = x.Pinned.ToString(),
                    ExtraFields = x.ExtraFields,
                    Files = x.Files,
                    Tags = string.Join(' ', x.Tags)
                });
            if (_config.GetInvariantApi() || Request.Headers.ContainsKey("culture-invariant"))
            {
                return Json(result, StaticHelper.GetInvariantOption());
            }
            else
            {
                return Json(result);
            }
        }

        [HttpGet]
        [Route("/api/v2/profiles/notes/all")]
        public IActionResult AllNotesV2(MethodParameter parameters) => AllNotes(parameters);

        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        [Route("/api/vehicle/notes")]
        public IActionResult Notes(int vehicleId = default, MethodParameter? parameters = null, int petProfileId = default)
        {
            MarkContractUsage("/api/vehicle/notes");
            parameters ??= new MethodParameter();
            var resolvedVehicleId = ResolveVehicleIdAlias(vehicleId, petProfileId, "/api/vehicle/notes");
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
            var vehicleRecords = _noteDataAccess.GetNotesByVehicleId(resolvedVehicleId);
            if (parameters.Id != default)
            {
                vehicleRecords.RemoveAll(x => x.Id != parameters.Id);
            }
            if (!string.IsNullOrWhiteSpace(parameters.Tags))
            {
                var tagsFilter = parameters.Tags.Split(' ').Distinct();
                vehicleRecords.RemoveAll(x => !x.Tags.Any(y => tagsFilter.Contains(y)));
            }
            var result = vehicleRecords
                .Select(x => new NoteRecordExportModel
                {
                    VehicleId = x.VehicleId.ToString(),
                    Id = x.Id.ToString(),
                    Description = x.Description,
                    NoteText = x.NoteText,
                    Pinned = x.Pinned.ToString(),
                    ExtraFields = x.ExtraFields,
                    Files = x.Files,
                    Tags = string.Join(' ', x.Tags)
                });
            if (_config.GetInvariantApi() || Request.Headers.ContainsKey("culture-invariant"))
            {
                return Json(result, StaticHelper.GetInvariantOption());
            }
            else
            {
                return Json(result);
            }
        }

        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        [Route("/api/v2/profiles/notes")]
        public IActionResult NotesV2(int petProfileId = default, MethodParameter? parameters = null, int vehicleId = default)
            => Notes(vehicleId, parameters, petProfileId);

        [TypeFilter(typeof(QueryParamFilter), Arguments = new object[] { new string[] { "vehicleId" } })]
        [TypeFilter(typeof(APIKeyFilter), Arguments = new object[] { HouseholdPermission.Edit })]
        [TypeFilter(typeof(CollaboratorFilter), Arguments = new object[] { false, true, HouseholdPermission.Edit })]
        [HttpPost]
        [Route("/api/vehicle/notes/add")]
        [Consumes("application/json")]
        public IActionResult AddNoteJson(int vehicleId, [FromBody] NoteRecordExportModel input, int petProfileId = default)
            => AddNote(vehicleId, input, petProfileId);
        [TypeFilter(typeof(APIKeyFilter), Arguments = new object[] { HouseholdPermission.Edit })]
        [TypeFilter(typeof(CollaboratorFilter), Arguments = new object[] { false, true, HouseholdPermission.Edit })]
        [HttpPost]
        [Route("/api/vehicle/notes/add")]
        public IActionResult AddNote(int vehicleId, NoteRecordExportModel input, int petProfileId = default)
        {
            return AddNoteCore(vehicleId, petProfileId, input, "/api/vehicle/notes/add", false);
        }

        [TypeFilter(typeof(QueryParamFilter), Arguments = new object[] { new string[] { "vehicleId" } })]
        [TypeFilter(typeof(APIKeyFilter), Arguments = new object[] { HouseholdPermission.Edit })]
        [TypeFilter(typeof(CollaboratorFilter), Arguments = new object[] { false, true, HouseholdPermission.Edit })]
        [HttpPost]
        [Route("/api/v2/profiles/notes/add")]
        [Consumes("application/json")]
        public IActionResult AddNoteV2Json(int vehicleId, [FromBody] NoteRecordExportModel input, int petProfileId = default)
            => AddNoteV2(vehicleId, input, petProfileId);

        [TypeFilter(typeof(APIKeyFilter), Arguments = new object[] { HouseholdPermission.Edit })]
        [TypeFilter(typeof(CollaboratorFilter), Arguments = new object[] { false, true, HouseholdPermission.Edit })]
        [HttpPost]
        [Route("/api/v2/profiles/notes/add")]
        public IActionResult AddNoteV2(int vehicleId, NoteRecordExportModel input, int petProfileId = default)
        {
            if (!IsNotesWriteV2Enabled())
            {
                return NotesWriteV2Disabled("add");
            }
            if (IsLegacyIdAliasRejectedOnV2(vehicleId, petProfileId))
            {
                return NotesWriteV2AliasDisabled("add");
            }

            return AddNoteCore(vehicleId, petProfileId, input, "/api/vehicle/notes/add", true);
        }

        [TypeFilter(typeof(APIKeyFilter), Arguments = new object[] { HouseholdPermission.Delete })]
        [HttpDelete]
        [Route("/api/vehicle/notes/delete")]
        public IActionResult DeleteNote(int id)
        {
            return DeleteNoteCore(id, expectedVehicleId: default, routeLabel: "/api/vehicle/notes/delete", isV2Route: false);
        }

        [TypeFilter(typeof(QueryParamFilter), Arguments = new object[] { new string[] { "vehicleId" } })]
        [TypeFilter(typeof(APIKeyFilter), Arguments = new object[] { HouseholdPermission.Delete })]
        [HttpDelete]
        [Route("/api/v2/profiles/notes/delete")]
        public IActionResult DeleteNoteV2(int id, int petProfileId = default, int vehicleId = default)
        {
            if (!IsNotesWriteV2Enabled())
            {
                return NotesWriteV2Disabled("delete");
            }
            if (IsLegacyIdAliasRejectedOnV2(vehicleId, petProfileId))
            {
                return NotesWriteV2AliasDisabled("delete");
            }

            var resolvedVehicleId = ResolveVehicleIdAlias(vehicleId, petProfileId, "/api/v2/profiles/notes/delete");
            if (resolvedVehicleId == -1)
            {
                _logger.LogWarning("Phase13 notes write conflict rejected on delete: vehicleId={VehicleId} petProfileId={PetProfileId}", vehicleId, petProfileId);
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Input object invalid, vehicleId and petProfileId do not match."));
            }

            return DeleteNoteCore(id, resolvedVehicleId, "/api/vehicle/notes/delete", true);
        }

        [TypeFilter(typeof(APIKeyFilter), Arguments = new object[] { HouseholdPermission.Edit })]
        [HttpPut]
        [Route("/api/vehicle/notes/update")]
        [Consumes("application/json")]
        public IActionResult UpdateNoteJson([FromBody] NoteRecordExportModel input)
            => UpdateNote(input);

        [TypeFilter(typeof(APIKeyFilter), Arguments = new object[] { HouseholdPermission.Edit })]
        [HttpPut]
        [Route("/api/vehicle/notes/update")]
        public IActionResult UpdateNote(NoteRecordExportModel input)
        {
            return UpdateNoteCore(input, expectedVehicleId: default, routeLabel: "/api/vehicle/notes/update", isV2Route: false);
        }

        [TypeFilter(typeof(QueryParamFilter), Arguments = new object[] { new string[] { "vehicleId" } })]
        [TypeFilter(typeof(APIKeyFilter), Arguments = new object[] { HouseholdPermission.Edit })]
        [HttpPut]
        [Route("/api/v2/profiles/notes/update")]
        [Consumes("application/json")]
        public IActionResult UpdateNoteV2Json([FromBody] NoteRecordExportModel input, int petProfileId = default, int vehicleId = default)
            => UpdateNoteV2(input, petProfileId, vehicleId);

        [TypeFilter(typeof(QueryParamFilter), Arguments = new object[] { new string[] { "vehicleId" } })]
        [TypeFilter(typeof(APIKeyFilter), Arguments = new object[] { HouseholdPermission.Edit })]
        [HttpPut]
        [Route("/api/v2/profiles/notes/update")]
        public IActionResult UpdateNoteV2(NoteRecordExportModel input, int petProfileId = default, int vehicleId = default)
        {
            if (!IsNotesWriteV2Enabled())
            {
                return NotesWriteV2Disabled("update");
            }
            if (IsLegacyIdAliasRejectedOnV2(vehicleId, petProfileId))
            {
                return NotesWriteV2AliasDisabled("update");
            }

            var resolvedVehicleId = ResolveVehicleIdAlias(vehicleId, petProfileId, "/api/v2/profiles/notes/update");
            if (resolvedVehicleId == -1)
            {
                _logger.LogWarning("Phase13 notes write conflict rejected on update: vehicleId={VehicleId} petProfileId={PetProfileId}", vehicleId, petProfileId);
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Input object invalid, vehicleId and petProfileId do not match."));
            }

            if (resolvedVehicleId != default &&
                !string.IsNullOrWhiteSpace(input.VehicleId) &&
                int.TryParse(input.VehicleId, out var bodyVehicleId) &&
                bodyVehicleId != resolvedVehicleId &&
                _config.GetWriteV2StrictIdConflictReject())
            {
                _logger.LogWarning("Phase13 notes write conflict rejected on update body id: resolvedVehicleId={ResolvedVehicleId} bodyVehicleId={BodyVehicleId}",
                    resolvedVehicleId, bodyVehicleId);
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Input object invalid, vehicleId and petProfileId do not match."));
            }

            return UpdateNoteCore(input, resolvedVehicleId, "/api/vehicle/notes/update", true);
        }

        private IActionResult AddNoteCore(int vehicleId, int petProfileId, NoteRecordExportModel input, string routeLabel, bool isV2Route)
        {
            MarkContractUsage(routeLabel);
            _logger.LogInformation("Phase13 notes write route used: contract={Contract} operation=add", isV2Route ? "v2-profiles" : "legacy-v1");
            var resolvedVehicleId = ResolveVehicleIdAlias(vehicleId, petProfileId, isV2Route ? "/api/v2/profiles/notes/add" : routeLabel);
            if (resolvedVehicleId == -1)
            {
                _logger.LogWarning("Phase13 notes write conflict rejected on add: vehicleId={VehicleId} petProfileId={PetProfileId}", vehicleId, petProfileId);
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Input object invalid, vehicleId and petProfileId do not match."));
            }

            if (resolvedVehicleId == default)
            {
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Must provide a valid vehicle id"));
            }
            if (string.IsNullOrWhiteSpace(input.Description) ||
                string.IsNullOrWhiteSpace(input.NoteText))
            {
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Input object invalid, Description and NoteText cannot be empty."));
            }
            if (input.Files == null)
            {
                input.Files = new List<UploadedFiles>();
            }
            if (input.ExtraFields == null)
            {
                input.ExtraFields = new List<ExtraField>();
            }
            try
            {
                var note = new Note()
                {
                    VehicleId = resolvedVehicleId,
                    Description = input.Description,
                    NoteText = input.NoteText,
                    Pinned = string.IsNullOrWhiteSpace(input.Pinned) ? false : bool.Parse(input.Pinned),
                    ExtraFields = input.ExtraFields,
                    Files = input.Files,
                    Tags = string.IsNullOrWhiteSpace(input.Tags) ? new List<string>() : input.Tags.Split(' ').Distinct().ToList()
                };
                _noteDataAccess.SaveNoteToVehicle(note);
                _eventLogic.PublishEvent(WebHookPayload.FromNoteRecord(note, "note.add.api", User.Identity?.Name ?? string.Empty));
                return Json(OperationResponse.Succeed("Note Added", new { recordId = note.Id }));
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(OperationResponse.Failed(ex.Message));
            }
        }
        private IActionResult DeleteNoteCore(int id, int expectedVehicleId, string routeLabel, bool isV2Route)
        {
            MarkContractUsage(routeLabel);
            _logger.LogInformation("Phase13 notes write route used: contract={Contract} operation=delete", isV2Route ? "v2-profiles" : "legacy-v1");
            var existingRecord = _noteDataAccess.GetNoteById(id);
            if (existingRecord == null || existingRecord.Id == default)
            {
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Invalid Record Id"));
            }
            if (expectedVehicleId != default &&
                existingRecord.VehicleId != expectedVehicleId &&
                _config.GetWriteV2StrictIdConflictReject())
            {
                _logger.LogWarning("Phase13 notes write conflict rejected on delete target mismatch: expectedVehicleId={ExpectedVehicleId} recordVehicleId={RecordVehicleId}",
                    expectedVehicleId, existingRecord.VehicleId);
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Input object invalid, vehicleId and petProfileId do not match."));
            }
            //security check.
            if (!_userLogic.UserCanEditVehicle(GetUserID(), existingRecord.VehicleId, HouseholdPermission.Delete))
            {
                Response.StatusCode = 401;
                return Json(OperationResponse.Failed("Access Denied, you don't have access to this vehicle."));
            }
            var result = _noteDataAccess.DeleteNoteById(existingRecord.Id);
            if (result)
            {
                _eventLogic.PublishEvent(WebHookPayload.FromNoteRecord(existingRecord, "note.delete.api", User.Identity?.Name ?? string.Empty));
            }
            return Json(OperationResponse.Conditional(result, "Note Deleted"));
        }

        private IActionResult UpdateNoteCore(NoteRecordExportModel input, int expectedVehicleId, string routeLabel, bool isV2Route)
        {
            MarkContractUsage(routeLabel);
            _logger.LogInformation("Phase13 notes write route used: contract={Contract} operation=update", isV2Route ? "v2-profiles" : "legacy-v1");
            if (string.IsNullOrWhiteSpace(input.Id) ||
                string.IsNullOrWhiteSpace(input.Description) ||
                string.IsNullOrWhiteSpace(input.NoteText))
            {
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Input object invalid, Id, Description, and NoteText cannot be empty."));
            }
            if (input.Files == null)
            {
                input.Files = new List<UploadedFiles>();
            }
            if (input.ExtraFields == null)
            {
                input.ExtraFields = new List<ExtraField>();
            }
            try
            {
                //retrieve existing record
                var existingRecord = _noteDataAccess.GetNoteById(int.Parse(input.Id));
                if (existingRecord != null && existingRecord.Id == int.Parse(input.Id))
                {
                    if (expectedVehicleId != default &&
                        existingRecord.VehicleId != expectedVehicleId &&
                        _config.GetWriteV2StrictIdConflictReject())
                    {
                        _logger.LogWarning("Phase13 notes write conflict rejected on update target mismatch: expectedVehicleId={ExpectedVehicleId} recordVehicleId={RecordVehicleId}",
                            expectedVehicleId, existingRecord.VehicleId);
                        Response.StatusCode = 400;
                        return Json(OperationResponse.Failed("Input object invalid, vehicleId and petProfileId do not match."));
                    }
                    //check if user has access to the vehicleId
                    if (!_userLogic.UserCanEditVehicle(GetUserID(), existingRecord.VehicleId, HouseholdPermission.Edit))
                    {
                        Response.StatusCode = 401;
                        return Json(OperationResponse.Failed("Access Denied, you don't have access to this vehicle."));
                    }
                    existingRecord.Description = input.Description;
                    existingRecord.NoteText = input.NoteText;
                    existingRecord.Pinned = string.IsNullOrWhiteSpace(input.Pinned) ? false : bool.Parse(input.Pinned);
                    existingRecord.Files = input.Files;
                    existingRecord.ExtraFields = input.ExtraFields;
                    existingRecord.Tags = string.IsNullOrWhiteSpace(input.Tags) ? new List<string>() : input.Tags.Split(' ').Distinct().ToList();
                    _noteDataAccess.SaveNoteToVehicle(existingRecord);
                    _eventLogic.PublishEvent(WebHookPayload.FromNoteRecord(existingRecord, "note.update.api", User.Identity?.Name ?? string.Empty));
                }
                else
                {
                    Response.StatusCode = 400;
                    return Json(OperationResponse.Failed("Invalid Record Id"));
                }
                return Json(OperationResponse.Succeed("Note Updated"));
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(OperationResponse.Failed(ex.Message));
            }
        }

        private bool IsNotesWriteV2Enabled()
        {
            return _config.GetWriteV2RoutesEnabled() && _config.GetWriteV2FamilyNotesEnabled();
        }

        private IActionResult NotesWriteV2Disabled(string operation)
        {
            _logger.LogInformation("Phase13 notes v2 write disabled by feature flags: operation={Operation}", operation);
            Response.StatusCode = 404;
            return Json(OperationResponse.Failed("Endpoint not found."));
        }

        private bool IsLegacyIdAliasRejectedOnV2(int vehicleId, int petProfileId)
        {
            return vehicleId != default &&
                petProfileId == default &&
                !_config.GetWriteV2AliasParsingEnabled();
        }

        private IActionResult NotesWriteV2AliasDisabled(string operation)
        {
            _logger.LogInformation("Phase13 notes v2 write legacy vehicleId alias rejected by feature flag: operation={Operation}", operation);
            Response.StatusCode = 400;
            return Json(OperationResponse.Failed("Input object invalid, vehicleId alias is disabled for v2 writes."));
        }
    }
}
