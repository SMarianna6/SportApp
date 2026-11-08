using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;

namespace SportLab.Controllers
{
    public class CartController : Controller
    {
        private const string CartSessionKey = "SportLab_Cart_Items";
        private readonly SportLabContext _context;

        public CartController(SportLabContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            if (productId <= 0 || quantity <= 0)
            {
                return Json(new { success = false, message = "Invalid product selection." });
            }

            var product = _context.Products.Find(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found." });
            }

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(i => i.ProductId == productId);

            if (existingItem != null)
            {
                var newQty = existingItem.Quantity + quantity;
                if (newQty > product.StockQuantity)
                {
                    newQty = product.StockQuantity;
                }
                existingItem.Quantity = newQty;
            }
            else
            {
                var qty = Math.Min(quantity, product.StockQuantity);
                if (qty <= 0)
                {
                    return Json(new { success = false, message = "Product is currently out of stock." });
                }

                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = qty,
                    PhotoUrl = product.PhotoUrl,
                    Category = product.Category,
                    MaxStock = product.StockQuantity
                });
            }

            SaveCart(cart);

            return Json(new
            {
                success = true,
                message = "Item added to cart.",
                totalCount = cart.Sum(i => i.Quantity),
                totalAmount = cart.Sum(i => i.Subtotal)
            });
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int delta)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.ProductId == productId);

            if (item == null)
            {
                return Json(new { success = false, message = "Item not found in cart." });
            }

            var product = _context.Products.Find(productId);
            var maxStock = product?.StockQuantity ?? item.MaxStock;

            item.Quantity += delta;

            if (item.Quantity > maxStock)
            {
                item.Quantity = maxStock;
            }

            bool removed = false;
            if (item.Quantity <= 0)
            {
                cart.Remove(item);
                removed = true;
            }

            SaveCart(cart);

            return Json(new
            {
                success = true,
                removed = removed,
                quantity = item.Quantity,
                itemSubtotal = item.Subtotal,
                totalCount = cart.Sum(i => i.Quantity),
                cartTotal = cart.Sum(i => i.Subtotal)
            });
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.ProductId == productId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            return Json(new
            {
                success = true,
                totalCount = cart.Sum(i => i.Quantity),
                cartTotal = cart.Sum(i => i.Subtotal)
            });
        }

        [HttpPost]
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove(CartSessionKey);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult GetCartSummary()
        {
            var cart = GetCart();
            return Json(new
            {
                totalCount = cart.Sum(i => i.Quantity),
                totalAmount = cart.Sum(i => i.Subtotal)
            });
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            var model = new CheckoutViewModel
            {
                CartItems = cart,
                Subtotal = cart.Sum(i => i.Subtotal),
                Shipping = 0,
                Total = cart.Sum(i => i.Subtotal),
                DeliveryMethod = "pickup",
                PaymentMethod = "card"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                model.CartItems = cart;
                model.Subtotal = cart.Sum(i => i.Subtotal);
                model.Total = model.Subtotal;
                return View(model);
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
            if (user == null)
            {
                user = new User
                {
                    Name = string.IsNullOrWhiteSpace(model.FullName) ? "SportLab Client" : model.FullName.Trim(),
                    Email = string.IsNullOrWhiteSpace(model.Email) ? $"guest_{Guid.NewGuid().ToString()[..8]}@sportlab.ua" : model.Email.Trim(),
                    Password = "UserPass123!",
                    Role = "Client",
                    IsBlocked = false
                };
                _context.Users.Add(user);
                _context.SaveChanges();
            }

            var totalAmount = cart.Sum(i => i.Subtotal);

            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalAmount,
                Status = "Confirmed"
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            foreach (var item in cart)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId
                };
                _context.OrderDetails.Add(orderDetail);

                var product = _context.Products.Find(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity = Math.Max(0, product.StockQuantity - item.Quantity);
                }
            }

            _context.SaveChanges();

            try
            {
                var emailMessage = new MimeKit.MimeMessage();
                emailMessage.From.Add(new MimeKit.MailboxAddress("SportLab Store", "sporthubproject01@gmail.com"));

                emailMessage.To.Add(new MimeKit.MailboxAddress(model.FullName, model.Email));

                emailMessage.Subject = "Your SportLab Order Confirmation 🎉";

                var itemsHtml = new System.Text.StringBuilder();
                foreach (var item in cart)
                {
                    string imgUrl = !string.IsNullOrEmpty(item.PhotoUrl) && item.PhotoUrl.StartsWith("http")
                        ? item.PhotoUrl
                        : $"{Request.Scheme}://{Request.Host}{item.PhotoUrl}";

                    itemsHtml.Append($@"
                        <tr>
                            <td style='padding: 12px; border-bottom: 1px solid #eee; text-align: center; width: 70px;'>
                                <img src='{imgUrl}' alt='{item.ProductName}' style='width: 60px; height: 60px; object-fit: cover; border-radius: 8px;' />
                            </td>
                            <td style='padding: 12px; border-bottom: 1px solid #eee;'>
                                <strong style='color: #333;'>{item.ProductName}</strong><br/>
                                <span style='color: #777; font-size: 13px;'>Qty: {item.Quantity}</span>
                            </td>
                            <td style='padding: 12px; border-bottom: 1px solid #eee; text-align: right; font-weight: bold; color: #111;'>
                                {item.Subtotal:N2} ₴
                            </td>
                        </tr>");
                }

                var builder = new MimeKit.BodyBuilder();
                builder.HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; background-color: #f4f6f9; padding: 30px; margin: 0;'>
                        <div style='max-width: 600px; background: #ffffff; margin: 0 auto; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.05); border: 1px solid #e5e9f2;'>
                            
                            <!-- Header -->
                            <div style='background-color: #0d6efd; padding: 25px; text-align: center; color: #ffffff;'>
                                <h1 style='margin: 0; font-size: 24px; font-weight: bold; letter-spacing: 1px;'>SPORTHUB</h1>
                                <p style='margin: 5px 0 0 0; font-size: 14px; opacity: 0.9;'>Order Confirmation</p>
                            </div>

                            <!-- Body Content -->
                            <div style='padding: 30px;'>
                                <h2 style='color: #2c3e50; font-size: 20px; margin-top: 0;'>Hi {model.FullName},</h2>
                                <p style='color: #555; line-height: 1.6; font-size: 15px;'>
                                    Thank you for your purchase! Your order has been successfully placed and is now being processed.
                                </p>

                                <!-- Order Summary Table -->
                                <h3 style='color: #333; font-size: 16px; border-bottom: 2px solid #0d6efd; padding-bottom: 8px; margin-top: 25px;'>Order Summary</h3>
                                <table style='width: 100%; border-collapse: collapse; margin-top: 10px;'>
                                    {itemsHtml}
                                </table>

                                <!-- Total Amount -->
                                <div style='text-align: right; padding-top: 15px; font-size: 18px;'>
                                    <span style='color: #555;'>Total Amount: </span>
                                    <strong style='color: #0d6efd; font-size: 22px;'>{order.TotalAmount:N2} ₴</strong>
                                </div>

                                <!-- Details Box -->
                                <div style='background-color: #f8f9fa; border-radius: 8px; padding: 15px; margin-top: 25px; font-size: 14px; color: #555;'>
                                    <p style='margin: 3px 0;'><strong>Payment Method:</strong> {model.PaymentMethod.ToUpper()}</p>
                                    <p style='margin: 3px 0;'><strong>Delivery Option:</strong> {model.DeliveryMethod}</p>
                                    {(string.IsNullOrWhiteSpace(model.Address) ? "" : $"<p style='margin: 3px 0;'><strong>Delivery Address:</strong> {model.Address}</p>")}
                                </div>

                                <p style='color: #777; font-size: 13px; margin-top: 30px; text-align: center;'>
                                    We will contact you shortly to confirm your delivery details.
                                </p>
                            </div>

                            <!-- Footer -->
                            <div style='background-color: #f1f3f5; padding: 15px; text-align: center; color: #888; font-size: 12px;'>
                                © {DateTime.Now.Year} SportLab Store. All rights reserved.
                            </div>
                        </div>
                    </div>";

                emailMessage.Body = builder.ToMessageBody();

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    client.Connect("smtp.gmail.com", 587, false);
                    client.AuthenticateAsync("sporthubproject01@gmail.com", "zcfe yyad ezet tirb").Wait();
                    client.Send(emailMessage);
                    client.Disconnect(true);
                }
            }
            catch
            {
               
            }

            // 3. Далі йде очистка кошика та редирект
            HttpContext.Session.Remove(CartSessionKey);

            TempData["CustomerName"] = model.FullName;
            TempData["CustomerPhone"] = model.Phone;
            TempData["DeliveryMethod"] = model.DeliveryMethod;
            TempData["DeliveryAddress"] = model.Address ?? "SportLab Reception Desk (Pickup)";
            TempData["PaymentMethod"] = model.PaymentMethod;

            return RedirectToAction(nameof(OrderSuccess), new { id = order.Id });

            HttpContext.Session.Remove(CartSessionKey);

            TempData["CustomerName"] = model.FullName;
            TempData["CustomerPhone"] = model.Phone;
            TempData["DeliveryMethod"] = model.DeliveryMethod;
            TempData["DeliveryAddress"] = model.Address ?? "SportLab Reception Desk (Pickup)";
            TempData["PaymentMethod"] = model.PaymentMethod;

            return RedirectToAction(nameof(OrderSuccess), new { id = order.Id });
        }

        public IActionResult OrderSuccess(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.User)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return RedirectToAction("Index", "Shop");
            }

            ViewBag.CustomerName = TempData["CustomerName"] as string ?? order.User?.Name ?? "SportLab Customer";
            ViewBag.CustomerPhone = TempData["CustomerPhone"] as string ?? "+380 (97) 123-45-67";
            ViewBag.DeliveryMethod = TempData["DeliveryMethod"] as string ?? "Pickup at SportLab Front Desk";
            ViewBag.DeliveryAddress = TempData["DeliveryAddress"] as string ?? "SportLab Gym Central Reception";
            ViewBag.PaymentMethod = TempData["PaymentMethod"] as string ?? "Card";

            return View(order);
        }

        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(json))
            {
                return new List<CartItem>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
            }
            catch
            {
                return new List<CartItem>();
            }
        }

        private void SaveCart(List<CartItem> cart)
        {
            var json = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString(CartSessionKey, json);
        }
    }

    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string? PhotoUrl { get; set; }
        public string Category { get; set; } = string.Empty;
        public int MaxStock { get; set; }
        public decimal Subtotal => UnitPrice * Quantity;
    }

    public class CheckoutViewModel
    {
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Total { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string DeliveryMethod { get; set; } = "pickup";
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Notes { get; set; }

        public string PaymentMethod { get; set; } = "card";
    }
}
