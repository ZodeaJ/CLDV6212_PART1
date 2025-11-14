using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ABCRetailers.Data;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly IFunctionsApi _api;

        public CartController(AuthDbContext db, IFunctionsApi api)
        {
            _db = db;
            _api = api;
        }

        // Displays the current user's shopping cart with product details
        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            // Retrieve all cart items for the current user
            var cartItems = await _db.Cart
                .Where(c => c.CustomerUsername == username)
                .ToListAsync();

            var viewModelList = new List<CartItemViewModel>();

            // Enhance cart items with product information from external API
            foreach (var item in cartItems)
            {
                var product = await _api.GetProductAsync(item.ProductId);
                if (product == null) continue;

                viewModelList.Add(new CartItemViewModel
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = (decimal)product.Price
                });
            }

            return View(new CartPageViewModel { Items = viewModelList });
        }

        // Adds a product to the user's shopping cart
        public async Task<IActionResult> Add(string productId)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(productId))
                return RedirectToAction("Index", "Product");

            // Verify product exists before adding to cart
            var product = await _api.GetProductAsync(productId);
            if (product == null)
                return NotFound();

            // Check if product already exists in cart
            var existing = await _db.Cart.FirstOrDefaultAsync(c =>
                c.ProductId == productId && c.CustomerUsername == username);

            if (existing != null)
            {
                existing.Quantity += 1;
            }
            else
            {
                _db.Cart.Add(new Cart
                {
                    CustomerUsername = username,
                    ProductId = productId,
                    Quantity = 1
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"{product.ProductName} added to cart.";
            return RedirectToAction("Index", "Product");
        }

        // Processes the checkout by creating orders and clearing the cart
        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            // Retrieve customer details from external system
            var customer = await _api.GetCustomerByUsernameAsync(username);
            if (customer == null)
            {
                TempData["Error"] = "Customer not found.";
                return RedirectToAction("Index");
            }

            // Get all items from user's cart
            var cartItems = await _db.Cart
                .Where(c => c.CustomerUsername == username)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            // Create orders for each cart item in external system
            foreach (var item in cartItems)
            {
                await _api.CreateOrderAsync(customer.CustomerId, item.ProductId, item.Quantity);
            }

            // Clear local cart after successful order creation
            _db.Cart.RemoveRange(cartItems);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Order placed successfully!";
            return RedirectToAction("Confirmation");
        }

        // Displays order confirmation page after successful checkout
        public IActionResult Confirmation()
        {
            ViewBag.Message = TempData["SuccessMessage"] ?? "Thank you for your purchase!";
            return View();
        }

        // Removes a specific product from the shopping cart
        [HttpPost]
        public async Task<IActionResult> Remove(string productId)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Index");

            var item = await _db.Cart.FirstOrDefaultAsync(c =>
                c.CustomerUsername == username && c.ProductId == productId);

            if (item != null)
            {
                _db.Cart.Remove(item);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Item removed from cart.";
            }

            return RedirectToAction("Index");
        }

        // Updates quantities for multiple items in the shopping cart
        [HttpPost]
        public async Task<IActionResult> UpdateQuantities(List<CartItemViewModel> items)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Index");

            // Update quantities for each item in the cart
            foreach (var item in items)
            {
                var cartItem = await _db.Cart.FirstOrDefaultAsync(c =>
                    c.CustomerUsername == username && c.ProductId == item.ProductId);

                if (cartItem != null)
                {
                    cartItem.Quantity = item.Quantity;
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Cart updated successfully.";
            return RedirectToAction("Index");
        }
    }
}