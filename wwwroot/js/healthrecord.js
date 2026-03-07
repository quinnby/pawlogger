function showAddHealthRecordModal() {
    $.get('/Vehicle/GetAddHealthRecordPartialView', function (data) {
        if (data) {
            $("#healthRecordModalContent").html(data);
            initDatePicker($('#healthRecordDate'));
            initTagSelector($("#healthRecordTag"));
            $('#healthRecordModal').modal('show');
        }
    });
}

// Phase 7 – Opens the health record modal pre-seeded with WeightCheck category.
// This ensures the weight measurement fields (healthWeightValue / healthWeightUnit)
// are visible from the moment the modal opens, preventing accidental saves under
// the wrong category (e.g. MiscellaneousCare) that would bypass the CurrentWeight sync.
function showAddWeightCheckModal() {
    $.get('/Vehicle/GetAddHealthRecordPartialView?category=7', function (data) {
        if (data) {
            $("#healthRecordModalContent").html(data);
            initDatePicker($('#healthRecordDate'));
            initTagSelector($("#healthRecordTag"));
            $('#healthRecordModal').modal('show');
        }
    });
}

function showEditHealthRecordModal(healthRecordId, nocache) {
    if (!nocache) {
        var existingContent = $("#healthRecordModalContent").html();
        if (existingContent.trim() != '') {
            var existingId = getHealthRecordModelData().id;
            if (existingId == healthRecordId && $('[data-changed=true]').length > 0) {
                $('#healthRecordModal').modal('show');
                $('.cached-banner').show();
                return;
            }
        }
    }
    $.get(`/Vehicle/GetHealthRecordForEditById?healthRecordId=${healthRecordId}`, function (data) {
        if (data) {
            $("#healthRecordModalContent").html(data);
            initDatePicker($('#healthRecordDate'));
            // also init follow-up date picker if visible
            if ($("#healthRecordFollowUpRequired").is(":checked")) {
                initDatePicker($('#healthRecordFollowUpDate'), false, true);
            }
            initTagSelector($("#healthRecordTag"));
            $('#healthRecordModal').modal('show');
            bindModalInputChanges('healthRecordModal');
            $('#healthRecordModal').off('shown.bs.modal').on('shown.bs.modal', function () {
                if (getGlobalConfig().useMarkDown) {
                    toggleMarkDownOverlay("healthRecordNotes");
                }
            });
        }
    });
}

function hideAddHealthRecordModal() {
    $('#healthRecordModal').modal('hide');
}

function deleteHealthRecord(healthRecordId) {
    $("#workAroundInput").show();
    confirmDelete("Deleted Health Records cannot be restored.", (result) => {
        if (result.isConfirmed) {
            $.post(`/Vehicle/DeleteHealthRecordById?healthRecordId=${healthRecordId}`, function (data) {
                if (data.success) {
                    hideAddHealthRecordModal();
                    successToast("Health Record Deleted");
                    var vehicleId = GetVehicleId().vehicleId;
                    getVehicleHealthRecords(vehicleId);
                } else {
                    errorToast(data.message);
                    $("#workAroundInput").hide();
                }
            });
        } else {
            $("#workAroundInput").hide();
        }
    });
}

function saveHealthRecordToVehicle(isEdit) {
    var formValues = getAndValidateHealthRecordValues();
    if (formValues.hasError) {
        errorToast("Please check the form data");
        return;
    }
    $.post('/Vehicle/SaveHealthRecordToVehicleId', formValues, function (data) {
        if (data.success) {
            successToast(isEdit ? "Health Record Updated" : "Health Record Added");
            hideAddHealthRecordModal();
            saveScrollPosition();
            getVehicleHealthRecords(formValues.vehicleId);
        } else {
            errorToast(data.message);
        }
    });
}

function getAndValidateHealthRecordValues() {
    var healthDate        = $("#healthRecordDate").val();
    var healthCategory    = parseInt($("#healthRecordCategory").val());
    var healthTitle       = $("#healthRecordTitle").val();
    var healthDescription = $("#healthRecordDescription").val();
    var healthCost        = $("#healthRecordCost").val();
    var healthNotes       = $("#healthRecordNotes").val();
    var healthProvider    = $("#healthRecordProvider").val();
    var healthStatus      = parseInt($("#healthRecordStatus").val());
    var healthFollowUp    = $("#healthRecordFollowUpRequired").is(":checked");
    var healthFollowUpDate = healthFollowUp ? $("#healthRecordFollowUpDate").val() : "";
    var healthTags        = $("#healthRecordTag").val();
    var vehicleId         = GetVehicleId().vehicleId;
    var recordId          = getHealthRecordModelData().id;
    var extraFields       = getAndValidateExtraFields();

    var hasError = false;
    if (extraFields.hasError) {
        hasError = true;
    }
    if (healthDate.trim() == '') {
        hasError = true;
        $("#healthRecordDate").addClass("is-invalid");
    } else {
        $("#healthRecordDate").removeClass("is-invalid");
    }
    if (healthTitle.trim() == '') {
        hasError = true;
        $("#healthRecordTitle").addClass("is-invalid");
    } else {
        $("#healthRecordTitle").removeClass("is-invalid");
    }
    if (healthCost.trim() != '' && !isValidMoney(healthCost)) {
        hasError = true;
        $("#healthRecordCost").addClass("is-invalid");
    } else {
        $("#healthRecordCost").removeClass("is-invalid");
    }
    // Default empty cost to "0"
    if (healthCost.trim() == '') {
        healthCost = "0";
    }

    return {
        id:               recordId,
        hasError:         hasError,
        vehicleId:        vehicleId,
        date:             healthDate,
        category:         isNaN(healthCategory) ? 13 : healthCategory,
        title:            healthTitle,
        description:      healthDescription,
        cost:             healthCost,
        notes:            healthNotes,
        provider:         healthProvider,
        status:           isNaN(healthStatus) ? 1 : healthStatus,
        followUpRequired: healthFollowUp,
        followUpDate:     healthFollowUpDate,
        tags:             healthTags,
        files:            uploadedFiles,
        extraFields:      extraFields.extraFields,
        // Phase 7 fields
        weightValue:      globalParseFloat($("#healthWeightValue").val() || "0"),
        weightUnit:       $("#healthWeightUnit").val() || (getGlobalConfig().preferredWeightUnit || "lbs"),
        allergyType:      $("#healthAllergyType").val() || "",
        trigger:          $("#healthTrigger").val() || "",
        severity:         $("#healthSeverity").val() || "",
        reminderEnabled:  $("#healthReminderEnabled").is(":checked"),
        reminderDueDate:  $("#healthReminderDueDate").val() || ""
    };
}

// ============================================================
// Phase 7 – Weight Trend Chart
// ============================================================
function showWeightTrendChart() {
    var vehicleId = GetVehicleId().vehicleId;
    $.get('/Vehicle/GetWeightTrendChartPartialView?vehicleId=' + vehicleId, function (data) {
        if (data) {
            // Phase 3: weightTrendModal is registered at page level in Index.cshtml.
            // The old fallback to a tab-level duplicate was removed to prevent ID collisions.
            $('#weightTrendModalContent').html(data);
            $('#weightTrendModal').modal('show');
        }
    });
}

// ============================================================
// Phase 7 – Quick Health Observation
// ============================================================
function showAddQuickHealthNoteModal() {
    var vehicleId = GetVehicleId().vehicleId;
    $.get('/Vehicle/GetAddQuickHealthNotePartialView?vehicleId=' + vehicleId, function (data) {
        if (data) {
            $('#quickHealthNoteModalContent').html(data);
            $('#quickHealthNoteModal').modal('show');
        }
    });
}

function hideQuickHealthNoteModal() {
    $('#quickHealthNoteModal').modal('hide');
}

function saveQuickHealthNote() {
    var vehicleId = GetVehicleId().vehicleId;
    var date      = $('#quickNoteDate').val();
    var category  = parseInt($('#quickNoteCategory').val());
    var title     = $('#quickNoteTitle').val().trim();
    var notes     = $('#quickNoteNotes').val();
    var status    = parseInt($('#quickNoteStatus').val());

    if (!date || !title) {
        errorToast('Please enter at least a date and observation text.');
        return;
    }

    var payload = {
        id: 0,
        vehicleId: vehicleId,
        date: date,
        category: isNaN(category) ? 3 : category,
        title: title,
        description: '',
        cost: '0',
        notes: notes,
        provider: '',
        status: isNaN(status) ? 2 : status,
        followUpRequired: false,
        followUpDate: '',
        tags: [],
        files: [],
        extraFields: [],
        weightValue: 0,
        weightUnit: 'lbs',
        allergyType: '',
        trigger: '',
        severity: '',
        reminderEnabled: false,
        reminderDueDate: ''
    };

    $.post('/Vehicle/SaveHealthRecordToVehicleId', payload, function (data) {
        if (data.success) {
            successToast('Observation saved');
            hideQuickHealthNoteModal();
            getVehicleHealthRecords(vehicleId);
        } else {
            errorToast(data.message);
        }
    });
}

// ============================================================
// Phase 7 – Pet Health Summary
// ============================================================
function showPetSummaryModal() {
    var vehicleId = GetVehicleId().vehicleId;
    $.get('/Vehicle/GetPetSummaryData?vehicleId=' + vehicleId, function (data) {
        if (data) {
            $('#petSummaryModalContent').html(data);
            $('#petSummaryModal').modal('show');
        }
    });
}

// ============================================================
// Phase 7 – PDF Export
// ============================================================
function exportPetSummaryPdf() {
    var vehicleId = GetVehicleId().vehicleId;
    window.location.href = '/Vehicle/ExportPetSummaryPdf?vehicleId=' + vehicleId;
}

