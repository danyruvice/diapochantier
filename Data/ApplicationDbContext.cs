using DiaspoChantier.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DiaspoChantier.Data
{
    /// <summary>
    /// Contexte EF Core — hérite de IdentityDbContext pour intégrer ASP.NET Core Identity.
    /// Approche Code First : migrations générées depuis les modèles C#.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<Utilisateur>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Entrepreneur> Entrepreneurs { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<TokenResetPassword> TokensResetPassword { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Utilisateur>(entity =>
            {
                entity.Property(u => u.Nom).HasMaxLength(100).IsRequired();
                entity.Property(u => u.Prenom).HasMaxLength(100).IsRequired();
                entity.Property(u => u.Telephone).HasMaxLength(30);
                entity.Property(u => u.Adresse).HasMaxLength(500);
                entity.Property(u => u.PhotoProfilUrl).HasMaxLength(500);

                entity.HasIndex(u => u.Email).IsUnique();
            });

            builder.Entity<Utilisateur>()
                .HasOne(u => u.ProfilEntrepreneur)
                .WithOne(e => e.Utilisateur)
                .HasForeignKey<Entrepreneur>(e => e.IdUtilisateur)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Entrepreneur>(entity =>
            {
                entity.HasKey(e => e.IdEntrepreneur);
                entity.Property(e => e.Specialite).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Ville).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ScoreFilabilite)
                      .HasPrecision(3, 2)
                      .HasDefaultValue(0.0);
            });

            builder.Entity<Document>(entity =>
            {
                entity.HasKey(d => d.IdDocument);
                entity.Property(d => d.UrlStockage).HasMaxLength(500).IsRequired();
                entity.Property(d => d.HashSHA256).HasMaxLength(64);

                entity.HasOne(d => d.Entrepreneur)
                      .WithMany(e => e.Documents)
                      .HasForeignKey(d => d.IdEntrepreneur)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<TokenResetPassword>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Token).HasMaxLength(256).IsRequired();
                entity.HasIndex(t => t.Token).IsUnique();
            });

            builder.Entity<Utilisateur>().ToTable("Utilisateurs");
        }
    }
}