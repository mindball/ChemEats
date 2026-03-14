using Services.Repositories.Settings;

namespace ChemEats.Tests.Services.Repositories.Settings;

public class InMemorySettingsRepositoryTests
{
    [Fact]
    public async Task GetCompanyPortionAsync_ShouldReturnDefaultValue_WhenNoValueWasSet()
    {
        InMemorySettingsRepository repository = new();

        decimal portion = await repository.GetCompanyPortionAsync();

        Assert.Equal(3.00m, portion);
    }

    [Fact]
    public async Task SetCompanyPortionAsync_ShouldClampToZero_WhenValueIsNegative()
    {
        InMemorySettingsRepository repository = new();

        await repository.SetCompanyPortionAsync(-5m);

        decimal portion = await repository.GetCompanyPortionAsync();
        Assert.Equal(0m, portion);
    }

    [Fact]
    public async Task SetCompanyPortionAsync_ShouldBeSharedBetweenInstances_BecauseStorageIsStatic()
    {
        InMemorySettingsRepository first = new();
        InMemorySettingsRepository second = new();

        await first.SetCompanyPortionAsync(9m);

        decimal portionFromSecond = await second.GetCompanyPortionAsync();
        Assert.Equal(9m, portionFromSecond);

        await first.SetCompanyPortionAsync(3.00m);
    }
}
