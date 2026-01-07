using System.Collections.Concurrent;
using VeronaShop.Data;
using VeronaShop.Data.Entites;
using Microsoft.EntityFrameworkCore;

namespace VeronaShop.Services
{
    public class OrdersPrefetchService
    {
        private record PrefetchKey(int UserId, int Page, int PageSize, string SortBy, OrderStatus? Status);
        private class PrefetchValue
        {
            public List<Orders> Orders { get; set; } = new();
            public Dictionary<int, Product> ProductLookup { get; set; } = new();
            public int TotalOrders { get; set; }
            public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        }

        private readonly ConcurrentDictionary<string, PrefetchValue> _cache = new();
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public OrdersPrefetchService(IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        private static string Key(PrefetchKey k) => $"{k.UserId}:{k.Page}:{k.PageSize}:{k.SortBy}:{k.Status}";

        public async Task PrefetchAsync(int userId, int page, int pageSize, string sortBy, OrderStatus? status)
        {
            var key = Key(new PrefetchKey(userId, page, pageSize, sortBy ?? "", status));
            try
            {
                using var db = _dbFactory.CreateDbContext();
                var customer = await db.Customers.FirstOrDefaultAsync(c => c.IdentityUserId == userId);
                IQueryable<Orders> baseQuery = db.Orders.Where(o => o.Customer != null);
                if (customer != null)
                    baseQuery = baseQuery.Where(o => o.CustomerId == customer.Id);
                else
                    baseQuery = baseQuery.Where(o => o.Customer != null && o.Customer.IdentityUserId == userId);

                // apply simple filter/sort if provided
                if (status.HasValue)
                    baseQuery = baseQuery.Where(o => o.Status == status.Value);

                int total = await baseQuery.CountAsync();
                var q = baseQuery.OrderByDescending(o => o.OrderDate).Skip(page * pageSize).Take(pageSize).Include(o => o.OrderProducts).AsNoTracking();
                var orders = await q.ToListAsync();

                var productIds = orders.SelectMany(o => o.OrderProducts).Select(op => op.ProductId).Distinct().ToList();
                var productLookup = new Dictionary<int, Product>();
                if (productIds.Any())
                {
                    var prods = await db.Products.Where(p => productIds.Contains(p.Id)).Include(p => p.Images).AsNoTracking().ToListAsync();
                    productLookup = prods.ToDictionary(p => p.Id, p => p);
                }

                var val = new PrefetchValue { Orders = orders, ProductLookup = productLookup, TotalOrders = total, CreatedAt = DateTimeOffset.UtcNow };
                _cache[ key ] = val;
            }
            catch
            {
                // swallow - prefetch best-effort
            }
        }

        public bool TryGetPrefetch(int userId, int page, int pageSize, string sortBy, OrderStatus? status, out List<Orders> orders, out Dictionary<int, Product> productLookup, out int totalOrders)
        {
            var key = Key(new PrefetchKey(userId, page, pageSize, sortBy ?? "", status));
            if (_cache.TryGetValue(key, out var v))
            {
                orders = v.Orders;
                productLookup = v.ProductLookup;
                totalOrders = v.TotalOrders;
                return true;
            }
            orders = new List<Orders>();
            productLookup = new Dictionary<int, Product>();
            totalOrders = 0;
            return false;
        }
    }
}
