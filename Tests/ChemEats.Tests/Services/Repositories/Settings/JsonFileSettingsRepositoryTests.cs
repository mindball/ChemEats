using Services.Repositories.Settings;

namespace ChemEats.Tests.Services.Repositories.Settings;

public sealed class JsonFileSettingsRepositoryTests : IDisposable
{
    private readonly string _tempDirectory;

    public JsonFileSettingsRepositoryTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"chemeats-settings-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task GetCompanyPortionAsync_WhenFileDoesNotExist_ShouldReturnDefaultPortion()
    {
        string filePath = Path.Combine(_tempDirectory, "settings.json");
        JsonFileSettingsRepository repository = new(filePath);

        decimal portion = await repository.GetCompanyPortionAsync();

        Assert.Equal(3.00m, portion);
    }

    [Fact]
    public async Task GetCompanyPortionAsync_WhenFileExists_ShouldReadPersistedValue()
    {
        string filePath = Path.Combine(_tempDirectory, "settings.json");
        CompanySettingsData data = new(PortionAmount: 7.50m);
        byte[] bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllBytesAsync(filePath, bytes);

        JsonFileSettingsRepository repository = new(filePath);

        decimal portion = await repository.GetCompanyPortionAsync();

        Assert.Equal(7.50m, portion);
    }

    [Fact]
    public async Task SetCompanyPortionAsync_WhenNegativeValueIsProvided_ShouldPersistZero()
    {
        string filePath = Path.Combine(_tempDirectory, "settings.json");
        JsonFileSettingsRepository repository = new(filePath);

        await repository.SetCompanyPortionAsync(-10m);

        decimal portion = await repository.GetCompanyPortionAsync();
        Assert.Equal(0m, portion);

        JsonFileSettingsRepository repositoryFromDisk = new(filePath);
        decimal persistedPortion = await repositoryFromDisk.GetCompanyPortionAsync();
        Assert.Equal(0m, persistedPortion);
    }

    [Fact]
    public async Task SetCompanyPortionAsync_ShouldCreateDirectoryAndPersistData()
    {
        string nestedDirectory = Path.Combine(_tempDirectory, "nested", "settings");
        string filePath = Path.Combine(nestedDirectory, "company.json");
        JsonFileSettingsRepository repository = new(filePath);

        await repository.SetCompanyPortionAsync(9.25m);

        Assert.True(File.Exists(filePath));

        JsonFileSettingsRepository repositoryReloaded = new(filePath);
        decimal portion = await repositoryReloaded.GetCompanyPortionAsync();
        Assert.Equal(9.25m, portion);
    }

    [Fact]
    public async Task GetCompanyPortionAsync_WhenJsonIsInvalid_ShouldThrowJsonException()
    {
        string filePath = Path.Combine(_tempDirectory, "settings.json");
        await File.WriteAllTextAsync(filePath, "{ invalid-json }");

        JsonFileSettingsRepository repository = new(filePath);

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => repository.GetCompanyPortionAsync());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
