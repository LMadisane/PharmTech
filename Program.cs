using PharmTech.Data;
using Microsoft.EntityFrameworkCore;
using PharmTech.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<PharmTechContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 45))
    ));

// Controllers
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Authentication (RBAC)
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/account/accessdenied";
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        // Prevent redirect to login page for API requests
        options.Events.OnRedirectToLogin = context =>
        {
            // If the request is to an API endpoint, return 401 Unauthorized
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"success\":false,\"message\":\"Unauthorized\"}");
            }
            // Otherwise, redirect to login page for normal MVC requests
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

// Antiforgery cookie fix
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; //Fixes the http/https mismatch
});

// Authorization (Role-based)
builder.Services.AddAuthorization();

// Login session (used in your login system)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Background service
builder.Services.AddHostedService<LowStockBackgroundService>();

var app = builder.Build();

// Initializing the Database
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<PharmTechContext>();
        logger.LogInformation("Ensuring database is created...");
        db.Database.Migrate();
        logger.LogInformation("Database ensured/created.");

        //Seeding default Admin account if no users exist
        await DbSeeder.SeedAsync(db);
        logger.LogInformation("Database seeding completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}

// Middleware Pipeline

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Session must come before Authentication and Authorization
app.UseSession();

// Authentication
app.UseAuthentication();

// User Authorization
app.UseAuthorization();

// ========== ROUTES ===========

// Login route (Default)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Alerts
app.MapControllerRoute(
    name: "alerts",
    pattern: "alerts/{action=Index}/{id?}",
    defaults: new { controller = "Alerts" });

// Audit Logs
app.MapControllerRoute(
    name: "audit",
    pattern: "audit/{action=Index}/{id?}",
    defaults: new { controller = "AuditLogView" });

// Dashboard
app.MapControllerRoute(
    name: "dashboard",
    pattern: "dashboard/{action=Index}/{id?}",
    defaults: new { controller = "DashboardView" });

// Dispensing
app.MapControllerRoute(
    name: "dispensing",
    pattern: "dispensing/{action=Index}/{id?}",
    defaults: new { controller = "DispensingView" });

// Drug Returns
app.MapControllerRoute(
    name: "returns",
    pattern: "returns/{action=Index}/{id?}",
    defaults: new { controller = "DrugReturnsView" });

// Facilities
app.MapControllerRoute(
    name: "facilities",
    pattern: "facilities/{action=Index}/{id?}",
    defaults: new { controller = "Facility" });

// Inventory
app.MapControllerRoute(
    name: "inventory",
    pattern: "inventory/{action=Index}/{id?}",
    defaults: new { controller = "Inventory" });

// Medicines
app.MapControllerRoute(
    name: "medicines",
    pattern: "medicines/{action=Index}/{id?}",
    defaults: new { controller = "Medicine" });

// Order Requests
app.MapControllerRoute(
    name: "orders",
    pattern: "orders/{action=Index}/{id?}",
    defaults: new { controller = "OrderRequest" });

// Prescriptions
app.MapControllerRoute(
    name: "prescriptions",
    pattern: "prescriptions/{action=Index}/{id?}",
    defaults: new { controller = "PrescriptionView" });

// Receipts
app.MapControllerRoute(
    name: "receipts",
    pattern: "receipts/{action=Index}/{id?}",
    defaults: new { controller = "ReceiptView" });

// Suppliers
app.MapControllerRoute(
    name: "suppliers",
    pattern: "suppliers/{action=Index}/{id?}",
    defaults: new { controller = "SupplierView" });

// User Management
app.MapControllerRoute(
    name: "users",
    pattern: "users/{action=Index}/{id?}",
    defaults: new { controller = "UserManagementView" });

app.Run();