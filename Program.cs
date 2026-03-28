using PharmTech.Data;
using Microsoft.EntityFrameworkCore;
using PharmTech.Services;

var builder = WebApplication.CreateBuilder(args);

// Register Services FIRST
builder.Services.AddDbContext<PharmTechContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 45))
    ));

builder.Services.AddControllersWithViews();


// Background service to check low stock daily
builder.Services.AddHostedService<LowStockBackgroundService>();

var app = builder.Build();

// Ensure database is created when the application starts (safe for empty dev DB)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<PharmTech.Data.PharmTechContext>();
        logger.LogInformation("Ensuring database is created...");
        db.Database.EnsureCreated();
        logger.LogInformation("Database ensured/created.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}

// Configure Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Map Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// No SignalR hub mapping (background service will run without SignalR)

app.Run();