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
                initDatePicker($('#healthRecordFollowUpDate'));
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
        extraFields:      extraFields.extraFields
    };
}
