using SportLab.Services;

namespace SportLab.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_PlainTextPassword_ProducesNonEmptyHashDifferentFromPassword()
    {
        // Arrange
        const string password = "Str0ngPass!";

        // Act
        var hash = PasswordHasher.Hash(password);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual(password, hash);
        Assert.StartsWith("pbkdf2$", hash);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        const string password = "CorrectHorseBatteryStaple";
        var hash = PasswordHasher.Hash(password);

        // Act
        var isValid = PasswordHasher.Verify(password, hash);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void Verify_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        const string password = "CorrectHorseBatteryStaple";
        var hash = PasswordHasher.Hash(password);

        // Act
        var isValid = PasswordHasher.Verify("WrongPassword123!", hash);

        // Assert
        Assert.False(isValid);
    }
}
