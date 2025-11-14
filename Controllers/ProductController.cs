using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    [Authorize] 
    public class ProductController : Controller
    {
        private readonly IFunctionsApi _api;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IFunctionsApi api, ILogger<ProductController> logger)
        {
            _api = api;
            _logger = logger;
        }

        // Both Admin and Customer can view products
        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> Index()
        {
            var products = await _api.GetProductsAsync();
            return View(products);
        }

        // Only Admin can Create a product
        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        // Only Admin can POST a product
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(product);
            try
            {
                var created = await _api.CreateProductAsync(product, imageFile);
                TempData["Success"] = $"Product '{created.ProductName}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                return View(product);
            }
        }

        // Only Admin can Edit a product
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var product = await _api.GetProductAsync(id);
            return product is null ? NotFound() : View(product);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(product);
            try
            {
                var updated = await _api.UpdateProductAsync(product.ProductId, product, imageFile);
                TempData["Success"] = $"Product '{updated.ProductName}' updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                return View(product);
            }
        }

        // Only Admin can Delete a product
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _api.DeleteProductAsync(id);
                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}