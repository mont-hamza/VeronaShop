using Microsoft.EntityFrameworkCore;
using VeronaShop.Data.Entites;

namespace VeronaShop.Services
{
    public class CartService
    {
        private readonly ApplicationDbContext _db;
    private readonly CartSessionService _session;

        public CartService(ApplicationDbContext db, CartSessionService session)
        {
            _db = db;
            _session = session;
        }

        public async Task<Cart> GetOrCreateCartAsync(string sessionId = null, int? customerId = null)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = await _session.GetOrCreateSessionIdAsync();
            }

            var cart = await _db.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
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
                _db.Carts.Add(cart);
                await _db.SaveChangesAsync();
            }

            return cart;
        }

        public async Task AddToCartAsync(Cart cart, int productId, int quantity)
        {
            var product = await _db.Products.FindAsync(productId);
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
                _db.CartItems.Add(item);
            }

            await _db.SaveChangesAsync();
        }

        public async Task RemoveItemAsync(int cartItemId)
        {
            var item = await _db.CartItems.FindAsync(cartItemId);
            if (item == null) return;
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
        }

        public async Task MergeCartAsync(string anonymousSessionId, ApplicationUser user)
        {
            var anonCart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.SessionId == anonymousSessionId);
            if (anonCart == null) return;

            // find or create user cart
            var userCart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Customer != null && c.Customer.Email == user.Email || c.CustomerId != null && c.CustomerId == user.Id);
            if (userCart == null)
            {
                userCart = new Cart
                {
                    CustomerId = null,
                    SessionId = $"user-{user.Id}",
                    CreatedAt = DateTimeOffset.UtcNow,
                    Items = new List<CartItem>()
                };
                _db.Carts.Add(userCart);
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

            _db.CartItems.RemoveRange(anonCart.Items);
            _db.Carts.Remove(anonCart);

            await _db.SaveChangesAsync();
        }
    }
}