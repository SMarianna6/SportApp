using SportLab.Models;

namespace SportLab.Tests;

public class RolesTests
{
    [Theory]
    [InlineData(" admin ", Roles.Administrator)]
    [InlineData("CLIENT", Roles.Client)]
    [InlineData("Trainer", Roles.Trainer)]
    [InlineData("  MODERATOR  ", Roles.Moderator)]
    public void Normalize_MixedCasingAndWhitespace_ReturnsCanonicalRole(string input, string expected)
    {
        // Arrange
        // The theory data provides the raw role value and the expected canonical role.

        // Act
        var result = Roles.Normalize(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, Roles.Client)]
    [InlineData("", Roles.Client)]
    [InlineData("   ", Roles.Client)]
    [InlineData("SuperAdmin", "SuperAdmin")]
    public void Normalize_EmptyWhitespaceOrUnknownRole_FollowsContract(string? input, string expected)
    {
        // Arrange
        // Empty and whitespace roles fall back to Client; unknown non-empty roles are returned unchanged.

        // Act
        var result = Roles.Normalize(input);

        // Assert
        Assert.Equal(expected, result);
    }
}
