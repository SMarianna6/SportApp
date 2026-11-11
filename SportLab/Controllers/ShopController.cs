using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SportLab.Controllers
{
    public class ShopController : Controller
    {
        private readonly SportLabContext _context;

        public ShopController(SportLabContext context)
        {
            _context = context;
        }

        public IActionResult Index(string? category, string? sort, string? search)
        {
            // Removed the SeedDefaultProducts() check

            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(term)) ||
                    (p.Description != null && p.Description.ToLower().Contains(term))
                );
            }

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.Category != null && p.Category.ToLower() == category.ToLower());
            }

            var products = sort switch
            {
                "price_asc" => query.OrderBy(p => p.Price).ToList(),
                "price_desc" => query.OrderByDescending(p => p.Price).ToList(),
                "name" => query.OrderBy(p => p.Name).ToList(),
                "stock" => query.OrderByDescending(p => p.StockQuantity).ToList(),
                _ => query.OrderByDescending(p => p.Id).ToList()
            };

            ViewBag.CurrentCategory = string.IsNullOrWhiteSpace(category) ? "all" : category.ToLower();
            ViewBag.CurrentSort = sort ?? "default";
            ViewBag.CurrentSearch = search ?? "";

            return View(products);
        }

        public IActionResult Details(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            var related = _context.Products
                .Where(p => p.Id != product.Id && p.Category == product.Category)
                .Take(4)
                .ToList();

            ViewBag.RelatedProducts = related;
            return View(product);
        }
    }
}