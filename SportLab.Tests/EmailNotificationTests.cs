using Microsoft.AspNetCore.Mvc;
using MimeKit;
using Moq;
using SportLab.Controllers;
using SportLab.Models;
using SportLab.Services;

namespace SportLab.Tests;

public class EmailNotificationTests
{
    [Fact]
    public async Task Checkout_ValidOrder_SendsEmailOnceToCustomerAndPersistsOrder()
    {
        // Arrange
        using var context = TestInfrastructure.CreateContext(Guid.NewGuid().ToString());

        var product = new Product { Name = "Boxing Gloves", Price = 30m, StockQuantity = 10 };
        context.Products.Add(product);
        context.SaveChanges();

        var emailMock = new Mock<IEmailSender>();
        emailMock
            .Setup(e => e.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CartTestFactory.Create(context, userId: 42, emailMock.Object, out _);
        controller.AddToCart(product.Id, quantity: 2);

        var model = CartTestFactory.CreateCheckoutModel("client@example.com");

        // Act
        var result = await controller.Checkout(model);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CartController.OrderSuccess), redirect.ActionName);

        var order = Assert.Single(context.Orders);
        Assert.Equal(60m, order.TotalAmount);
        Assert.Equal("Confirmed", order.Status);

        var orderDetail = Assert.Single(context.OrderDetails);
        Assert.Equal(2, orderDetail.Quantity);
        Assert.Equal(30m, orderDetail.UnitPrice);

        Assert.Equal(8, context.Products.Single(p => p.Id == product.Id).StockQuantity);

        emailMock.Verify(
            e => e.SendAsync(
                It.Is<MimeMessage>(m => m.To.Mailboxes.Any(mailbox => mailbox.Address == "client@example.com")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Checkout_WhenEmailSenderThrows_StillPersistsOrderAndRedirectsToSuccess()
    {
        // Arrange
        using var context = TestInfrastructure.CreateContext(Guid.NewGuid().ToString());

        var product = new Product { Name = "Resistance Band", Price = 15m, StockQuantity = 5 };
        context.Products.Add(product);
        context.SaveChanges();

        var emailMock = new Mock<IEmailSender>();
        emailMock
            .Setup(e => e.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP server is unavailable."));

        var controller = CartTestFactory.Create(context, userId: 43, emailMock.Object, out _);
        controller.AddToCart(product.Id, quantity: 1);

        var model = CartTestFactory.CreateCheckoutModel("buyer@example.com");

        // Act
        var result = await controller.Checkout(model);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CartController.OrderSuccess), redirect.ActionName);

        var order = Assert.Single(context.Orders);
        Assert.Equal(15m, order.TotalAmount);

        Assert.Equal(4, context.Products.Single(p => p.Id == product.Id).StockQuantity);

        emailMock.Verify(
            e => e.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
