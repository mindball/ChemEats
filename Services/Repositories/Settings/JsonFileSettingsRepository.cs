using System.Text.Json;
using Domain.Repositories.Settings;

namespace Services.Repositories.Settings;

public sealed class JsonFileSettingsRepository : ISettingsRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private const decimal DefaultPortionAmount = 3.00m;

    private readonly string _filePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private decimal _portionAmount = DefaultPortionAmount;
    private bool _isLoaded;

    public JsonFileSettingsRepository(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public async Task<decimal> GetCompanyPortionAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _portionAmount;
    }

    public async Task SetCompanyPortionAsync(decimal portionAmount, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _portionAmount = portionAmount < 0 ? 0 : portionAmount;
            await PersistAsync(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_isLoaded) return;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_isLoaded) return;

            if (File.Exists(_filePath))
            {
                byte[] bytes = await File.ReadAllBytesAsync(_filePath, cancellationToken);
                CompanySettingsData? data = JsonSerializer.Deserialize<CompanySettingsData>(bytes, JsonOptions);
                if (data is not null)
                {
                    _portionAmount = data.PortionAmount;
                }
            }

            _isLoaded = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        CompanySettingsData data = new(PortionAmount: _portionAmount);
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(data, JsonOptions);
        await File.WriteAllBytesAsync(_filePath, bytes, cancellationToken);
    }
}
