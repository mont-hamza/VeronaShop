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
using Microsoft.AspNetCore.Builder;
using FluentEmail.Core;
using FluentEmail.MailKitSmtp;
using FluentEmail.Razor;

var builder = WebApplication.CreateBuilder(args);

// Add localization services and resources path
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Explicitly register a typed localizer for SharedResources using the known base name and root namespace.
// This helps when implicit resource discovery doesn't locate satellite resources at runtime.
// Defer typed IStringLocalizer<SharedResources> registration until the type is available
builder.Services.AddSingleton(sp =>
{
    var factory = sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizerFactory>();
    return factory.Create("SharedResources", "VeronaShop");
});


// Add MudBlazor services
builder.Services.AddMudServices();

// Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
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
    .AddSignInManager()
    .AddDefaultTokenProviders();

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
var sendGridKey = builder.Configuration["SendGrid:ApiKey"];
var sendGridFrom = builder.Configuration["SendGrid:FromEmail"] ?? builder.Configuration["Smtp:User"] ?? "no-reply@veronashop.local";
var sendGridFromName = builder.Configuration["SendGrid:FromName"] ?? builder.Configuration["Smtp:FromName"] ?? "VeronaShop";

if (!string.IsNullOrWhiteSpace(sendGridKey))
{
    // Use official SendGrid SDK
    builder.Services.AddScoped<IEmailSender>(sp => new SendGridEmailSender(sendGridKey, sendGridFrom, sendGridFromName));
}
else
{
    var smtpHost = builder.Configuration["Smtp:Host"];
    var smtpPort = int.TryParse(builder.Configuration["Smtp:Port"], out var p) ? p : 587;
    var smtpUser = builder.Configuration["Smtp:User"] ?? string.Empty;
    var smtpPass = builder.Configuration["Smtp:Pass"] ?? string.Empty;

    if (!string.IsNullOrWhiteSpace(smtpHost))
    {
        // FluentEmail setup (Razor renderer + MailKit SMTP sender). Gmail typically requires StartTls.
        builder.Services
            .AddFluentEmail(string.IsNullOrWhiteSpace(smtpUser) ? "no-reply@veronashop.local" : smtpUser, builder.Configuration["Smtp:FromName"] ?? "VeronaShop")
            .AddRazorRenderer()
            .AddMailKitSender(new SmtpClientOptions
            {
                Server = smtpHost,
                Port = smtpPort,
                User = smtpUser,
                Password = smtpPass,
                UseSsl = false,
                SocketOptions = MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable
            });

        builder.Services.AddScoped<IEmailSender, FluentEmailSenderAdapter>();
    }
    else
    {
        // No SMTP configured: fallback to dev logger sender
        builder.Services.AddScoped<IEmailSender, DevEmailSender>();
    }
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

// Configure supported cultures for localization
var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("ar") };
var requestLocalizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = supportedCultures.ToList(),
    SupportedUICultures = supportedCultures.ToList()
};
// Allow overriding via query string (e.g. ?culture=ar)
requestLocalizationOptions.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());

// RequestLocalization will be configured later in the pipeline (after HTTPS redirection) so it runs before auth and endpoints.

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

    // Ensure Schema (best-effort)
    DbInitializer.EnsureSchema(db);

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


// Apply request localization early, before authentication/authorization and endpoint routing
app.UseRequestLocalization(requestLocalizationOptions);

// Diagnostic middleware removed - keep pipeline minimal for production behavior

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

// Endpoint to mark notifications as read for the current authenticated user
app.MapPost("/api/notifications/mark-read", async (HttpContext http, IDbContextFactory<ApplicationDbContext> dbFactory) =>
{
    try
    {
        var body = await http.Request.ReadFromJsonAsync<JsonElement?>();
        List<int> ids = null;
        if (body.HasValue && body.Value.ValueKind == JsonValueKind.Object && body.Value.TryGetProperty("ids", out var idsElem) && idsElem.ValueKind == JsonValueKind.Array)
        {
            ids = new List<int>();
            foreach (var e in idsElem.EnumerateArray())
            {
                if (e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out var v)) ids.Add(v);
            }
        }

        // resolve user info
        var user = http.User;
        int? userId = null;
        string? email = null;
        try
        {
            var idClaim = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? user?.FindFirst("sub") ?? user?.FindFirst("id");
            if (idClaim != null && int.TryParse(idClaim.Value, out var parsed)) userId = parsed;
            email = user?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        }
        catch { }

        using var db = dbFactory.CreateDbContext();

        // If we don't have an email but do have a userId, try to lookup email
        if (string.IsNullOrEmpty(email) && userId.HasValue)
        {
            try { email = await db.Users.Where(u => u.Id == userId.Value).Select(u => u.Email).FirstOrDefaultAsync(); } catch { }
        }

        var now = DateTimeOffset.UtcNow;

        if (ids == null || !ids.Any())
        {
            // mark all notifications for this email as read
            if (!string.IsNullOrEmpty(email))
            {
                var sql = @"INSERT INTO NotificationReads (NotificationId, UserId, RecipientEmail, ReadAt)
SELECT n.Id, {0}, {1}, {2}
FROM Notifications n
WHERE n.RecipientEmail = {1}
AND NOT EXISTS (SELECT 1 FROM NotificationReads r WHERE r.NotificationId = n.Id AND (r.UserId = {0} OR r.RecipientEmail = {1}));";
                try { await db.Database.ExecuteSqlRawAsync(sql, userId.HasValue ? (object)userId.Value : DBNull.Value, email, now); } catch { }
            }
        }
        else
        {
            // mark specific ids
            foreach (var nid in ids)
            {
                var sql = @"IF NOT EXISTS (SELECT 1 FROM NotificationReads r WHERE r.NotificationId = {0} AND (r.UserId = {1} OR r.RecipientEmail = {2}))
BEGIN
    INSERT INTO NotificationReads (NotificationId, UserId, RecipientEmail, ReadAt) VALUES ({0}, {1}, {2}, {3});
END";
                try { await db.Database.ExecuteSqlRawAsync(sql, nid, userId.HasValue ? (object)userId.Value : DBNull.Value, email ?? string.Empty, now); } catch { }
            }
        }

        return Results.Ok();
    }
    catch (Exception ex)
    {
        var logger = http.RequestServices.GetService<ILogger<Program>>();
        logger?.LogError(ex, "mark-read failed");
        return Results.StatusCode(500);
    }
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

    // Development-only diagnostic endpoint to inspect localization at runtime
    app.MapGet("/diag/localize", (IServiceProvider sp) =>
    {
        try
        {
            var localizer = sp.GetService(typeof(Microsoft.Extensions.Localization.IStringLocalizer<VeronaShop.SharedResources>)) as Microsoft.Extensions.Localization.IStringLocalizer;
            var siteTitle = localizer == null ? "(no-localizer)" : localizer["SiteTitle"].Value;
            var currentCulture = System.Globalization.CultureInfo.CurrentCulture.Name;
            var currentUi = System.Globalization.CultureInfo.CurrentUICulture.Name;
            var asm = typeof(VeronaShop.SharedResources).Assembly;
            var resources = asm.GetManifestResourceNames();
            return Results.Ok(new { currentCulture, currentUi, siteTitle, resources });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.ToString());
        }
    });
}

app.MapGet("/set-culture/{culture}", (string culture, HttpContext ctx) =>
{
    var cookieValue = Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
    ctx.Response.Cookies.Append(Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName,
        cookieValue,
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), SameSite = SameSiteMode.Lax });
    // redirect back to referring page (or home)
    var referer = ctx.Request.Headers["Referer"].FirstOrDefault() ?? "/";
    return Results.Redirect(referer);
});

app.Run();
