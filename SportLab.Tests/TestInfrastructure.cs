using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Localization;
using Moq;
using SportLab.Controllers;
using SportLab.Data;

namespace SportLab.Tests;

/// <summary>
/// Shared helpers for the unit test suite: an isolated EF Core InMemory
/// database, a fake session, a mocked localizer and a controller context
/// with an authenticated user claim.
/// </summary>
internal static class TestInfrastructure
{
    public static SportLabContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<SportLabContext>()
            .UseInMemoryDatabase(databaseName)
            // The InMemory provider does not support real transactions.
            // Ignoring this warning lets BeginTransactionAsync(...) run as a no-op.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new SportLabContext(options);
    }

    public static Mock<IStringLocalizer<T>> CreateLocalizer<T>()
    {
        var mock = new Mock<IStringLocalizer<T>>();

        mock.Setup(l => l[It.IsAny<string>()])
            .Returns((string name) => new LocalizedString(name, name));

        mock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string name, object[] args) => new LocalizedString(name, $"{name}:{string.Join(",", args)}"));

        return mock;
    }

    public static ControllerContext CreateControllerContext(int userId, out DefaultHttpContext httpContext)
    {
        httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, "Client")
            }, authenticationType: "TestAuth"))
        };

        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");
        httpContext.Session = new TestSession();

        return new ControllerContext { HttpContext = httpContext };
    }

    public static TempDataDictionary CreateTempData(DefaultHttpContext httpContext) =>
        new(httpContext, Mock.Of<ITempDataProvider>());
}

/// <summary>
/// Minimal in-memory <see cref="ISession"/> implementation so cart tests
/// can exercise the real session-based cart logic without a web server.
/// </summary>
internal sealed class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString();
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _store.TryGetValue(key, out value);
}

/// <summary>
/// Builds a <see cref="CartController"/> with all of its dependencies
/// (session, TempData, mocked localizer and email sender) wired up.
/// </summary>
internal static class CartTestFactory
{
    public static CartController Create(
        SportLabContext context,
        int userId,
        SportLab.Services.IEmailSender emailSender,
        out DefaultHttpContext httpContext)
    {
        var controllerContext = TestInfrastructure.CreateControllerContext(userId, out httpContext);
        var localizer = TestInfrastructure.CreateLocalizer<CartController>();

        return new CartController(context, localizer.Object, emailSender)
        {
            ControllerContext = controllerContext,
            TempData = TestInfrastructure.CreateTempData(httpContext)
        };
    }

    public static CheckoutViewModel CreateCheckoutModel(string email = "client@example.com") => new()
    {
        FullName = "Test Client",
        Phone = "+380501112233",
        Email = email,
        DeliveryMethod = "pickup",
        PaymentMethod = "card",
        Address = "Kyiv"
    };
}
