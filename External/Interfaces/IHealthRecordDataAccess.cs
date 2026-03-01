using CarCareTracker.Models;

namespace CarCareTracker.External.Interfaces
{
    public interface IHealthRecordDataAccess
    {
        public List<HealthRecord> GetHealthRecordsByVehicleId(int vehicleId);
        public HealthRecord GetHealthRecordById(int healthRecordId);
        public bool DeleteHealthRecordById(int healthRecordId);
        public bool SaveHealthRecord(HealthRecord healthRecord);
        public bool DeleteAllHealthRecordsByVehicleId(int vehicleId);
    }
}
