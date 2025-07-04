using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<LoginHistory> LoginHistory { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Set up unique constraint for Matricule
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Matricule)
                .IsUnique();
                
            // Configure cascading delete for LoginHistory
            modelBuilder.Entity<LoginHistory>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure many-to-many relationship between User and Role
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => ur.Id);
                
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Create default roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "admin", Description = "Administrator" },
                new Role { Id = 2, Name = "ges", Description = "Gestionnaire" },
                new Role { Id = 3, Name = "formateur", Description = "Formateur" },
                new Role { Id = 4, Name = "emploi", Description = "Gestion des Emplois du temps" }
            );
        }
    }
} 