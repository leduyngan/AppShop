using App.Models.Contacts;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using App.Models.Blog;
using App.Models.Product;

namespace App.Models
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            base.OnConfiguring(builder);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (tableName.StartsWith("AspNet"))
                {
                    entityType.SetTableName(tableName.Substring(6));
                }
            }
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasIndex(c => c.Slug)
                      .IsUnique();
            });
            modelBuilder.Entity<PostCategory>(entity =>
            {
                entity.HasKey(c => new { c.PostID, c.CategoryID });
            });
            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasIndex(c => c.Slug)
                      .IsUnique();
            });


            modelBuilder.Entity<CategoryProduct>(entity =>
            {
                entity.HasIndex(c => c.Slug)
                    .IsUnique();
            });
            modelBuilder.Entity<ProductCategoryProduct>(entity =>
            {
                entity.HasKey(c => new { c.ProductID, c.CategoryID });
            });
            modelBuilder.Entity<ProductModel>(entity =>
            {
                entity.HasIndex(c => c.Slug)
                      .IsUnique();
            });
        }
        public DbSet<ContactModel> Contacts { set; get; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Post> Posts { set; get; }
        public DbSet<PostCategory> PostCategorys { set; get; }


        public DbSet<CategoryProduct> CategoryProducts { set; get; }
        public DbSet<ProductModel> Products { set; get; }
        public DbSet<ProductCategoryProduct> ProductCategoryProducts { set; get; }
        public DbSet<ProductPhoto> ProductPhotos { set; get; }


    }
}

//dotnet aspnet-codegenerator controller -name Contact -namespace App.Areas.Contact.Controllers  -m App.Models.Contacts.ContactModel -udl -dc App.Models.AppDbContext -outDir Areas/Contact/Controllers/