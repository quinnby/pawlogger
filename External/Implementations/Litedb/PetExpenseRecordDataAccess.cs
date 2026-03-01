using CarCareTracker.External.Interfaces;
using CarCareTracker.Models;
using CarCareTracker.Helper;
using LiteDB;

namespace CarCareTracker.External.Implementations
{
    /// <summary>Phase 6 – LiteDB data access for centralized pet expense records.</summary>
    public class PetExpenseRecordDataAccess : IPetExpenseRecordDataAccess
    {
        private ILiteDBHelper _liteDB { get; set; }
        private static string tableName = "petexpenserecords";

        public PetExpenseRecordDataAccess(ILiteDBHelper liteDB)
        {
            _liteDB = liteDB;
        }

        public List<PetExpenseRecord> GetPetExpenseRecordsByVehicleId(int vehicleId)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<PetExpenseRecord>(tableName);
            var records = table.Find(Query.EQ(nameof(PetExpenseRecord.VehicleId), vehicleId));
            return records.ToList() ?? new List<PetExpenseRecord>();
        }

        public PetExpenseRecord GetPetExpenseRecordById(int petExpenseRecordId)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<PetExpenseRecord>(tableName);
            return table.FindById(petExpenseRecordId);
        }

        public bool DeletePetExpenseRecordById(int petExpenseRecordId)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<PetExpenseRecord>(tableName);
            table.Delete(petExpenseRecordId);
            db.Checkpoint();
            return true;
        }

        public bool SavePetExpenseRecord(PetExpenseRecord petExpenseRecord)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<PetExpenseRecord>(tableName);
            table.Upsert(petExpenseRecord);
            db.Checkpoint();
            return true;
        }

        public bool DeleteAllPetExpenseRecordsByVehicleId(int vehicleId)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<PetExpenseRecord>(tableName);
            table.DeleteMany(Query.EQ(nameof(PetExpenseRecord.VehicleId), vehicleId));
            db.Checkpoint();
            return true;
        }
    }
}
