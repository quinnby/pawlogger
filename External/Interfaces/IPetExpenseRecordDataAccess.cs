using CarCareTracker.Models;

namespace CarCareTracker.External.Interfaces
{
    /// <summary>Phase 6 – Data access interface for centralized pet expense records.</summary>
    public interface IPetExpenseRecordDataAccess
    {
        List<PetExpenseRecord> GetPetExpenseRecordsByVehicleId(int vehicleId);
        PetExpenseRecord GetPetExpenseRecordById(int petExpenseRecordId);
        bool DeletePetExpenseRecordById(int petExpenseRecordId);
        bool SavePetExpenseRecord(PetExpenseRecord petExpenseRecord);
        bool DeleteAllPetExpenseRecordsByVehicleId(int vehicleId);
    }
}
