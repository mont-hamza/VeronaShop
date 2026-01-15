using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VeronaShop.Data.Entites;
using System.Data;
using System.Threading.Tasks;

namespace VeronaShop.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Orders> Orders { get; set; }
        public DbSet<OrderProduct> OrderProducts { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<Carrier> Carriers { get; set; }
        public DbSet<AdminProfile> AdminProfiles { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Data.Entites.Notification> Notifications { get; set; }
        public DbSet<Data.Entites.NotificationView> NotificationViews { get; set; }
        public DbSet<Data.Entites.NotificationRead> NotificationReads { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal precisions to avoid SQL truncation warnings
            modelBuilder.Entity<CartItem>().Property(ci => ci.UnitPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<OrderProduct>().Property(op => op.UnitPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Orders>().Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Orders>().Property(o => o.ShippingCost).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>().Property(p => p.DiscountPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<ProductImage>().Property(pi => pi.Url).IsRequired();
            modelBuilder.Entity<Invoice>().Property(i => i.Amount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Data.Entites.Notification>().Property(n => n.RecipientEmail).HasMaxLength(256);
            modelBuilder.Entity<Data.Entites.NotificationView>().HasIndex(nv => new { nv.NotificationId, nv.AdminId }).IsUnique(false);
            modelBuilder.Entity<Data.Entites.NotificationRead>().HasIndex(nr => new { nr.NotificationId, nr.UserId }).IsUnique(false);
        }

        public async Task<bool> TableExistsAsync(string tableName)
        {
            try
            {
                var conn = this.Database.GetDbConnection();
                if (conn.State == ConnectionState.Closed)
                    await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = @name AND s.name = 'dbo'";
                var p = cmd.CreateParameter();
                p.ParameterName = "@name";
                p.Value = tableName;
                cmd.Parameters.Add(p);
                var res = await cmd.ExecuteScalarAsync();
                if (res == null || res == DBNull.Value) return false;
                return Convert.ToInt32(res) > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}