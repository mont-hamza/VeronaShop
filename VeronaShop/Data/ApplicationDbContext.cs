using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VeronaShop.Data.Entites;

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
        public DbSet<AdminProfile> AdminProfiles { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Promotion> Promotions { get; set; }

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
            modelBuilder.Entity<Invoice>().Property(i => i.Amount).HasColumnType("decimal(18,2)");
        }
    }
}