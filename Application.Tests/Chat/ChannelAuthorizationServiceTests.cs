namespace BackBase.Application.Tests.Chat;

using BackBase.Domain.Enums;
using BackBase.Infrastructure.Chat;

public sealed class ChannelAuthorizationServiceTests
{
    private readonly ChannelAuthorizationService _service = new();
    private static readonly Guid TestUserId = Guid.NewGuid();
    private const string TestChannelId = "test-channel";

    [Fact]
    public async Task CanUserAccessChannelAsync_GlobalChannel_ReturnsTrue()
    {
        // Arrange & Act
        var result = await _service.CanUserAccessChannelAsync(
            TestUserId, ChannelType.Global, TestChannelId);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(ChannelType.Guild)]
    [InlineData(ChannelType.DirectMessage)]
    [InlineData(ChannelType.Party)]
    public async Task CanUserAccessChannelAsync_NonGlobalChannel_ReturnsFalse(
        ChannelType channelType)
    {
        // Arrange & Act
        var result = await _service.CanUserAccessChannelAsync(
            TestUserId, channelType, TestChannelId);

        // Assert
        Assert.False(result);
    }
}
