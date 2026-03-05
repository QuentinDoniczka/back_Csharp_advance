namespace BackBase.Application.Tests.Helpers;

using BackBase.Application.Constants;
using BackBase.Application.Helpers;
using BackBase.Domain.Enums;

public sealed class ChannelNameBuilderTests
{
    [Theory]
    [InlineData(ChannelType.Global, "General", "global:General")]
    [InlineData(ChannelType.Guild, "dragon-slayers", "guild:dragon-slayers")]
    [InlineData(ChannelType.DirectMessage, "abc123", "dm:abc123")]
    [InlineData(ChannelType.Party, "raid-group-1", "party:raid-group-1")]
    public void Build_ValidChannelTypeAndId_ReturnsCorrectFormat(
        ChannelType channelType, string channelId, string expected)
    {
        // Arrange & Act
        var result = ChannelNameBuilder.Build(channelType, channelId);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Build_InvalidChannelType_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidChannelType = (ChannelType)999;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ChannelNameBuilder.Build(invalidChannelType, "some-id"));
    }
}
