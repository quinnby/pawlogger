// Phase 6 – Centralized pet expense record JavaScript
// Follows the vetvisitrecord.js / medicationrecord.js pattern.
// Phase 7 – expense modal uses a health record selector instead of a raw number input.

/**
 * Populates the #expenseLinkedHealthRecordSelector dropdown with HealthRecords for the
 * current pet, then pre-selects the given preselectedId (0 = "No linked record").
 * Called each time the expense modal opens so the list stays fresh.
 */
function loadHealthRecordSelectorOptions(vehicleId, preselectedId) {
    var $sel = $('#expenseLinkedHealthRecordSelector');
    $sel.prop('disabled', true);
    $sel.empty().append('<option value="0">Loading health records…</option>');
    $.get('/Vehicle/GetHealthRecordListForExpenseSelector?vehicleId=' + vehicleId, function (records) {
        $sel.empty();
        $sel.append('<option value="0">— No linked record —</option>');
        if (records && records.length > 0) {
            $.each(records, function (i, r) {
                var label = r.date + ' – ' + r.title + ' (' + r.category + ')';
                var $opt = $('<option></option>').val(r.id).text(label);
                if (r.id === preselectedId) {
                    $opt.prop('selected', true);
                }
                $sel.append($opt);
            });
        } else {
            $sel.append('<option disabled>No health records found</option>');
        }
        // Sync hidden field to selector value now that the list is populated
        $('#expenseLinkedHealthRecordId').val($sel.val() || '0');
        $sel.prop('disabled', false);
    }).fail(function () {
        $sel.empty().append('<option value="0">Unable to load health records</option>');
        $sel.prop('disabled', false);
    });
}

function showAddPetExpenseRecordModal() {
    $.get('/Vehicle/GetAddPetExpenseRecordPartialView', function (data) {
        if (data) {
            $("#petExpenseRecordModalContent").html(data);
            initDatePicker($('#expenseDate'));
            initTagSelector($("#expenseTag"));
            // Phase 7 – populate health record selector (no pre-selected record for new expense)
            var vehicleId = GetVehicleId().vehicleId;
            loadHealthRecordSelectorOptions(vehicleId, 0);
            $('#petExpenseRecordModal').modal('show');
        }
    });
}

function showEditPetExpenseRecordModal(petExpenseRecordId, nocache) {
    $.get(`/Vehicle/GetPetExpenseRecordForEditById?petExpenseRecordId=${petExpenseRecordId}`, function (data) {
        if (data) {
            $("#petExpenseRecordModalContent").html(data);
            initDatePicker($('#expenseDate'));
            initTagSelector($("#expenseTag"));
            // Phase 7 – populate health record selector, pre-selecting the existing linked record
            var vehicleId = GetVehicleId().vehicleId;
            var preselectedId = parseInt($('#expenseLinkedHealthRecordId').val() || '0', 10);
            loadHealthRecordSelectorOptions(vehicleId, preselectedId);
            $('#petExpenseRecordModal').modal('show');
            bindModalInputChanges('petExpenseRecordModal');
            $('#petExpenseRecordModal').off('shown.bs.modal').on('shown.bs.modal', function () {
                if (getGlobalConfig().useMarkDown) {
                    toggleMarkDownOverlay("expenseNotes");
                }
            });
        }
    });
}

function hideAddPetExpenseRecordModal() {
    $('#petExpenseRecordModal').modal('hide');
}

function deletePetExpenseRecord(petExpenseRecordId) {
    $("#workAroundInput").show();
    confirmDelete("Deleted Expense Records cannot be restored.", (result) => {
        if (result.isConfirmed) {
            $.post(`/Vehicle/DeletePetExpenseRecordById?petExpenseRecordId=${petExpenseRecordId}`, function (data) {
                if (data.success) {
                    hideAddPetExpenseRecordModal();
                    successToast("Expense Record Deleted");
                    var vehicleId = GetVehicleId().vehicleId;
                    getVehiclePetExpenseRecords(vehicleId);
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

function savePetExpenseRecordToVehicle(isEdit) {
    var formValues = getAndValidatePetExpenseRecordValues();
    if (formValues.hasError) {
        errorToast("Please check the form data");
        return;
    }
    $.post('/Vehicle/SavePetExpenseRecordToVehicleId', formValues, function (data) {
        if (data.success) {
            successToast(isEdit ? "Expense Updated" : "Expense Added");
            hideAddPetExpenseRecordModal();
            saveScrollPosition();
            getVehiclePetExpenseRecords(formValues.vehicleId);
        } else {
            errorToast(data.message);
        }
    });
}

function getAndValidatePetExpenseRecordValues() {
    var date        = $("#expenseDate").val();
    var category    = parseInt($("#expenseCategory").val(), 10);
    var description = $("#expenseDescription").val();
    var vendor      = $("#expenseVendor").val();
    var cost        = $("#expenseCost").val();
    var isRecurring = $("#expenseIsRecurring").is(":checked");
    var linkedId    = parseInt($("#expenseLinkedHealthRecordId").val() || "0", 10);
    var notes       = $("#expenseNotes").val();
    var tags        = $("#expenseTag").val();
    var vehicleId   = GetVehicleId().vehicleId;
    var recordId    = getPetExpenseRecordModelData().id;
    var extraFields = getAndValidateExtraFields();

    var hasError = false;
    if (extraFields.hasError) { hasError = true; }

    if (date.trim() === '') {
        hasError = true;
        $("#expenseDate").addClass("is-invalid");
    } else {
        $("#expenseDate").removeClass("is-invalid");
    }
    if (description.trim() === '') {
        hasError = true;
        $("#expenseDescription").addClass("is-invalid");
    } else {
        $("#expenseDescription").removeClass("is-invalid");
    }
    if (cost.trim() !== '' && !isValidMoney(cost)) {
        hasError = true;
        $("#expenseCost").addClass("is-invalid");
    } else {
        $("#expenseCost").removeClass("is-invalid");
    }

    return {
        hasError: hasError,
        id: recordId,
        vehicleId: vehicleId,
        date: date,
        category: category,
        description: description,
        vendor: vendor,
        cost: cost === '' ? 0 : cost,
        isRecurring: isRecurring,
        linkedHealthRecordId: isNaN(linkedId) ? 0 : linkedId,
        notes: notes,
        tags: tags,
        extraFields: extraFields.extraFields
    };
}
