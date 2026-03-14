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

[CollectionDefinition(nameof(ProfileWriteIntegrationCollection), DisableParallelization = true)]
public sealed class ProfileWriteIntegrationCollection : ICollectionFixture<ProfileWriteIntegrationEnabledFixture>
{
}

[Collection(nameof(ProfileWriteIntegrationCollection))]
public class ProfileWriteIntegrationTests
{
    private readonly ProfileWriteIntegrationEnabledFixture _fixture;

    public ProfileWriteIntegrationTests(ProfileWriteIntegrationEnabledFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LegacyAddNote_Works_WithVehicleId()
    {
        var payload = BuildNotePayload("legacy-add", "legacy note text");
        var response = await _fixture.AuthenticatedClient.PostAsync(
            "/api/vehicle/notes/add?vehicleId=100",
            payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertHeader(response, "X-PawLogger-Api-Contract", "legacy-vehicle-v1");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Note Added", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task V2AddNote_Works_WithPetProfileId_WhenEnabled()
    {
        var payload = BuildNotePayload("v2-add-pet", "v2 note text");
        var response = await _fixture.AuthenticatedClient.PostAsync(
            "/api/v2/profiles/notes/add?petProfileId=100",
            payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertHeader(response, "X-PawLogger-Api-Contract", "v2-profiles-shadow");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Note Added", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task V2AddNote_AcceptsVehicleIdAlias_WhenAliasFlagEnabled()
    {
        var payload = BuildNotePayload("v2-add-vehicle", "v2 alias note");
        var response = await _fixture.AuthenticatedClient.PostAsync(
            "/api/v2/profiles/notes/add?vehicleId=100",
            payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertHeader(response, "X-PawLogger-Legacy-Id", "vehicleId");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Note Added", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task V2AddNote_ConflictingIds_ReturnsBadRequest()
    {
        var payload = BuildNotePayload("v2-conflict", "v2 conflict note");
        var response = await _fixture.AuthenticatedClient.PostAsync(
            "/api/v2/profiles/notes/add?vehicleId=100&petProfileId=101",
            payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("vehicleId and petProfileId do not match", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LegacyAndV2AddNote_ValidationFailures_AreConsistent()
    {
        var invalidPayload = BuildNotePayload(string.Empty, "only text");
        var legacyResponse = await _fixture.AuthenticatedClient.PostAsync(
            "/api/vehicle/notes/add?vehicleId=100",
            invalidPayload);

        var invalidPayloadV2 = BuildNotePayload(string.Empty, "only text");
        var v2Response = await _fixture.AuthenticatedClient.PostAsync(
            "/api/v2/profiles/notes/add?petProfileId=100",
            invalidPayloadV2);

        Assert.Equal(HttpStatusCode.BadRequest, legacyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, v2Response.StatusCode);
        var legacyBody = await legacyResponse.Content.ReadAsStringAsync();
        var v2Body = await v2Response.Content.ReadAsStringAsync();
        Assert.Contains("Description and NoteText cannot be empty", legacyBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Description and NoteText cannot be empty", v2Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LegacyAndV2AddNote_AuthOutcomes_AreEquivalent_ForUnauthorizedVehicle()
    {
        var legacyResponse = await _fixture.AuthenticatedClient.PostAsync(
            "/api/vehicle/notes/add?vehicleId=101",
            BuildNotePayload("legacy-denied", "text"));
        var v2Response = await _fixture.AuthenticatedClient.PostAsync(
            "/api/v2/profiles/notes/add?petProfileId=101",
            BuildNotePayload("v2-denied", "text"));

        Assert.Equal(legacyResponse.StatusCode, v2Response.StatusCode);
        var legacyBody = await legacyResponse.Content.ReadAsStringAsync();
        var v2Body = await v2Response.Content.ReadAsStringAsync();
        Assert.Contains("Access Denied", legacyBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Access Denied", v2Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unauthenticated_LegacyAndV2AddRoutes_ReturnUnauthorized()
    {
        var legacy = await _fixture.UnauthenticatedClient.PostAsync(
            "/api/vehicle/notes/add?vehicleId=100",
            BuildNotePayload("legacy-unauth", "text"));
        var v2 = await _fixture.UnauthenticatedClient.PostAsync(
            "/api/v2/profiles/notes/add?petProfileId=100",
            BuildNotePayload("v2-unauth", "text"));

        Assert.Equal(HttpStatusCode.Unauthorized, legacy.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, v2.StatusCode);
    }

    [Fact]
    public async Task V2UpdateAndDelete_Work_WithIdAliasGuards()
    {
        var createResponse = await _fixture.AuthenticatedClient.PostAsync(
            "/api/v2/profiles/notes/add?petProfileId=100",
            BuildNotePayload("v2-edit-delete", "before"));
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var noteId = await FindNoteIdByDescriptionAsync("v2-edit-delete");
        Assert.True(noteId > 0);

        var updatePayload = BuildUpdatePayload(noteId, "v2-edit-delete", "after");
        var updateResponse = await _fixture.AuthenticatedClient.PutAsync(
            "/api/v2/profiles/notes/update?petProfileId=100",
            updatePayload);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var conflictUpdateResponse = await _fixture.AuthenticatedClient.PutAsync(
            "/api/v2/profiles/notes/update?vehicleId=100&petProfileId=101",
            BuildUpdatePayload(noteId, "v2-edit-delete", "conflict"));
        Assert.Equal(HttpStatusCode.BadRequest, conflictUpdateResponse.StatusCode);

        var deleteResponse = await _fixture.AuthenticatedClient.DeleteAsync(
            $"/api/v2/profiles/notes/delete?id={noteId}&petProfileId=100");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var conflictDeleteResponse = await _fixture.AuthenticatedClient.DeleteAsync(
            $"/api/v2/profiles/notes/delete?id={noteId}&vehicleId=100&petProfileId=101");
        Assert.Equal(HttpStatusCode.BadRequest, conflictDeleteResponse.StatusCode);
    }

    private async Task<int> FindNoteIdByDescriptionAsync(string description)
    {
        var notesResponse = await _fixture.AuthenticatedClient.GetAsync("/api/vehicle/notes?vehicleId=100");
        notesResponse.EnsureSuccessStatusCode();
        var notesBody = await notesResponse.Content.ReadAsStringAsync();
        using var notesJson = JsonDocument.Parse(notesBody);
        foreach (var element in notesJson.RootElement.EnumerateArray())
        {
            if (TryGetStringProperty(element, "Description", out var elementDescription) &&
                string.Equals(elementDescription, description, StringComparison.Ordinal))
            {
                if (TryGetStringProperty(element, "Id", out var elementId) &&
                    int.TryParse(elementId, out var parsedId))
                {
                    return parsedId;
                }
            }
        }
        return 0;
    }

    private static bool TryGetStringProperty(JsonElement element, string propertyName, out string value)
    {
        if (element.TryGetProperty(propertyName, out var direct))
        {
            value = direct.GetString() ?? string.Empty;
            return true;
        }

        var camel = char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
        if (element.TryGetProperty(camel, out var camelValue))
        {
            value = camelValue.GetString() ?? string.Empty;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static StringContent BuildNotePayload(string description, string noteText)
    {
        return new StringContent(
            JsonSerializer.Serialize(new
            {
                description,
                noteText,
                pinned = "false",
                tags = "phase13"
            }),
            Encoding.UTF8,
            "application/json");
    }

    private static StringContent BuildUpdatePayload(int id, string description, string noteText)
    {
        return new StringContent(
            JsonSerializer.Serialize(new
            {
                id = id.ToString(),
                description,
                noteText,
                pinned = "false",
                tags = "phase13"
            }),
            Encoding.UTF8,
            "application/json");
    }

    private static void AssertHeader(HttpResponseMessage response, string header, string expectedValue)
    {
        Assert.True(response.Headers.TryGetValues(header, out var values));
        Assert.Contains(values, value => string.Equals(value, expectedValue, StringComparison.OrdinalIgnoreCase));
    }
}

[CollectionDefinition(nameof(ProfileWriteIntegrationFlagOffCollection), DisableParallelization = true)]
public sealed class ProfileWriteIntegrationFlagOffCollection : ICollectionFixture<ProfileWriteIntegrationDisabledFixture>
{
}

[Collection(nameof(ProfileWriteIntegrationFlagOffCollection))]
public class ProfileWriteIntegrationFeatureFlagOffTests
{
    private readonly ProfileWriteIntegrationDisabledFixture _fixture;

    public ProfileWriteIntegrationFeatureFlagOffTests(ProfileWriteIntegrationDisabledFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task V2AddNote_IsBlocked_WhenFlagsDisabled_AndLegacyRemainsAvailable()
    {
        var v2Response = await _fixture.AuthenticatedClient.PostAsync(
            "/api/v2/profiles/notes/add?petProfileId=100",
            new StringContent(
                JsonSerializer.Serialize(new { description = "blocked", noteText = "blocked" }),
                Encoding.UTF8,
                "application/json"));
        Assert.Equal(HttpStatusCode.NotFound, v2Response.StatusCode);

        var legacyResponse = await _fixture.AuthenticatedClient.PostAsync(
            "/api/vehicle/notes/add?vehicleId=100",
            new StringContent(
                JsonSerializer.Serialize(new { description = "legacy-ok", noteText = "legacy-ok" }),
                Encoding.UTF8,
                "application/json"));
        Assert.Equal(HttpStatusCode.OK, legacyResponse.StatusCode);
    }
}

public sealed class ProfileWriteIntegrationEnabledFixture : ProfileWriteIntegrationFixtureBase
{
    public ProfileWriteIntegrationEnabledFixture() : base(new Dictionary<string, string?>
    {
        ["PAWLOGGER_WRITE_V2_ROUTES"] = "true",
        ["PAWLOGGER_WRITE_V2_FAMILY_NOTES"] = "true",
        ["PAWLOGGER_WRITE_V2_ALIAS_PARSING"] = "true",
        ["PAWLOGGER_WRITE_V2_STRICT_ID_CONFLICT_REJECT"] = "true"
    })
    {
    }
}

public sealed class ProfileWriteIntegrationDisabledFixture : ProfileWriteIntegrationFixtureBase
{
    public ProfileWriteIntegrationDisabledFixture() : base(new Dictionary<string, string?>
    {
        ["PAWLOGGER_WRITE_V2_ROUTES"] = "false",
        ["PAWLOGGER_WRITE_V2_FAMILY_NOTES"] = "false",
        ["PAWLOGGER_WRITE_V2_ALIAS_PARSING"] = "false",
        ["PAWLOGGER_WRITE_V2_STRICT_ID_CONFLICT_REJECT"] = "true"
    })
    {
    }
}

public abstract class ProfileWriteIntegrationFixtureBase : IAsyncLifetime
{
    private const string Username = "write-integration-user";
    private const string Password = "write-integration-password";

    private readonly IDictionary<string, string?> _featureFlags;
    private readonly string _dbDirectory = Path.Combine(Path.GetTempPath(), "pawlogger-tests", Guid.NewGuid().ToString("N"));
    private PawLoggerWebApplicationFactory? _factory;

    public HttpClient AuthenticatedClient { get; private set; } = default!;
    public HttpClient UnauthenticatedClient { get; private set; } = default!;

    protected ProfileWriteIntegrationFixtureBase(IDictionary<string, string?> featureFlags)
    {
        _featureFlags = featureFlags;
    }

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_dbDirectory);
        _factory = new PawLoggerWebApplicationFactory(_dbDirectory, _featureFlags);
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
            Id = 20,
            UserName = Username,
            EmailAddress = "write-integration@example.com",
            Password = StaticHelper.GetHash(Password),
            IsAdmin = false,
            IsRootUser = false
        });

        db.GetCollection<Vehicle>("vehicles").Upsert(new Vehicle
        {
            Id = 100,
            Year = 2026,
            Make = "Paw",
            Model = "Write",
            LicensePlate = "WRITE100"
        });

        db.GetCollection<Vehicle>("vehicles").Upsert(new Vehicle
        {
            Id = 101,
            Year = 2026,
            Make = "Paw",
            Model = "Denied",
            LicensePlate = "WRITE101"
        });

        db.GetCollection<UserAccess>("useraccessrecords").Upsert(new UserAccess
        {
            Id = new UserVehicle { UserId = 20, VehicleId = 100 }
        });

        db.GetCollection<Note>("notes").Upsert(new Note
        {
            Id = 2001,
            VehicleId = 100,
            Description = "seed-write-note",
            NoteText = "seed text",
            Pinned = false,
            Tags = new List<string> { "seed" }
        });

        db.Checkpoint();
    }

    private sealed class PawLoggerWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbDirectory;
        private readonly IDictionary<string, string?> _featureFlags;

        public PawLoggerWebApplicationFactory(string dbDirectory, IDictionary<string, string?> featureFlags)
        {
            _dbDirectory = dbDirectory;
            _featureFlags = featureFlags;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["EnableAuth"] = "true",
                    ["POSTGRES_CONNECTION"] = string.Empty,
                    ["PAWLOGGER_POSTGRES_CONNECTION"] = string.Empty
                };
                foreach (var kvp in _featureFlags)
                {
                    values[kvp.Key] = kvp.Value;
                }
                configBuilder.AddInMemoryCollection(values);
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
