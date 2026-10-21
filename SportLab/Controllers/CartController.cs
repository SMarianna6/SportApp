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
        public IActionResult Checkout(CheckoutViewModel model)
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

            // Find or create customer user
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

            // Create Order
            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalAmount,
                Status = "Confirmed"
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            // Create OrderDetails & decrease stock
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

            // Clear session cart
            HttpContext.Session.Remove(CartSessionKey);

            // Save order metadata in TempData for OrderSuccess page
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
