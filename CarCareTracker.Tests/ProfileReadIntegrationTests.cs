using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CarCareTracker.Helper;
using CarCareTracker.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CarCareTracker.Tests;

[CollectionDefinition(nameof(ProfileReadIntegrationCollection), DisableParallelization = true)]
public sealed class ProfileReadIntegrationCollection : ICollectionFixture<ProfileReadIntegrationFixture>
{
}

[Collection(nameof(ProfileReadIntegrationCollection))]
public class ProfileReadIntegrationTests
{
    private readonly ProfileReadIntegrationFixture _fixture;

    public ProfileReadIntegrationTests(ProfileReadIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    public static IEnumerable<object[]> Phase8Segments()
    {
        yield return new object[] { "servicerecords" };
        yield return new object[] { "gasrecords" };
        yield return new object[] { "reminders" };
        yield return new object[] { "odometerrecords" };
    }

    public static IEnumerable<object[]> Phase9Segments()
    {
        yield return new object[] { "planrecords" };
        yield return new object[] { "taxrecords" };
        yield return new object[] { "repairrecords" };
    }

    public static IEnumerable<object[]> Phase10Segments()
    {
        yield return new object[] { "notes" };
        yield return new object[] { "upgraderecords" };
        yield return new object[] { "equipmentrecords" };
        yield return new object[] { "supplyrecords" };
    }

    public static IEnumerable<object[]> Phase11Segments()
    {
        yield return new object[] { "healthrecords" };
        yield return new object[] { "vetvisitrecords" };
        yield return new object[] { "vaccinationrecords" };
        yield return new object[] { "medicationrecords" };
        yield return new object[] { "licensingrecords" };
        yield return new object[] { "petexpenserecords" };
    }

    [Fact]
    public async Task Unauthenticated_Request_ToLegacyReadRoute_ReturnsUnauthorizedWithBasicChallenge()
    {
        var response = await _fixture.UnauthenticatedClient.GetAsync("/api/vehicle/servicerecords?vehicleId=100");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, header =>
            string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Authenticated_Request_WithoutVehicleAccess_IsDenied()
    {
        var response = await _fixture.AuthenticatedClient.GetAsync("/api/vehicle/servicerecords?vehicleId=101");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Error/Unauthorized", response.Headers.Location?.ToString());
    }

    [Theory]
    [MemberData(nameof(Phase8Segments))]
    public async Task Phase8_SingleReadRoutes_LegacyV2AliasAndHeaders_Work(string segment)
    {
        var legacyVehicle = await _fixture.AuthenticatedClient.GetAsync($"/api/vehicle/{segment}?vehicleId=100");
        Assert.Equal(HttpStatusCode.OK, legacyVehicle.StatusCode);
        AssertHeader(legacyVehicle, "X-PawLogger-Legacy-Route", "true");
        AssertHeader(legacyVehicle, "X-PawLogger-Api-Contract", "legacy-vehicle-v1");
        await AssertJsonArrayResponse(legacyVehicle);

        var legacyAlias = await _fixture.AuthenticatedClient.GetAsync($"/api/vehicle/{segment}?petProfileId=100");
        Assert.Equal(HttpStatusCode.OK, legacyAlias.StatusCode);
        AssertHeader(legacyAlias, "X-PawLogger-Alias-Id", "petProfileId");
        await AssertJsonArrayResponse(legacyAlias);

        var v2Pet = await _fixture.AuthenticatedClient.GetAsync($"/api/v2/profiles/{segment}?petProfileId=100");
        Assert.Equal(HttpStatusCode.OK, v2Pet.StatusCode);
        AssertHeader(v2Pet, "X-PawLogger-Api-Contract", "v2-profiles-shadow");
        await AssertJsonArrayResponse(v2Pet);

        var v2Vehicle = await _fixture.AuthenticatedClient.GetAsync($"/api/v2/profiles/{segment}?vehicleId=100");
        Assert.Equal(HttpStatusCode.OK, v2Vehicle.StatusCode);
        AssertHeader(v2Vehicle, "X-PawLogger-Legacy-Id", "vehicleId");
        await AssertJsonArrayResponse(v2Vehicle);
    }

    [Fact]
    public async Task Phase8_LatestOdometer_LegacyV2AliasAndHeaders_Work()
    {
        var legacyVehicle = await _fixture.AuthenticatedClient.GetAsync("/api/vehicle/odometerrecords/latest?vehicleId=100");
        Assert.Equal(HttpStatusCode.OK, legacyVehicle.StatusCode);
        AssertHeader(legacyVehicle, "X-PawLogger-Legacy-Route", "true");
        await AssertJsonNumberResponse(legacyVehicle);

        var legacyAlias = await _fixture.AuthenticatedClient.GetAsync("/api/vehicle/odometerrecords/latest?petProfileId=100");
        Assert.Equal(HttpStatusCode.OK, legacyAlias.StatusCode);
        AssertHeader(legacyAlias, "X-PawLogger-Alias-Id", "petProfileId");
        await AssertJsonNumberResponse(legacyAlias);

        var v2Pet = await _fixture.AuthenticatedClient.GetAsync("/api/v2/profiles/odometerrecords/latest?petProfileId=100");
        Assert.Equal(HttpStatusCode.OK, v2Pet.StatusCode);
        AssertHeader(v2Pet, "X-PawLogger-Api-Contract", "v2-profiles-shadow");
        await AssertJsonNumberResponse(v2Pet);

        var v2Vehicle = await _fixture.AuthenticatedClient.GetAsync("/api/v2/profiles/odometerrecords/latest?vehicleId=100");
        Assert.Equal(HttpStatusCode.OK, v2Vehicle.StatusCode);
        AssertHeader(v2Vehicle, "X-PawLogger-Legacy-Id", "vehicleId");
        await AssertJsonNumberResponse(v2Vehicle);
    }

    [Theory]
    [MemberData(nameof(Phase8Segments))]
    public async Task Phase8_AllReadRoutes_LegacyAndV2_WorkWithContractHeaders(string segment)
    {
        var legacy = await _fixture.AuthenticatedClient.GetAsync($"/api/vehicle/{segment}/all");
        Assert.Equal(HttpStatusCode.OK, legacy.StatusCode);
        AssertHeader(legacy, "X-PawLogger-Api-Contract", "legacy-vehicle-v1");
        await AssertJsonArrayResponse(legacy);

        var v2 = await _fixture.AuthenticatedClient.GetAsync($"/api/v2/profiles/{segment}/all");
        Assert.Equal(HttpStatusCode.OK, v2.StatusCode);
        AssertHeader(v2, "X-PawLogger-Api-Contract", "v2-profiles-shadow");
        await AssertJsonArrayResponse(v2);
    }

    [Theory]
    [InlineData("/api/vehicle/servicerecords")]
    [InlineData("/api/vehicle/gasrecords")]
    [InlineData("/api/vehicle/reminders")]
    [InlineData("/api/vehicle/odometerrecords")]
    [InlineData("/api/vehicle/odometerrecords/latest")]
    [InlineData("/api/v2/profiles/servicerecords")]
    [InlineData("/api/v2/profiles/gasrecords")]
    [InlineData("/api/v2/profiles/reminders")]
    [InlineData("/api/v2/profiles/odometerrecords")]
    [InlineData("/api/v2/profiles/odometerrecords/latest")]
    public async Task Phase8_ConflictingIds_ReturnBadRequest(string path)
    {
        var response = await _fixture.AuthenticatedClient.GetAsync($"{path}?vehicleId=100&petProfileId=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("vehicleId and petProfileId do not match", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(Phase9Segments))]
    public async Task Phase9_SingleReadRoutes_LegacyV2AliasAndHeaders_Work(string segment)
    {
        var legacyVehicle = await _fixture.AuthenticatedClient.GetAsync($"/api/vehicle/{segment}?vehicleId=100");
        Assert.Equal(HttpStatusCode.OK, legacyVehicle.StatusCode);
        AssertHeader(legacyVehicle, "X-PawLogger-Legacy-Route", "true");
        await AssertJsonArrayResponse(legacyVehicle);

        var legacyAlias = await _fixture.AuthenticatedClient.GetAsync($"/api/vehicle/{segment}?petProfileId=100");
        Assert.Equal(HttpStatusCode.OK, legacyAlias.StatusCode);
        AssertHeader(legacyAlias, "X-PawLogger-Alias-Id", "petProfileId");
        await AssertJsonArrayResponse(legacyAlias);

        var v2Pet = await _fixture.AuthenticatedClient.GetAsync($"/api/v2/profiles/{segment}?petProfileId=100");
        Assert.Equal(HttpStatusCode.OK, v2Pet.StatusCode);
        AssertHeader(v2Pet, "X-PawLogger-Api-Contract", "v2-profiles-shadow");
        await AssertJsonArrayResponse(v2Pet);

        var v2Vehicle = await _fixture.AuthenticatedClient.GetAsync($"/api/v2/profiles/{segment}?vehicleId=100");
        Assert.Equal(HttpStatusCode.OK, v2Vehicle.StatusCode);
        AssertHeader(v2Vehicle, "X-PawLogger-Legacy-Id", "vehicleId");
        await AssertJsonArrayResponse(v2Vehicle);
    }

    [Theory]
    [MemberData(nameof(Phase9Segments))]
    public async Task Phase9_AllReadRoutes_LegacyAndV2_WorkWithContractHeaders(string segment)
    {
        var legacy = await _fixture.AuthenticatedClient.GetAsync($"/api/vehicle/{segment}/all");
        Assert.Equal(HttpStatusCode.OK, legacy.StatusCode);
        AssertHeader(legacy, "X-PawLogger-Api-Contract", "legacy-vehicle-v1");
        await AssertJsonArrayResponse(legacy);

        var v2 = await _fixture.AuthenticatedClient.GetAsync($"/api/v2/profiles/{segment}/all");
        Assert.Equal(HttpStatusCode.OK, v2.StatusCode);
        AssertHeader(v2, "X-PawLogger-Api-Contract", "v2-profiles-shadow");
        await AssertJsonArrayResponse(v2);
    }

    [Theory]
    [InlineData("/api/vehicle/planrecords")]
    [InlineData("/api/vehicle/taxrecords")]
    [InlineData("/api/vehicle/repairrecords")]
    [InlineData("/api/v2/profiles/planrecords")]
    [InlineData("/api/v2/profiles/taxrecords")]
    [InlineData("/api/v2/profiles/repairrecords")]
    public async Task Phase9_ConflictingIds_ReturnBadRequest(string path)
    {
        var response = await _fixture.AuthenticatedClient.GetAsync($"{path}?vehicleId=100&petProfileId=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("vehicleId and petProfileId do not match", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(Phase10Segments))]
    public async Task Phase10_SingleReadRoutes_LegacyV2AliasAndHeaders_Work(string segment)
    {
        var legacyVehicle = await _fixture.AuthenticatedClient.GetAsync($"/api/vehicle/{segment}?vehicleId=100");
        Assert.Equal(HttpStatusCode.OK, legacyVehicle.StatusCode);
        AssertHeader(legacyVehicle, "X-PawLogger-Legacy-Route", "true");
        await AssertJsonArrayResponse(legacyVehicle);

        var legacyAlias = await _fixture.AuthenticatedClient.GetAsync($"/api/vehicle/{segment}?petProfileId=100");
        Assert.Equal(HttpStatusCode.OK, legacyAlias.StatusCode);
        AssertHeader(legacyAlias, "X-PawLogger-Alias-Id", "petProfileId");
        await AssertJsonArrayResponse(legacyAlias);

        var v2Pet = await _fixture.AuthenticatedClient.GetAsync($"/api/v2/profiles/{segment}?petProfileId=100");
        Assert.Equal(HttpStatusCode.OK, v2Pet.StatusCode);
        AssertHeader(v2Pet, "X-PawLogger-Api-Contract", "v2-profiles-shadow");
        await AssertJsonArrayResponse(v2Pet);

        var v2Vehicle = await _fixture.AuthenticatedClient.GetAsync($"/api/v2/profiles/{segment}?vehicleId=100");
        Assert.Equal(HttpStatusCode.OK, v2Vehicle.StatusCode);
        AssertHeader(v2Vehicle, "X-PawLogger-Legacy-Id", "vehicleId");
        await AssertJsonArrayResponse(v2Vehicle);
    }

    [Theory]
    [MemberData(nameof(Phase10Segments))]
    public async Task Phase10_AllReadRoutes_LegacyAndV2_WorkWithContractHeaders(string segment)
    {
        var legacy = await _fixture.AuthenticatedClient.GetAsync($"/api/vehicle/{segment}/all");
        Assert.Equal(HttpStatusCode.OK, legacy.StatusCode);
        AssertHeader(legacy, "X-PawLogger-Api-Contract", "legacy-vehicle-v1");
        await AssertJsonArrayResponse(legacy);

        var v2 = await _fixture.AuthenticatedClient.GetAsync($"/api/v2/profiles/{segment}/all");
        Assert.Equal(HttpStatusCode.OK, v2.StatusCode);
        AssertHeader(v2, "X-PawLogger-Api-Contract", "v2-profiles-shadow");
        await AssertJsonArrayResponse(v2);
    }

    [Theory]
    [InlineData("/api/vehicle/notes")]
    [InlineData("/api/vehicle/upgraderecords")]
    [InlineData("/api/vehicle/equipmentrecords")]
    [InlineData("/api/vehicle/supplyrecords")]
    [InlineData("/api/v2/profiles/notes")]
    [InlineData("/api/v2/profiles/upgraderecords")]
    [InlineData("/api/v2/profiles/equipmentrecords")]
    [InlineData("/api/v2/profiles/supplyrecords")]
    public async Task Phase10_ConflictingIds_ReturnBadRequest(string path)
    {
        var response = await _fixture.AuthenticatedClient.GetAsync($"{path}?vehicleId=100&petProfileId=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("vehicleId and petProfileId do not match", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(Phase11Segments))]
    public async Task Phase11_SingleReadRoutes_LegacyV2AliasAndHeaders_Work(string segment)
    {
        var legacyVehicle = await _fixture.AuthenticatedClient.GetAsync($"/api/vehicle/{segment}?vehicleId=100");
        Assert.Equal(HttpStatusCode.OK, legacyVehicle.StatusCode);
        AssertHeader(legacyVehicle, "X-PawLogger-Legacy-Route", "true");
        await AssertJsonArrayResponse(legacyVehicle);

        var legacyAlias = await _fixture.AuthenticatedClient.GetAsync($"/api/vehicle/{segment}?petProfileId=100");
        Assert.Equal(HttpStatusCode.OK, legacyAlias.StatusCode);
        AssertHeader(legacyAlias, "X-PawLogger-Alias-Id", "petProfileId");
        await AssertJsonArrayResponse(legacyAlias);

        var v2Pet = await _fixture.AuthenticatedClient.GetAsync($"/api/v2/profiles/{segment}?petProfileId=100");
        Assert.Equal(HttpStatusCode.OK, v2Pet.StatusCode);
        AssertHeader(v2Pet, "X-PawLogger-Api-Contract", "v2-profiles-shadow");
        await AssertJsonArrayResponse(v2Pet);

        var v2Vehicle = await _fixture.AuthenticatedClient.GetAsync($"/api/v2/profiles/{segment}?vehicleId=100");
        Assert.Equal(HttpStatusCode.OK, v2Vehicle.StatusCode);
        AssertHeader(v2Vehicle, "X-PawLogger-Legacy-Id", "vehicleId");
        await AssertJsonArrayResponse(v2Vehicle);
    }

    [Theory]
    [MemberData(nameof(Phase11Segments))]
    public async Task Phase11_AllReadRoutes_LegacyAndV2_WorkWithContractHeaders(string segment)
    {
        var legacy = await _fixture.AuthenticatedClient.GetAsync($"/api/vehicle/{segment}/all");
        Assert.Equal(HttpStatusCode.OK, legacy.StatusCode);
        AssertHeader(legacy, "X-PawLogger-Api-Contract", "legacy-vehicle-v1");
        await AssertJsonArrayResponse(legacy);

        var v2 = await _fixture.AuthenticatedClient.GetAsync($"/api/v2/profiles/{segment}/all");
        Assert.Equal(HttpStatusCode.OK, v2.StatusCode);
        AssertHeader(v2, "X-PawLogger-Api-Contract", "v2-profiles-shadow");
        await AssertJsonArrayResponse(v2);
    }

    [Theory]
    [InlineData("/api/vehicle/healthrecords")]
    [InlineData("/api/vehicle/vetvisitrecords")]
    [InlineData("/api/vehicle/vaccinationrecords")]
    [InlineData("/api/vehicle/medicationrecords")]
    [InlineData("/api/vehicle/licensingrecords")]
    [InlineData("/api/vehicle/petexpenserecords")]
    [InlineData("/api/v2/profiles/healthrecords")]
    [InlineData("/api/v2/profiles/vetvisitrecords")]
    [InlineData("/api/v2/profiles/vaccinationrecords")]
    [InlineData("/api/v2/profiles/medicationrecords")]
    [InlineData("/api/v2/profiles/licensingrecords")]
    [InlineData("/api/v2/profiles/petexpenserecords")]
    public async Task Phase11_ConflictingIds_ReturnBadRequest(string path)
    {
        var response = await _fixture.AuthenticatedClient.GetAsync($"{path}?vehicleId=100&petProfileId=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("vehicleId and petProfileId do not match", body, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertHeader(HttpResponseMessage response, string header, string expectedValue)
    {
        Assert.True(response.Headers.TryGetValues(header, out var values));
        Assert.Contains(values, value => string.Equals(value, expectedValue, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task AssertJsonArrayResponse(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
        Assert.True(json.RootElement.GetArrayLength() > 0);
    }

    private static async Task AssertJsonNumberResponse(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Number, json.RootElement.ValueKind);
        Assert.True(json.RootElement.GetInt32() > 0);
    }
}

public sealed class ProfileReadIntegrationFixture : IAsyncLifetime
{
    private const string Username = "integration-user";
    private const string Password = "integration-password";

    private readonly string _dbDirectory = Path.Combine(Path.GetTempPath(), "pawlogger-tests", Guid.NewGuid().ToString("N"));
    private PawLoggerWebApplicationFactory? _factory;

    public HttpClient AuthenticatedClient { get; private set; } = default!;
    public HttpClient UnauthenticatedClient { get; private set; } = default!;

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_dbDirectory);
        _factory = new PawLoggerWebApplicationFactory(_dbDirectory);
        SeedData(_factory.Services);

        var clientOptions = new WebApplicationFactoryClientOptions { AllowAutoRedirect = false };
        UnauthenticatedClient = _factory.CreateClient(clientOptions);
        AuthenticatedClient = _factory.CreateClient(clientOptions);
        AuthenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}")));

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        AuthenticatedClient.Dispose();
        UnauthenticatedClient.Dispose();
        _factory?.Dispose();

        if (Directory.Exists(_dbDirectory))
        {
            Directory.Delete(_dbDirectory, true);
        }

        return Task.CompletedTask;
    }

    private static void SeedData(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ILiteDBHelper>().GetLiteDB();

        db.GetCollection<UserData>("userrecords").Upsert(new UserData
        {
            Id = 10,
            UserName = Username,
            EmailAddress = "integration@example.com",
            Password = StaticHelper.GetHash(Password),
            IsAdmin = false,
            IsRootUser = false
        });

        db.GetCollection<Vehicle>("vehicles").Upsert(new Vehicle
        {
            Id = 100,
            Year = 2024,
            Make = "Paw",
            Model = "Logger",
            LicensePlate = "PET100"
        });

        db.GetCollection<Vehicle>("vehicles").Upsert(new Vehicle
        {
            Id = 101,
            Year = 2023,
            Make = "No",
            Model = "Access",
            LicensePlate = "PET101"
        });

        db.GetCollection<UserAccess>("useraccessrecords").Upsert(new UserAccess
        {
            Id = new UserVehicle { UserId = 10, VehicleId = 100 }
        });

        db.GetCollection<ServiceRecord>("servicerecords").Upsert(new ServiceRecord
        {
            Id = 1001,
            VehicleId = 100,
            Date = DateTime.UtcNow.Date,
            Mileage = 42000,
            Description = "Annual exam",
            Cost = 95.5m,
            Notes = "ok",
            Tags = new List<string> { "exam" }
        });

        db.GetCollection<GasRecord>("gasrecords").Upsert(new GasRecord
        {
            Id = 1002,
            VehicleId = 100,
            Date = DateTime.UtcNow.Date,
            Mileage = 42000,
            Gallons = 10.5m,
            Cost = 40.1m,
            Notes = "seed",
            Tags = new List<string> { "food" }
        });

        db.GetCollection<ReminderRecord>("reminderrecords").Upsert(new ReminderRecord
        {
            Id = 1003,
            VehicleId = 100,
            Date = DateTime.UtcNow.Date.AddDays(7),
            Mileage = 43000,
            Description = "Follow-up",
            Metric = ReminderMetric.Date,
            Tags = new List<string> { "followup" }
        });

        db.GetCollection<OdometerRecord>("odometerrecords").Upsert(new OdometerRecord
        {
            Id = 1004,
            VehicleId = 100,
            Date = DateTime.UtcNow.Date,
            InitialMileage = 41000,
            Mileage = 42000,
            Notes = "seed",
            Tags = new List<string> { "odo" }
        });

        db.GetCollection<PlanRecord>("planrecords").Upsert(new PlanRecord
        {
            Id = 1005,
            VehicleId = 100,
            DateCreated = DateTime.UtcNow.Date,
            DateModified = DateTime.UtcNow.Date,
            Description = "Planned check",
            Notes = "seed",
            ImportMode = ImportMode.ServiceRecord,
            Priority = PlanPriority.Normal,
            Progress = PlanProgress.Backlog,
            Cost = 15m
        });

        db.GetCollection<TaxRecord>("taxrecords").Upsert(new TaxRecord
        {
            Id = 1006,
            VehicleId = 100,
            Date = DateTime.UtcNow.Date,
            Description = "License renewal",
            Cost = 60m,
            Notes = "seed",
            Tags = new List<string> { "renewal" }
        });

        db.GetCollection<CollisionRecord>("collisionrecords").Upsert(new CollisionRecord
        {
            Id = 1007,
            VehicleId = 100,
            Date = DateTime.UtcNow.Date,
            Mileage = 42100,
            Description = "Minor repair",
            Cost = 75m,
            Notes = "seed",
            Tags = new List<string> { "repair" }
        });

        db.GetCollection<Note>("notes").Upsert(new Note
        {
            Id = 1008,
            VehicleId = 100,
            Description = "Food preference",
            NoteText = "Prefers salmon kibble.",
            Pinned = true,
            Tags = new List<string> { "notes" }
        });

        db.GetCollection<UpgradeRecord>("upgraderecords").Upsert(new UpgradeRecord
        {
            Id = 1009,
            VehicleId = 100,
            Date = DateTime.UtcNow.Date,
            Mileage = 42200,
            Description = "Harness upgrade",
            Cost = 35m,
            Notes = "seed",
            Tags = new List<string> { "upgrade" }
        });

        db.GetCollection<EquipmentRecord>("equipmentrecords").Upsert(new EquipmentRecord
        {
            Id = 1010,
            VehicleId = 100,
            Description = "Travel crate",
            IsEquipped = true,
            Notes = "seed",
            Tags = new List<string> { "equipment" }
        });

        db.GetCollection<SupplyRecord>("supplyrecords").Upsert(new SupplyRecord
        {
            Id = 1011,
            VehicleId = 100,
            Date = DateTime.UtcNow.Date,
            Description = "Kibble stock",
            PartNumber = "KBL-1",
            PartSupplier = "Pet Store",
            Quantity = 1,
            Cost = 22m,
            Notes = "seed",
            Tags = new List<string> { "supply" }
        });

        db.GetCollection<HealthRecord>("healthrecords").Upsert(new HealthRecord
        {
            Id = 1012,
            VehicleId = 100,
            Date = DateTime.UtcNow.Date,
            Title = "Annual wellness exam",
            Category = HealthRecordCategory.PreventiveCare,
            Provider = "Paw Clinic",
            Status = HealthRecordStatus.Completed,
            Cost = 80m,
            Notes = "seed",
            Tags = new List<string> { "health" }
        });

        db.GetCollection<VetVisitRecord>("vetvisitrecords").Upsert(new VetVisitRecord
        {
            Id = 1013,
            VehicleId = 100,
            Date = DateTime.UtcNow.Date,
            Clinic = "Paw Clinic",
            Veterinarian = "Dr. Quinn",
            ReasonForVisit = "Limp check",
            Diagnosis = "Minor strain",
            TreatmentProvided = "Rest",
            Cost = 65m,
            Notes = "seed",
            Tags = new List<string> { "vetvisit" }
        });

        db.GetCollection<VaccinationRecord>("vaccinationrecords").Upsert(new VaccinationRecord
        {
            Id = 1014,
            VehicleId = 100,
            Date = DateTime.UtcNow.Date,
            VaccineName = "Rabies",
            NextDueDate = DateTime.UtcNow.Date.AddYears(1).ToString("yyyy-MM-dd"),
            AdministeredBy = "Tech A",
            Clinic = "Paw Clinic",
            Cost = 45m,
            Notes = "seed",
            Tags = new List<string> { "vaccination" }
        });

        db.GetCollection<MedicationRecord>("medicationrecords").Upsert(new MedicationRecord
        {
            Id = 1015,
            VehicleId = 100,
            Date = DateTime.UtcNow.Date,
            MedicationName = "Apoquel",
            Dosage = "16",
            Unit = "mg",
            Frequency = "Daily",
            Route = "Oral",
            PrescribingVet = "Dr. Quinn",
            Purpose = "Itch control",
            IsActive = true,
            Cost = 30m,
            Notes = "seed",
            Tags = new List<string> { "medication" }
        });

        db.GetCollection<LicensingRecord>("licensingrecords").Upsert(new LicensingRecord
        {
            Id = 1016,
            VehicleId = 100,
            Date = DateTime.UtcNow.Date,
            LicenseNumber = "LIC-100",
            Issuer = "City",
            ExpiryDate = DateTime.UtcNow.Date.AddYears(1).ToString("yyyy-MM-dd"),
            RenewalReminderEnabled = true,
            Cost = 25m,
            Notes = "seed",
            Tags = new List<string> { "licensing" }
        });

        db.GetCollection<PetExpenseRecord>("petexpenserecords").Upsert(new PetExpenseRecord
        {
            Id = 1017,
            VehicleId = 100,
            Date = DateTime.UtcNow.Date,
            Category = PetExpenseCategory.Vet,
            Vendor = "Paw Clinic",
            Description = "Exam fee",
            Cost = 95m,
            Notes = "seed",
            Tags = new List<string> { "petexpense" }
        });

        db.Checkpoint();
    }

    private sealed class PawLoggerWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbDirectory;

        public PawLoggerWebApplicationFactory(string dbDirectory)
        {
            _dbDirectory = dbDirectory;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EnableAuth"] = "true",
                    ["POSTGRES_CONNECTION"] = string.Empty,
                    ["PAWLOGGER_POSTGRES_CONNECTION"] = string.Empty
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ILiteDBHelper>();
                services.AddSingleton<ILiteDBHelper>(_ => new TestLiteDbHelper(Path.Combine(_dbDirectory, "cartracker.db")));
            });
        }
    }

    private sealed class TestLiteDbHelper : ILiteDBHelper
    {
        private readonly string _dbPath;
        private LiteDB.LiteDatabase? _db;

        public TestLiteDbHelper(string dbPath)
        {
            _dbPath = dbPath;
        }

        public LiteDB.LiteDatabase GetLiteDB()
        {
            _db ??= new LiteDB.LiteDatabase(_dbPath);
            return _db;
        }

        public void DisposeLiteDB()
        {
            _db?.Dispose();
            _db = null;
        }
    }
}
