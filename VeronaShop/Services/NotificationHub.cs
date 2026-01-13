using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VeronaShop.Data;
using Microsoft.EntityFrameworkCore;

namespace VeronaShop.Services
{
    public class NotificationHub : Hub
    {
        // Server can call Clients.All.SendAsync("NotifyCounts", pendingCount, unviewedCount)

        public async Task<int> MarkAllViewed()
        {
            try
            {
                var http = Context.GetHttpContext();
                if (http != null)
                {
                    var scope = http.RequestServices.CreateScope();
                    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                    using var db = dbFactory.CreateDbContext();

                    // find current user id robustly
                    var user = http.User;
                    int adminId = 0;
                    try
                    {
                        var idClaim = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                            ?? user?.FindFirst("sub")
                            ?? user?.FindFirst("id");
                        if (idClaim != null && int.TryParse(idClaim.Value, out var parsed))
                        {
                            adminId = parsed;
                        }
                        else if (!string.IsNullOrEmpty(user?.Identity?.Name))
                        {
                            // fallback: lookup by username in database
                            var maybeUser = await db.Users.FirstOrDefaultAsync(u => u.UserName == user.Identity.Name);
                            if (maybeUser != null) adminId = maybeUser.Id;
                        }
                    }
                    catch { }

                    // insert NotificationView entries for notifications not yet viewed by this admin
                    var viewedIds = new List<int>();
                    if (await db.TableExistsAsync("NotificationViews"))
                        viewedIds = await db.NotificationViews.Where(nv => nv.AdminId == adminId).Select(nv => nv.NotificationId).ToListAsync();

                    var toView = await db.Notifications.Where(n => !viewedIds.Contains(n.Id)).ToListAsync();

                    // only attempt to insert if the table exists; otherwise skip and allow migrations to create it
                    if (toView.Any() && await db.TableExistsAsync("NotificationViews"))
                    {
                        foreach (var n in toView)
                        {
                            db.NotificationViews.Add(new VeronaShop.Data.Entites.NotificationView { NotificationId = n.Id, AdminId = adminId, ViewedAt = DateTimeOffset.UtcNow });
                        }
                        await db.SaveChangesAsync();
                    }

                    var pending = await db.Orders.Where(o => o.Status == VeronaShop.Data.Entites.OrderStatus.Pending).CountAsync();
                    var totalViews = 0;
                    if (await db.TableExistsAsync("NotificationViews"))
                        totalViews = await db.NotificationViews.CountAsync();
                    await Clients.All.SendCoreAsync("NotifyCounts", new object[] { pending, totalViews });

                    // compute unviewed for this admin after marking
                    var viewedIdsAfter = new List<int>();
                    if (await db.TableExistsAsync("NotificationViews"))
                        viewedIdsAfter = await db.NotificationViews.Where(nv => nv.AdminId == adminId).Select(nv => nv.NotificationId).ToListAsync();
                    var unviewedForAdmin = await db.Notifications.Where(n => !viewedIdsAfter.Contains(n.Id)).CountAsync();
                    return unviewedForAdmin;
                }
            }
            catch { }
            return 0;
        }

        public async Task<int> GetUnviewedCount()
        {
            try
            {
                var http = Context.GetHttpContext();
                if (http == null) return 0;
                var user = http.User;
                int adminId = 0;
                try
                {
                    var idClaim = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                        ?? user?.FindFirst("sub")
                        ?? user?.FindFirst("id");
                    if (idClaim != null && int.TryParse(idClaim.Value, out var parsed))
                    {
                        adminId = parsed;
                    }
                    else if (!string.IsNullOrEmpty(user?.Identity?.Name))
                    {
                        // fallback: lookup by username in database
                        using var lookupScope = http.RequestServices.CreateScope();
                        var lookupFactory = lookupScope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                        using var dbTemp = lookupFactory.CreateDbContext();
                        var maybeUser = await dbTemp.Users.FirstOrDefaultAsync(u => u.UserName == user.Identity.Name);
                        if (maybeUser != null) adminId = maybeUser.Id;
                    }
                }
                catch { }

                var scope = http.RequestServices.CreateScope();
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                using var db = dbFactory.CreateDbContext();

                var viewedIds2 = new List<int>();
                if (await db.TableExistsAsync("NotificationViews"))
                    viewedIds2 = await db.NotificationViews.Where(nv => nv.AdminId == adminId).Select(nv => nv.NotificationId).ToListAsync();
                var unviewed = await db.Notifications.Where(n => !viewedIds2.Contains(n.Id)).CountAsync();
                return unviewed;
            }
            catch { return 0; }
        }

        // Notify a specific user by their user id (int -> string)
        public async Task SendToUser(string userId, string orderNumber, string createdAt)
        {
            await Clients.User(userId).SendCoreAsync("NewNotification", new object[] { orderNumber, createdAt });
        }
    }
}
