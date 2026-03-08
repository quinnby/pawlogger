namespace CarCareTracker.Helper
{
    public enum ProfileIdResolutionSource
    {
        None = 0,
        VehicleId = 1,
        PetProfileId = 2
    }

    public static class ProfileIdResolver
    {
        public static bool TryResolveVehicleIdAlias(
            int vehicleId,
            int petProfileId,
            out int resolvedVehicleId,
            out ProfileIdResolutionSource source)
        {
            resolvedVehicleId = default;
            source = ProfileIdResolutionSource.None;

            if (vehicleId != default && petProfileId != default && vehicleId != petProfileId)
            {
                return false;
            }

            if (vehicleId != default)
            {
                resolvedVehicleId = vehicleId;
                source = ProfileIdResolutionSource.VehicleId;
                return true;
            }

            if (petProfileId != default)
            {
                resolvedVehicleId = petProfileId;
                source = ProfileIdResolutionSource.PetProfileId;
                return true;
            }

            return true;
        }
    }
}
