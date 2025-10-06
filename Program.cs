using System.Globalization;
using ABCRetailers.Services;

namespace ABCRetailers
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Register HttpClient and Function API client
            builder.Services.AddHttpClient("Functions", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["Functions:BaseUrl"] ?? "http://localhost:7030/api");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            builder.Services.AddScoped<IFunctionsApi, FunctionsApiClient>();

            // Register Azure Storage Service
            builder.Services.AddScoped<IAzureStorageService, AzureStorageService>();

            // Add logging
            builder.Services.AddLogging();

            // Set culture for decimal handling (FIXES PRICE ISSUE)
            var culture = new CultureInfo("en-ZA");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}