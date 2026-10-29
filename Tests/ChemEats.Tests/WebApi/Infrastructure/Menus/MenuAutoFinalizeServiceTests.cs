using Domain.Repositories.MealOrders;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebApi.Infrastructure.Menus;

namespace ChemEats.Tests.WebApi.Infrastructure.Menus;

public class MenuAutoFinalizeServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 12, 5, 0);

    private readonly Mock<IMealOrderRepository> _orderRepositoryMock = new();

    [Fact]
    public async Task FinalizeExpiredMenusAsync_WhenExpiredMenusHavePendingOrders_ShouldCompleteEachMenuAndReturnTotal()
    {
        Guid firstMenu = Guid.NewGuid();
        Guid secondMenu = Guid.NewGuid();
        _orderRepositoryMock.Setup(repository => repository.GetExpiredMenuIdsWithPendingOrdersAsync(Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync([firstMenu, secondMenu]);
        _orderRepositoryMock.Setup(repository => repository.MarkPendingOrdersAsCompletedForMenuAsync(firstMenu, It.IsAny<CancellationToken>()))
            .ReturnsAsync((3, 30m));
        _orderRepositoryMock.Setup(repository => repository.MarkPendingOrdersAsCompletedForMenuAsync(secondMenu, It.IsAny<CancellationToken>()))
            .ReturnsAsync((2, 18m));

        int result = await CreateService().FinalizeExpiredMenusAsync(Now);

        Assert.Equal(5, result);
        _orderRepositoryMock.VerifyAll();
    }

    [Fact]
    public async Task FinalizeExpiredMenusAsync_WhenNothingExpired_ShouldNotCompleteAnything()
    {
        _orderRepositoryMock.Setup(repository => repository.GetExpiredMenuIdsWithPendingOrdersAsync(Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        int result = await CreateService().FinalizeExpiredMenusAsync(Now);

        Assert.Equal(0, result);
        _orderRepositoryMock.Verify(
            repository => repository.MarkPendingOrdersAsCompletedForMenuAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task FinalizeExpiredMenusAsync_WhenOneMenuFails_ShouldStillFinalizeTheOthers()
    {
        Guid failingMenu = Guid.NewGuid();
        Guid okMenu = Guid.NewGuid();
        _orderRepositoryMock.Setup(repository => repository.GetExpiredMenuIdsWithPendingOrdersAsync(Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync([failingMenu, okMenu]);
        _orderRepositoryMock.Setup(repository => repository.MarkPendingOrdersAsCompletedForMenuAsync(failingMenu, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database busy"));
        _orderRepositoryMock.Setup(repository => repository.MarkPendingOrdersAsCompletedForMenuAsync(okMenu, It.IsAny<CancellationToken>()))
            .ReturnsAsync((4, 40m));

        int result = await CreateService().FinalizeExpiredMenusAsync(Now);

        Assert.Equal(4, result);
        _orderRepositoryMock.Verify(
            repository => repository.MarkPendingOrdersAsCompletedForMenuAsync(okMenu, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private MenuAutoFinalizeService CreateService() =>
        new(_orderRepositoryMock.Object, NullLogger<MenuAutoFinalizeService>.Instance);
}
