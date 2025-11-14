using System.Security.Claims;
using ABCRetailers.Data;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Controllers
{
    public class LoginController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<LoginController> _logger;

        public LoginController(AuthDbContext db, IFunctionsApi functionsApi, ILogger<LoginController> logger)
        {
            _db = db;
            _functionsApi = functionsApi;
            _logger = logger;
        }

        // Hashes the provided password using SHA256 algorithm
        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // Displays the login form to the user
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // Processes user login authentication and establishes session
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Verify user credentials against database
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
                if (user == null || user.PasswordHash != model.Password)
                {
                    ViewBag.Error = "Invalid username or password.";
                    return View(model);
                }

                // Retrieve customer information from external API
                var customer = await _functionsApi.GetCustomerByUsernameAsync(user.Username);
                if (customer == null)
                {
                    _logger.LogWarning("No matching customer found in Azure for username {Username}", user.Username);
                    ViewBag.Error = "No customer record found in the system.";
                    return View(model);
                }

                // Create authentication claims for the user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("CustomerId", customer.CustomerId)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // Sign in user with authentication cookie
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                    });

                // Store user session data
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);
                HttpContext.Session.SetString("CustomerId", customer.CustomerId);

                // Redirect user based on role or return URL
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return user.Role switch
                {
                    "Admin" => RedirectToAction("AdminDashboard", "Home"),
                    _ => RedirectToAction("CustomerDashboard", "Home")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected login error for user {Username}", model.Username);
                ViewBag.Error = "Unexpected error occurred during login. Please try again later.";
                return View(model);
            }
        }

        // Displays the user registration form
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // Processes new user registration and creates accounts
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check for existing username
            var exists = await _db.Users.AnyAsync(u => u.Username == model.Username);
            if (exists)
            {
                ViewBag.Error = "Username already exists.";
                return View(model);
            }

            try
            {
                // Create local user account in database
                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = model.Password,
                    Role = model.Role
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                // Attempt to create customer record in external system
                try
                {
                    var customer = new Customer
                    {
                        Username = model.Username,
                        Name = model.FirstName,
                        Surname = model.LastName,
                        Email = model.Email,
                        ShippingAddress = model.ShippingAddress
                    };
                    await _functionsApi.CreateCustomerAsync(customer);
                }
                catch (Exception funcEx)
                {
                    _logger.LogWarning(funcEx, "Failed to create customer in Azure Functions, but local user was created");
                    // Continue with local user creation despite external system failure
                }

                TempData["Success"] = "Registration successful! Please log in.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for user {Username}", model.Username);
                ViewBag.Error = "Could not complete registration. Please try again later.";
                return View(model);
            }
        }

        // Logs out the current user and clears session data
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        // Displays access denied page for unauthorized requests
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();
    }
}