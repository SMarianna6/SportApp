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
            if (!_context.Products.Any())
            {
                SeedDefaultProducts();
            }

            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p => 
                    (p.Name != null && p.Name.ToLower().Contains(term)) ||
                    (p.Description != null && p.Description.ToLower().Contains(term)) //Lab 5
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

        private void SeedDefaultProducts()
        {
            var defaultProducts = new List<Product>
            {
                new Product
                {
                    Name = "Optimum 100% Whey Gold Standard",
                    Category = "Nutrition",
                    Rating = "5.0",
                    Description = "24g of high-quality whey protein per serving with whey protein isolate as the primary ingredient. Double Rich Chocolate flavor, 2.27kg tub.",
                    Price = 1850,
                    StockQuantity = 28,
                    PhotoUrl = "https://images.unsplash.com/photo-1579722821273-0f6c7d44362f?w=600&h=450&fit=crop"
                },
                new Product
                {
                    Name = "Essential BCAA Energy 2:1:1",
                    Category = "Nutrition",
                    Rating = "4.8",
                    Description = "Instantized Branched Chain Amino Acids with electrolytes for sustained muscle endurance and post-workout recovery. Green Apple flavor, 400g.",
                    Price = 720,
                    StockQuantity = 35,
                    PhotoUrl = "https://images.unsplash.com/photo-1593095948071-474c5cc2989d?w=600&h=450&fit=crop"
                },
                new Product
                {
                    Name = "SportLab Pro Shaker 750ml",
                    Category = "Accessories",
                    Rating = "4.9",
                    Description = "BPA-free premium matte shaker bottle with surgical stainless steel blending ball, leak-proof flip cap, and measurement markings.",
                    Price = 320,
                    StockQuantity = 65,
                    PhotoUrl = "https://images.unsplash.com/photo-1693996045838-980674653385?w=600&h=450&fit=crop"
                },
                new Product
                {
                    Name = "Elite Duffel Sport Bag",
                    Category = "Apparel",
                    Rating = "4.9",
                    Description = "Spacious water-resistant training bag with separate ventilated shoe compartment, padded shoulder strap, and multiple accessory pockets.",
                    Price = 1250,
                    StockQuantity = 18,
                    PhotoUrl = "https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=600&h=450&fit=crop"
                },
                new Product
                {
                    Name = "Creatine Monohydrate Ultra Pure",
                    Category = "Nutrition",
                    Rating = "5.0",
                    Description = "100% pure micronized creatine monohydrate powder to increase strength, power, and lean muscle mass. Unflavored, 500g (100 servings).",
                    Price = 680,
                    StockQuantity = 42,
                    PhotoUrl = "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=600&h=450&fit=crop"
                },
                new Product
                {
                    Name = "SportLab Dry-Fit Performance T-Shirt",
                    Category = "Apparel",
                    Rating = "4.7",
                    Description = "Breathable moisture-wicking athletic shirt designed for intense workout sessions. Ergonomic athletic cut with anti-chafing seams.",
                    Price = 650,
                    StockQuantity = 25,
                    PhotoUrl = "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?w=600&h=450&fit=crop"
                },
                new Product
                {
                    Name = "Resistance Loop Bands Set (5 Levels)",
                    Category = "Equipment",
                    Rating = "4.8",
                    Description = "Set of 5 natural latex loop resistance bands with carry pouch and exercise guide for stretching, mobility, and glute activation workouts.",
                    Price = 420,
                    StockQuantity = 55,
                    PhotoUrl = "https://images.unsplash.com/photo-1598289431512-b97b0917affc?w=600&h=450&fit=crop"
                },
                new Product
                {
                    Name = "Deep Tissue High-Density Foam Roller",
                    Category = "Equipment",
                    Rating = "4.9",
                    Description = "Trigger point myofascial release foam roller for muscle recovery, soreness relief, and mobility workouts. 33 cm length.",
                    Price = 590,
                    StockQuantity = 22,
                    PhotoUrl = "https://images.unsplash.com/photo-1601422407692-ec4eeec1d9b3?w=600&h=450&fit=crop"
                }
            };

            _context.Products.AddRange(defaultProducts);
            _context.SaveChanges();
        }
    }
}
