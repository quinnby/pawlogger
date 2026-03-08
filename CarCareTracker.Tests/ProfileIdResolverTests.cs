using CarCareTracker.Helper;
using Xunit;

namespace CarCareTracker.Tests;

public class ProfileIdResolverTests
{
    [Fact]
    public void TryResolveVehicleIdAlias_WhenBothIdsConflict_ReturnsFalse()
    {
        var success = ProfileIdResolver.TryResolveVehicleIdAlias(11, 22, out var resolvedVehicleId, out var source);

        Assert.False(success);
        Assert.Equal(0, resolvedVehicleId);
        Assert.Equal(ProfileIdResolutionSource.None, source);
    }

    [Fact]
    public void TryResolveVehicleIdAlias_WhenVehicleIdIsProvided_UsesVehicleId()
    {
        var success = ProfileIdResolver.TryResolveVehicleIdAlias(11, 0, out var resolvedVehicleId, out var source);

        Assert.True(success);
        Assert.Equal(11, resolvedVehicleId);
        Assert.Equal(ProfileIdResolutionSource.VehicleId, source);
    }

    [Fact]
    public void TryResolveVehicleIdAlias_WhenPetProfileIdIsProvided_UsesPetProfileId()
    {
        var success = ProfileIdResolver.TryResolveVehicleIdAlias(0, 22, out var resolvedVehicleId, out var source);

        Assert.True(success);
        Assert.Equal(22, resolvedVehicleId);
        Assert.Equal(ProfileIdResolutionSource.PetProfileId, source);
    }
}
