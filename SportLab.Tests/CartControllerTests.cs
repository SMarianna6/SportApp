using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SportLab.Controllers;
using SportLab.Models;
using SportLab.Services;

namespace SportLab.Tests;

public class CartControllerTests
{
    [Fact]
    public void AddToCart_MultipleProducts_CalculatesSubtotalAndTotalCorrectly()
    {
        // Arrange
        using var context = TestInfrastructure.CreateContext(Guid.NewGuid().ToString());

        var protein = new Product { Name = "Protein", Price = 10.50m, StockQuantity = 10 };
        var shaker = new Product { Name = "Shaker", Price = 4.25m, StockQuantity = 10 };
        context.Products.AddRange(protein, shaker);
        context.SaveChanges();

        var emailMock = new Mock<IEmailSender>();
        var controller = CartTestFactory.Create(context, userId: 1, emailMock.Object, out _);

        // Act
        controller.AddToCart(protein.Id, quantity: 2);
        controller.AddToCart(shaker.Id, quantity: 3);

        var summaryResult = Assert.IsType<JsonResult>(controller.GetCartSummary());
        var json = JsonSerializer.Serialize(summaryResult.Value);
        using var document = JsonDocument.Parse(json);

        var totalAmount = document.RootElement.GetProperty("totalAmount").GetDecimal();
        var totalCount = document.RootElement.GetProperty("totalCount").GetInt32();

        // Assert
        var proteinSubtotal = new CartItem { UnitPrice = 10.50m, Quantity = 2 }.Subtotal;

        Assert.Equal(21.00m, proteinSubtotal);
        Assert.Equal((10.50m * 2) + (4.25m * 3), totalAmount);
        Assert.Equal(33.75m, totalAmount);
        Assert.Equal(5, totalCount);
    }

    [Fact]
    public async Task Checkout_WhenStockIsLessThanCartQuantity_RejectsOrderAndDoesNotDeductStock()
    {
        // Arrange
        using var context = TestInfrastructure.CreateContext(Guid.NewGuid().ToString());

        var product = new Product { Name = "Jump Rope", Price = 25m, StockQuantity = 5 };
        context.Products.Add(product);
        context.SaveChanges();

        var emailMock = new Mock<IEmailSender>();
        var controller = CartTestFactory.Create(context, userId: 7, emailMock.Object, out _);

        controller.AddToCart(product.Id, quantity: 5);

        // Simulate another customer buying stock after this cart was filled.
        product.StockQuantity = 2;
        context.SaveChanges();

        var model = CartTestFactory.CreateCheckoutModel();

        // Act
        var result = await controller.Checkout(model);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CartController.Index), redirect.ActionName);

        Assert.Empty(context.Orders);
        Assert.Empty(context.OrderDetails);

        var storedProduct = context.Products.Single(p => p.Id == product.Id);
        Assert.Equal(2, storedProduct.StockQuantity);

        emailMock.Verify(
            e => e.SendAsync(It.IsAny<MimeKit.MimeMessage>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
