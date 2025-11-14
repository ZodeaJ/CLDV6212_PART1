using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ABCRetailers.Controllers
{
    public class HomeController : Controller
    {
        private readonly IFunctionsApi _api;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IFunctionsApi api, ILogger<HomeController> logger)
        {
            _api = api;
            _logger = logger;
        }

        // Public homepage displaying featured products
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _api.GetProductsAsync() ?? new List<Product>();
                var vm = new HomeViewModel
                {
                    FeaturedProducts = products.Take(8).ToList(),
                    ProductCount = products.Count
                };
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page");
                var vm = new HomeViewModel
                {
                    FeaturedProducts = new List<Product>(),
                    ProductCount = 0
                };
                return View(vm);
            }
        }

        // Administrative dashboard
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            try
            {
                var customers = await _api.GetCustomersAsync() ?? new List<Customer>();
                var orders = await _api.GetOrdersAsync() ?? new List<Order>();

                var model = new
                {
                    TotalCustomers = customers.Count,
                    TotalOrders = orders.Count
                };

                ViewBag.AdminSummary = model;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Admin Dashboard data.");
                TempData["Error"] = "Could not load Admin Dashboard data.";
                return View();
            }
        }

        // Customer dashboard
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CustomerDashboard()
        {
            try
            {
                var userEmail = User.Identity?.Name;
                ViewBag.UserEmail = userEmail;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Customer Dashboard data.");
                TempData["Error"] = "Could not load your dashboard. Please try again.";
                return View();
            }
        }

        // Privacy policy and data protection information
        [AllowAnonymous]
        public IActionResult Privacy() => View();

        // Global error handling page for application exceptions
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
            => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}