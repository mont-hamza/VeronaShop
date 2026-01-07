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
using VeronaShop.Data;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add EF Core and Identity
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register a DbContext factory for creating short-lived DbContext instances
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(60);
    }));

// Provide a scoped ApplicationDbContext that is created from the factory so
// other libraries (Identity) that expect a scoped DbContext can still get one.
builder.Services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options => { options.SignIn.RequireConfirmedAccount = false; })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();

// Register MudBlazor dialog services
// MudBlazor already registered by AddMudServices(); dialog service will be available via DI

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

// SignalR hub for notifications
builder.Services.AddSignalR();

// App services
builder.Services.AddScoped<IEmailSender, DevEmailSender>();
// Configure SMTP email sender only if SMTP settings are provided; otherwise keep DevEmailSender
var smtpHost = builder.Configuration["Smtp:Host"];
if (!string.IsNullOrEmpty(smtpHost))
{
    var smtpPort = int.TryParse(builder.Configuration["Smtp:Port"], out var p) ? p : 587;
    var smtpUser = builder.Configuration["Smtp:User"] ?? string.Empty;
    var smtpPass = builder.Configuration["Smtp:Pass"] ?? string.Empty;
    // Register concrete SmtpEmailSender as the IEmailSender implementation
    builder.Services.AddSingleton<IEmailSender>(new SmtpEmailSender(smtpHost, smtpPort, smtpUser, smtpPass));
}
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<CartSessionService>();
builder.Services.AddSingleton<VeronaShop.Services.OrdersPrefetchService>();
// Background queue for long-running or non-critical tasks (email notifications)
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<QueuedHostedService>();
// MudBlazor snackbar provider is registered by AddMudServices(); no manual registration required here.

// Optional: register SmtpEmailSender with real settings in production
// builder.Services.AddSingleton<IEmailSender>(new SmtpEmailSender("smtp.example.com", 587, "user@example.com", "password"));

var app = builder.Build();

// Configure site-wide culture to use LYD for currency formatting
var culture = new CultureInfo("en-LY");
// Ensure currency symbol is LYD
culture.NumberFormat.CurrencySymbol = "LYD";
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// Drop and recreate database from migrations, then seed roles and initial admin (only in Development)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var env = services.GetRequiredService<IWebHostEnvironment>();
    var db = services.GetRequiredService<ApplicationDbContext>();

    if (env.IsDevelopment())
    {
        // In development, apply migrations but do NOT drop the database so created data persists across runs.
        // Previously the DB was deleted on every start which removed user/product data.
        db.Database.Migrate();
    }
    else
    {
        // In non-development, apply pending migrations (non-destructive)
        db.Database.Migrate();
    }

    // Ensure Notifications table exists for runtime-written notification records (best-effort).
    try
    {
        var ensureSql = @"IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'Notifications' AND s.name = 'dbo')
BEGIN
    CREATE TABLE [dbo].[Notifications](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [OrderNumber] NVARCHAR(max) NOT NULL,
        [RecipientEmail] NVARCHAR(256) NOT NULL,
        [Status] INT NOT NULL,
        [AttemptedAt] DATETIMEOFFSET NULL,
        [ErrorMessage] NVARCHAR(max) NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL
    );
END";

        try { db.Database.ExecuteSqlRaw(ensureSql); } catch { }
        // Notification views are tracked in NotificationViews table per admin; migrations should create that table.
    }
    catch { }

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

// Logout endpoint that performs a full HTTP sign-out so cookies can be removed on the response
app.MapGet("/auth/logout", async (HttpContext http, SignInManager<ApplicationUser> signInManager, ILogger<Program> logger) =>
{
    try
    {
        await signInManager.SignOutAsync();
        logger.LogInformation("Logged out via /auth/logout");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Logout failed");
    }

    http.Response.Redirect("/");
    return Results.Redirect("/");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// SignalR hub mapping
app.MapHub<VeronaShop.Services.NotificationHub>("/hubs/notifications");

// POST endpoint to perform cookie sign-in from browser form (ensures Set-Cookie is sent to client)
app.MapPost("/auth/login", async (HttpContext http, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, VeronaShop.Services.CartService cartService, ILogger<Program> logger) =>
{
    var form = await http.Request.ReadFormAsync();
    var userField = form["user"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].FirstOrDefault() ?? "/";
    var sessionId = form["sessionId"].FirstOrDefault();
    // mask user field for logging (never log passwords)
    string MaskUser(string u)
    {
        if (string.IsNullOrEmpty(u)) return u;
        if (u.Contains("@"))
        {
            var parts = u.Split('@', 2);
            return "***@" + parts[1];
        }
        return u.Length <= 2 ? "**" : u.Substring(0, 2) + "***";
    }

    logger.LogInformation("/auth/login called for {UserMask} (session present={HasSession})", MaskUser(userField), !string.IsNullOrEmpty(sessionId));

    ApplicationUser user = null;
    if (userField.Contains("@"))
        user = await userManager.FindByEmailAsync(userField);
    else
        user = await userManager.FindByNameAsync(userField);

    if (user == null)
    {
        logger.LogWarning("Login failed: user not found for {UserMask}", MaskUser(userField));
        http.Response.Redirect($"/account/login?error=invalid");
        return Results.Redirect("/account/login?error=invalid");
    }

    try
    {
        var result = await signInManager.PasswordSignInAsync(user.UserName, password, false, false);
        if (result.Succeeded)
        {
            // Merge cart if sessionId provided
            if (!string.IsNullOrEmpty(sessionId))
            {
                try
                {
                    await cartService.MergeCartAsync(sessionId, user);
                    logger.LogInformation("Merged cart for user {UserMask} from session", MaskUser(userField));
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Cart merge failed for {UserMask}", MaskUser(userField));
                }
            }
            logger.LogInformation("Login succeeded for {UserMask}", MaskUser(userField));
            http.Response.Redirect(returnUrl);
            return Results.Redirect(returnUrl);
        }

        logger.LogWarning("SignInManager result for {UserMask}: Succeeded={Succeeded}, IsLockedOut={Locked}, IsNotAllowed={NotAllowed}, RequiresTwoFactor={TwoFactor}", MaskUser(userField), result.Succeeded, result.IsLockedOut, result.IsNotAllowed, result.RequiresTwoFactor);
        http.Response.Redirect($"/account/login?error=invalid");
        return Results.Redirect("/account/login?error=invalid");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Exception during /auth/login for {UserMask}", MaskUser(userField));
        http.Response.Redirect($"/account/login?error=error");
        return Results.Redirect("/account/login?error=error");
    }
});

// Development-only debug endpoint to set customer phone directly and return current value
if (app.Environment.IsDevelopment())
{
    app.MapPost("/debug/set-customer-phone", async (HttpContext http, IDbContextFactory<ApplicationDbContext> dbFactory, ILogger<Program> logger) =>
    {
        try
        {
            var doc = await http.Request.ReadFromJsonAsync<JsonElement>();
            if (!doc.TryGetProperty("id", out var idElem) || !doc.TryGetProperty("phone", out var phoneElem))
                return Results.BadRequest(new { error = "missing id or phone" });

            var id = idElem.GetInt32();
            var phone = phoneElem.GetString() ?? string.Empty;

            using var db = dbFactory.CreateDbContext();
            var rows = await db.Database.ExecuteSqlRawAsync("UPDATE Customers SET Phone = {0}, UpdatedAt = {1} WHERE Id = {2}", phone, DateTimeOffset.UtcNow, id);
            var raw = await db.Customers.AsNoTracking().Where(c => c.Id == id).Select(c => c.Phone).FirstOrDefaultAsync();
            logger.LogInformation("/debug/set-customer-phone id={Id} rows={Rows} rawPhone='{Phone}'", id, rows, raw);
            return Results.Ok(new { rows, phone = raw });
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "debug set phone failed");
            return Results.StatusCode(500);
        }
    });
}

app.Run();
