using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Bilet.Models;

public partial class BiletDbContext : DbContext
{
    public BiletDbContext()
    {
    }

    public BiletDbContext(DbContextOptions<BiletDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BiletFirmalari> BiletFirmalaris { get; set; }

    public virtual DbSet<Etkinlikler> Etkinliklers { get; set; }

    public virtual DbSet<Kategoriler> Kategorilers { get; set; }

    public virtual DbSet<Katilimlar> Katilimlars { get; set; }

    public virtual DbSet<Sehirler> Sehirlers { get; set; }

    public virtual DbSet<Uyeler> Uyelers { get; set; }

    public virtual DbSet<Bildirimler> Bildirimlers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.;Database=BiletDb;Trusted_Connection=True;Encrypt=False");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BiletFirmalari>(entity =>
        {
            entity.HasKey(e => e.FirmaId).HasName("PK__BiletFir__CD9C5ECF91839E9D");

            entity.ToTable("BiletFirmalari");

            entity.HasIndex(e => e.Mail, "UQ__BiletFir__2724B2D14F5F67C4").IsUnique();

            entity.Property(e => e.FirmaId).HasColumnName("FirmaID");
            entity.Property(e => e.FirmaAdi)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Mail)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Sifre)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.WebSitesi)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Etkinlikler>(entity =>
        {
            entity.HasKey(e => e.EtkinlikId).HasName("PK__Etkinlik__0299F28DA3E9CE92");

            entity.ToTable("Etkinlikler");

            entity.Property(e => e.Aciklama)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.Adres)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.BasvuruBitisTarihi).HasColumnType("datetime");
            entity.Property(e => e.EtkinlikAdi)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Tarih).HasColumnType("datetime");

            entity.HasOne(d => d.Kategori).WithMany(p => p.Etkinliklers)
                .HasForeignKey(d => d.KategoriId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Etkinlikler_Kategoriler");

            entity.HasOne(d => d.Organizator).WithMany(p => p.Etkinliklers)
                .HasForeignKey(d => d.OrganizatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Etkinlikler_Uyeler");

            entity.HasOne(d => d.Sehir).WithMany(p => p.Etkinliklers)
                .HasForeignKey(d => d.SehirId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Etkinlikler_Sehirler");
        });

        modelBuilder.Entity<Kategoriler>(entity =>
        {
            entity.HasKey(e => e.KategoriId);

            entity.ToTable("Kategoriler");

            entity.Property(e => e.KategoriAdi)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Katilimlar>(entity =>
        {
            entity.HasKey(e => e.KatilimId).HasName("PK__Katiliml__7739554BD3935767");

            entity.ToTable("Katilimlar");

            entity.Property(e => e.KatilimId).HasColumnName("KatilimID");
            entity.Property(e => e.EtkinlikId).HasColumnName("EtkinlikID");
            entity.Property(e => e.KatilimciId).HasColumnName("KatilimciID");

            entity.HasOne(d => d.Etkinlik).WithMany(p => p.Katilimlars)
                .HasForeignKey(d => d.EtkinlikId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Katilimlar_Etkinlikler");

            entity.HasOne(d => d.Katilimci).WithMany(p => p.Katilimlars)
                .HasForeignKey(d => d.KatilimciId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Katilimlar_Uyeler");
        });

        modelBuilder.Entity<Sehirler>(entity =>
        {
            entity.HasKey(e => e.SehirId);

            entity.ToTable("Sehirler");

            entity.Property(e => e.SehirAdi)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Uyeler>(entity =>
        {
            entity.HasKey(e => e.UyeId).HasName("PK__Uyeler__76F7D98F44FBD206");

            entity.ToTable("Uyeler");

            entity.HasIndex(e => e.Eposta, "UQ__Uyeler__03ABA3914F9FA28E").IsUnique();

            entity.Property(e => e.Adi)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Eposta)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Sifre)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.Soyadi)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false);
        });
        modelBuilder.Entity<Bildirimler>(entity =>
        {
            entity.HasKey(e => e.BildirimId);
            entity.ToTable("Bildirimler");           
            entity.Property(e => e.Bildirim)
                .HasMaxLength(500)
                .IsUnicode(true);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
