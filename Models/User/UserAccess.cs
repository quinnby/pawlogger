using System.Text.Json.Serialization;

namespace CarCareTracker.Models
{
    public class UserVehicle
    {
        public int UserId { get; set; }
        public int VehicleId { get; set; }
        // Internal alias only; persistence compatibility stays on VehicleId.
        [JsonIgnore]
        public int PetProfileId
        {
            get => VehicleId;
            set => VehicleId = value;
        }
    }
    public class UserAccess
    {
        public UserVehicle Id { get; set; }
    }
}
