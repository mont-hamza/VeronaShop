using MudBlazor.Services;
using VeronaShop.Components;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using VeronaShop.Data.Entites;
using VeronaShop.Services;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add EF Core and Identity
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Enable retry on failure for transient faults
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
        // Increase command timeout for long-running commands
        sqlOptions.CommandTimeout(60);
    }));

builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options => { options.SignIn.RequireConfirmedAccount = false; })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register server-side Blazor to allow configuring circuit options (detailed errors)
builder.Services.AddServerSideBlazor().AddCircuitOptions(options =>
{
    // enable detailed errors in Development or when configured in appsettings
    var configured = builder.Configuration.GetValue<bool>("DetailedError", false);
    options.DetailedErrors = builder.Environment.IsDevelopment() || configured;
});

// App services
builder.Services.AddScoped<IEmailSender, DevEmailSender>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<CartSessionService>();

// Optional: register SmtpEmailSender with real settings in production
// builder.Services.AddSingleton<IEmailSender>(new SmtpEmailSender("smtp.example.com", 587, "user@example.com", "password"));

var app = builder.Build();

// Drop and recreate database from migrations, then seed roles and initial admin (only in Development)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var env = services.GetRequiredService<IWebHostEnvironment>();
    var db = services.GetRequiredService<ApplicationDbContext>();

    if (env.IsDevelopment())
    {
        // Ensure DB is recreated from migrations (destructive)
        db.Database.EnsureDeleted();
        db.Database.Migrate();
    }
    else
    {
        // In non-development, just apply pending migrations (non-destructive)
        db.Database.Migrate();
    }

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
    // run seed
    IdentitySeed.SeedAsync(userManager, roleManager).GetAwaiter().GetResult();

    // DEV-ONLY: allow resetting admin password once via file trigger
    try
    {
        var resetFile = Path.Combine(AppContext.BaseDirectory, "reset-admin.json");
        if (File.Exists(resetFile) && app.Environment.IsDevelopment())
        {
            var json = File.ReadAllText(resetFile);
            var doc = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (doc != null && doc.TryGetValue("password", out var newPassword))
            {
                var admin = userManager.FindByEmailAsync("admin@veronashop.local").GetAwaiter().GetResult();
                if (admin != null)
                {
                    var token = userManager.GeneratePasswordResetTokenAsync(admin).GetAwaiter().GetResult();
                    var res = userManager.ResetPasswordAsync(admin, token, newPassword).GetAwaiter().GetResult();
                    if (res.Succeeded)
                    {
                        File.Delete(resetFile); // single-use
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        // swallow; this is developer convenience only
        Console.Error.WriteLine($"reset-admin failed: {ex}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Show developer exception page in Development so full stack traces are visible
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// Global middleware to log unhandled exceptions to console and a simple log file
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetService<ILogger<Program>>();
        logger?.LogError(ex, "Unhandled exception in request");
        try
        {
            System.IO.Directory.CreateDirectory("logs");
            System.IO.File.AppendAllText("logs/errors.log", $"{DateTime.UtcNow:o} {ex}\n\n");
        }
        catch { /* best-effort logging only */ }
        throw;
    }
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// POST endpoint to perform cookie sign-in from browser form (ensures Set-Cookie is sent to client)
app.MapPost("/auth/login", async (HttpContext http, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, VeronaShop.Services.CartService cartService) =>
{
    var form = await http.Request.ReadFormAsync();
    var userField = form["user"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].FirstOrDefault() ?? "/";
    var sessionId = form["sessionId"].FirstOrDefault();

    ApplicationUser user = null;
    if (userField.Contains("@"))
        user = await userManager.FindByEmailAsync(userField);
    else
        user = await userManager.FindByNameAsync(userField);

    if (user == null)
    {
        http.Response.Redirect($"/account/login?error=invalid");
        return Results.Redirect("/account/login?error=invalid");
    }

    var result = await signInManager.PasswordSignInAsync(user.UserName, password, false, false);
    if (result.Succeeded)
    {
        // Merge cart if sessionId provided
        if (!string.IsNullOrEmpty(sessionId))
        {
            try { await cartService.MergeCartAsync(sessionId, user); } catch { }
        }
        http.Response.Redirect(returnUrl);
        return Results.Redirect(returnUrl);
    }

    http.Response.Redirect($"/account/login?error=invalid");
    return Results.Redirect("/account/login?error=invalid");
});

app.Run();
