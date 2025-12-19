using Microsoft.EntityFrameworkCore;
using VeronaShop.Data;
using VeronaShop.Data.Entites;

namespace VeronaShop.Services
{
    public class CartService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly CartSessionService _session;
        private readonly System.Threading.SemaphoreSlim _mutex = new(1, 1);
        // Event to notify listeners when cart item count changes. Argument: current item count
        public event Action<int>? CartCountChanged;

        public CartService(IDbContextFactory<ApplicationDbContext> dbFactory, CartSessionService session)
        {
            _dbFactory = dbFactory;
            _session = session;
        }

        public async Task<Cart> GetOrCreateCartAsync(string sessionId = null, int? customerId = null)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = await _session.GetOrCreateSessionIdAsync();
            }

            using var db = _dbFactory.CreateDbContext();
            var cart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.SessionId == sessionId || (customerId.HasValue && c.CustomerId == customerId));

            if (cart == null)
            {
                cart = new Cart
                {
                    SessionId = sessionId,
                    CustomerId = customerId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Items = new List<CartItem>()
                };
                db.Carts.Add(cart);
                await db.SaveChangesAsync();

            // notify listeners
            try
            {
                var count = await GetCartItemCountAsync(cart.SessionId);
                CartCountChanged?.Invoke(count);
            }
            catch { }
            }

            return cart;
        }

        public async Task AddToCartAsync(Cart cart, int productId, int quantity)
        {
            using var db = _dbFactory.CreateDbContext();
            var product = await db.Products.FindAsync(productId);
            if (product == null) return;

            var item = cart.Items?.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                item.Quantity += quantity;
            }
            else
            {
                item = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Product = product,
                    Quantity = quantity,
                    UnitPrice = product.Price
                };
                db.CartItems.Add(item);
            }

            await db.SaveChangesAsync();
        }

        public async Task RemoveItemAsync(int cartItemId)
        {
            using var db = _dbFactory.CreateDbContext();
            var item = await db.CartItems.Include(ci => ci.Cart).FirstOrDefaultAsync(ci => ci.Id == cartItemId);
            if (item == null) return;
            var sessionId = item.Cart?.SessionId;
            db.CartItems.Remove(item);
            await db.SaveChangesAsync();

            try
            {
                if (!string.IsNullOrEmpty(sessionId))
                {
                    var count = await GetCartItemCountAsync(sessionId);
                    CartCountChanged?.Invoke(count);
                }
            }
            catch { }
        }

        public async Task MergeCartAsync(string anonymousSessionId, ApplicationUser user)
        {
            await _mutex.WaitAsync();
            try
            {
            using var db = _dbFactory.CreateDbContext();
            var anonCart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.SessionId == anonymousSessionId);
            if (anonCart == null) return;

            // find or create user cart
            var userCart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Customer != null && c.Customer.Email == user.Email || c.CustomerId != null && c.CustomerId == user.Id);
            if (userCart == null)
            {
                userCart = new Cart
                {
                    CustomerId = null,
                    SessionId = $"user-{user.Id}",
                    CreatedAt = DateTimeOffset.UtcNow,
                    Items = new List<CartItem>()
                };
                db.Carts.Add(userCart);
            }

            foreach (var item in anonCart.Items)
            {
                var exists = userCart.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (exists != null)
                {
                    exists.Quantity += item.Quantity;
                }
                else
                {
                    userCart.Items.Add(new CartItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });
                }
            }

            db.CartItems.RemoveRange(anonCart.Items);
            db.Carts.Remove(anonCart);

            await db.SaveChangesAsync();

                try
                {
                    var count = await GetCartItemCountAsync(userCart.SessionId);
                    CartCountChanged?.Invoke(count);
                }
                catch { }
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<int> GetCartItemCountAsync(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return 0;
            using var db = _dbFactory.CreateDbContext();
            var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.SessionId == sessionId);
            if (cart == null) return 0;
            return cart.Items?.Sum(i => i.Quantity) ?? 0;
        }
    }
}