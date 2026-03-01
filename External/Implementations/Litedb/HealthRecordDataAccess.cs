using CarCareTracker.External.Interfaces;
using CarCareTracker.Models;
using CarCareTracker.Helper;
using LiteDB;

namespace CarCareTracker.External.Implementations
{
    public class HealthRecordDataAccess : IHealthRecordDataAccess
    {
        private ILiteDBHelper _liteDB { get; set; }
        private static string tableName = "healthrecords";

        public HealthRecordDataAccess(ILiteDBHelper liteDB)
        {
            _liteDB = liteDB;
        }

        public List<HealthRecord> GetHealthRecordsByVehicleId(int vehicleId)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<HealthRecord>(tableName);
            var records = table.Find(Query.EQ(nameof(HealthRecord.VehicleId), vehicleId));
            return records.ToList() ?? new List<HealthRecord>();
        }

        public HealthRecord GetHealthRecordById(int healthRecordId)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<HealthRecord>(tableName);
            return table.FindById(healthRecordId);
        }

        public bool DeleteHealthRecordById(int healthRecordId)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<HealthRecord>(tableName);
            table.Delete(healthRecordId);
            db.Checkpoint();
            return true;
        }

        public bool SaveHealthRecord(HealthRecord healthRecord)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<HealthRecord>(tableName);
            table.Upsert(healthRecord);
            db.Checkpoint();
            return true;
        }

        public bool DeleteAllHealthRecordsByVehicleId(int vehicleId)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<HealthRecord>(tableName);
            table.DeleteMany(Query.EQ(nameof(HealthRecord.VehicleId), vehicleId));
            db.Checkpoint();
            return true;
        }
    }
}
