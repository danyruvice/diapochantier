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
        public DbSet<Projet> Projets { get; set; }
        public DbSet<Jalon> Jalons { get; set; }
        public DbSet<InvitationProjet> Invitations { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Sequestre> Sequestres { get; set; }
        public DbSet<Paiement> Paiements { get; set; }
        public DbSet<WebhookEvent> WebhookEvents { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<AvisEntrepreneur> AvisEntrepreneurs { get; set; }
        public DbSet<Materiau> Materiaux { get; set; }

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
                entity.Property(u => u.PieceIdentiteUrl).HasMaxLength(500);
                entity.Property(u => u.PieceIdentiteHashSha256).HasMaxLength(64);

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

            builder.Entity<Projet>(entity =>
            {
                entity.HasKey(p => p.IdProjet);
                entity.Property(p => p.Titre).HasMaxLength(150).IsRequired();
                entity.Property(p => p.TypeProjet).HasMaxLength(100).IsRequired();
                entity.Property(p => p.Localisation).HasMaxLength(200).IsRequired();
                entity.Property(p => p.BudgetTotal).HasPrecision(18, 2);

                entity.HasOne(p => p.Investisseur)
                      .WithMany()
                      .HasForeignKey(p => p.IdInvestisseur)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Entrepreneur)
                      .WithMany()
                      .HasForeignKey(p => p.IdEntrepreneur)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Jalon>(entity =>
            {
                entity.HasKey(j => j.IdJalon);
                entity.Property(j => j.Titre).HasMaxLength(150).IsRequired();
                entity.Property(j => j.Description).HasMaxLength(500);
                entity.Property(j => j.Montant).HasPrecision(18, 2);

                entity.HasOne(j => j.Projet)
                      .WithMany(p => p.Jalons)
                      .HasForeignKey(j => j.IdProjet)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<InvitationProjet>(entity =>
            {
                entity.HasKey(i => i.IdInvitation);
                entity.Property(i => i.Message).HasMaxLength(500);
                entity.Property(i => i.MotifRefus).HasMaxLength(500);
                entity.Property(i => i.Statut).HasConversion<string>();
                entity.HasIndex(i => new { i.IdProjet, i.IdEntrepreneur }).IsUnique();

                entity.HasOne(i => i.Projet)
                    .WithMany()
                    .HasForeignKey(i => i.IdProjet)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(i => i.Entrepreneur)
                    .WithMany()
                    .HasForeignKey(i => i.IdEntrepreneur)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Photo>(entity =>
            {
                entity.HasKey(p => p.IdPhoto);
                entity.Property(p => p.Url).HasMaxLength(500).IsRequired();
                entity.Property(p => p.HashSHA256).HasMaxLength(64).IsRequired();
                entity.Property(p => p.Source).HasMaxLength(100);
                entity.HasIndex(p => p.IdProjet);
                entity.HasIndex(p => p.IdJalon);

                entity.HasOne(p => p.Projet)
                    .WithMany()
                    .HasForeignKey(p => p.IdProjet)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Jalon)
                    .WithMany()
                    .HasForeignKey(p => p.IdJalon)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Sequestre>(entity =>
            {
                entity.HasKey(s => s.IdSequestre);
                entity.Property(s => s.MontantBloque).HasPrecision(18, 2);
                entity.Property(s => s.MontantLibere).HasPrecision(18, 2);
                entity.Property(s => s.Statut).HasConversion<string>();
                entity.HasIndex(s => new { s.IdProjet, s.IdInvestisseur }).IsUnique();

                entity.HasOne(s => s.Projet)
                    .WithMany()
                    .HasForeignKey(s => s.IdProjet)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Investisseur)
                    .WithMany()
                    .HasForeignKey(s => s.IdInvestisseur)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Paiement>(entity =>
            {
                entity.HasKey(p => p.IdPaiement);
                entity.Property(p => p.Montant).HasPrecision(18, 2);
                entity.Property(p => p.TypePaiement).HasConversion<string>();
                entity.Property(p => p.Canal).HasConversion<string>();
                entity.Property(p => p.Statut).HasConversion<string>();
                entity.Property(p => p.ReferenceExterne).HasMaxLength(120);
                entity.Property(p => p.Message).HasMaxLength(500);
                entity.HasIndex(p => p.IdProjet);
                entity.HasIndex(p => p.IdInvestisseur);
                entity.HasIndex(p => p.ReferenceExterne).IsUnique();

                entity.HasOne(p => p.Projet)
                    .WithMany()
                    .HasForeignKey(p => p.IdProjet)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Investisseur)
                    .WithMany()
                    .HasForeignKey(p => p.IdInvestisseur)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Entrepreneur)
                    .WithMany()
                    .HasForeignKey(p => p.IdEntrepreneur)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Sequestre)
                    .WithMany()
                    .HasForeignKey(p => p.IdSequestre)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<WebhookEvent>(entity =>
            {
                entity.HasKey(w => w.IdWebhookEvent);
                entity.Property(w => w.EventId).HasMaxLength(200).IsRequired();
                entity.Property(w => w.EventType).HasMaxLength(120).IsRequired();
                entity.Property(w => w.PayloadHash).HasMaxLength(64).IsRequired();
                entity.HasIndex(w => w.EventId).IsUnique();
            });

            builder.Entity<TokenResetPassword>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Token).HasMaxLength(256).IsRequired();
                entity.HasIndex(t => t.Token).IsUnique();
            });

            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Token).HasMaxLength(128).IsRequired();
                entity.HasIndex(r => r.Token).IsUnique();
            });

            builder.Entity<AvisEntrepreneur>(entity =>
            {
                entity.HasKey(a => a.IdAvis);
                entity.HasIndex(a => new { a.IdEntrepreneur, a.IdProjet, a.IdInvestisseur }).IsUnique();
            });

            builder.Entity<Materiau>(entity =>
            {
                entity.HasKey(m => m.IdMateriau);
                entity.Property(m => m.PrixTotal).HasPrecision(18, 2);
                entity.Property(m => m.Quantite).HasPrecision(18, 3);
                entity.HasIndex(m => m.HashSHA256);
            });

            builder.Entity<Utilisateur>().ToTable("Utilisateurs");
        }
    }
}