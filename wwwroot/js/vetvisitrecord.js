function showAddVetVisitRecordModal() {
    $.get('/Vehicle/GetAddVetVisitRecordPartialView', function (data) {
        if (data) {
            $("#vetVisitRecordModalContent").html(data);
            initDatePicker($('#vetVisitDate'));
            initTagSelector($("#vetVisitTag"));
            $('#vetVisitRecordModal').modal('show');
        }
    });
}

function showEditVetVisitRecordModal(vetVisitRecordId, nocache) {
    $.get(`/Vehicle/GetVetVisitRecordForEditById?vetVisitRecordId=${vetVisitRecordId}`, function (data) {
        if (data) {
            $("#vetVisitRecordModalContent").html(data);
            initDatePicker($('#vetVisitDate'));
            if ($("#vetVisitFollowUpNeeded").is(":checked")) {
                initDatePicker($('#vetVisitFollowUpDate'));
            }
            initTagSelector($("#vetVisitTag"));
            $('#vetVisitRecordModal').modal('show');
            bindModalInputChanges('vetVisitRecordModal');
            $('#vetVisitRecordModal').off('shown.bs.modal').on('shown.bs.modal', function () {
                if (getGlobalConfig().useMarkDown) {
                    toggleMarkDownOverlay("vetVisitNotes");
                }
            });
        }
    });
}

function hideAddVetVisitRecordModal() {
    $('#vetVisitRecordModal').modal('hide');
}

function deleteVetVisitRecord(vetVisitRecordId) {
    $("#workAroundInput").show();
    confirmDelete("Deleted Vet Visit Records cannot be restored.", (result) => {
        if (result.isConfirmed) {
            $.post(`/Vehicle/DeleteVetVisitRecordById?vetVisitRecordId=${vetVisitRecordId}`, function (data) {
                if (data.success) {
                    hideAddVetVisitRecordModal();
                    successToast("Vet Visit Record Deleted");
                    var vehicleId = GetVehicleId().vehicleId;
                    getVehicleVetVisitRecords(vehicleId);
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

function saveVetVisitRecordToVehicle(isEdit) {
    var formValues = getAndValidateVetVisitRecordValues();
    if (formValues.hasError) {
        errorToast("Please check the form data");
        return;
    }
    $.post('/Vehicle/SaveVetVisitRecordToVehicleId', formValues, function (data) {
        if (data.success) {
            successToast(isEdit ? "Vet Visit Updated" : "Vet Visit Added");
            hideAddVetVisitRecordModal();
            saveScrollPosition();
            getVehicleVetVisitRecords(formValues.vehicleId);
        } else {
            errorToast(data.message);
        }
    });
}

function getAndValidateVetVisitRecordValues() {
    var date             = $("#vetVisitDate").val();
    var clinic           = $("#vetVisitClinic").val();
    var veterinarian     = $("#vetVisitVeterinarian").val();
    var reason           = $("#vetVisitReason").val();
    var symptoms         = $("#vetVisitSymptoms").val();
    var diagnosis        = $("#vetVisitDiagnosis").val();
    var treatment        = $("#vetVisitTreatment").val();
    var followUpNeeded   = $("#vetVisitFollowUpNeeded").is(":checked");
    var followUpDate     = followUpNeeded ? $("#vetVisitFollowUpDate").val() : "";
    // Phase 5.1 – only collect reminder flag when follow-up is enabled
    var reminderEnabled  = followUpNeeded && $("#vetVisitReminderEnabled").is(":checked");
    var cost             = $("#vetVisitCost").val();
    var notes            = $("#vetVisitNotes").val();
    var tags             = $("#vetVisitTag").val();
    var vehicleId        = GetVehicleId().vehicleId;
    var recordId         = getVetVisitRecordModelData().id;
    var extraFields      = getAndValidateExtraFields();

    var hasError = false;
    if (extraFields.hasError) { hasError = true; }

    if (date.trim() == '') {
        hasError = true;
        $("#vetVisitDate").addClass("is-invalid");
    } else {
        $("#vetVisitDate").removeClass("is-invalid");
    }
    if (reason.trim() == '') {
        hasError = true;
        $("#vetVisitReason").addClass("is-invalid");
    } else {
        $("#vetVisitReason").removeClass("is-invalid");
    }
    if (cost.trim() != '' && !isValidMoney(cost)) {
        hasError = true;
        $("#vetVisitCost").addClass("is-invalid");
    } else {
        $("#vetVisitCost").removeClass("is-invalid");
    }

    return {
        hasError: hasError,
        id: recordId,
        vehicleId: vehicleId,
        date: date,
        clinic: clinic,
        veterinarian: veterinarian,
        reasonForVisit: reason,
        symptomsReported: symptoms,
        diagnosis: diagnosis,
        treatmentProvided: treatment,
        followUpNeeded: followUpNeeded,
        followUpDate: followUpDate,
        reminderEnabled: reminderEnabled,
        cost: cost == '' ? 0 : cost,
        notes: notes,
        tags: tags,
        extraFields: extraFields.extraFields
    };
}
