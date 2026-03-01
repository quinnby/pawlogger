using CarCareTracker.Models;

namespace CarCareTracker.External.Interfaces
{
    // Phase 3 – Note: all vehicleId parameters here represent the PetId.
    // The VehicleId naming is kept for DB/infrastructure compatibility;
    // do not add new methods using vehicleId without this context in mind.
    public interface IHealthRecordDataAccess
    {
        public List<HealthRecord> GetHealthRecordsByVehicleId(int vehicleId);
        public HealthRecord GetHealthRecordById(int healthRecordId);
        public bool DeleteHealthRecordById(int healthRecordId);
        public bool SaveHealthRecord(HealthRecord healthRecord);
        public bool DeleteAllHealthRecordsByVehicleId(int vehicleId);
    }
}
