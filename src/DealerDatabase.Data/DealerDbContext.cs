using DealerDatabase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DealerDatabase.Data;

/// <summary>
/// Provides access to consolidated dealers and their source observations.
/// </summary>
public class DealerDbContext(DbContextOptions<DealerDbContext> options) : DbContext(options)
{
    /// <summary>Gets the consolidated dealer records.</summary>
    public DbSet<Dealer> Dealers => Set<Dealer>();

    /// <summary>Gets the source records used to build dealer records.</summary>
    public DbSet<DealerSourceRecord> SourceRecords => Set<DealerSourceRecord>();

    /// <summary>Gets names observed in source records.</summary>
    public DbSet<DealerName> DealerNames => Set<DealerName>();

    /// <summary>Gets addresses observed in source records.</summary>
    public DbSet<DealerAddress> DealerAddresses => Set<DealerAddress>();

    /// <summary>Gets field values observed in source records.</summary>
    public DbSet<DealerSourceFieldValue> SourceFieldValues => Set<DealerSourceFieldValue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Dealer>(entity =>
        {
            entity.Property(d => d.LegalName)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(d => d.CompanyRegistrationNumber)
                .IsUnique();

            entity.HasMany(d => d.SourceRecords)
                .WithOne(source => source.Dealer)
                .HasForeignKey(source => source.DealerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DealerSourceRecord>(entity =>
        {
            entity.HasAlternateKey(source => new { source.Id, source.DealerId });
            entity.Property(source => source.SourceSystem)
                .IsRequired();

            entity.Property(source => source.SourceRecordId)
                .IsRequired();

            entity.Property(source => source.ContributedFieldsJson)
                .IsRequired()
                .HasDefaultValue("[]");

            entity.HasIndex(source => new { source.SourceSystem, source.SourceRecordId })
                .IsUnique();
        });
        modelBuilder.Entity<DealerName>(entity =>
        {
            entity.Property(name => name.NameType).IsRequired().HasMaxLength(32);
            entity.Property(name => name.SourceField).IsRequired().HasMaxLength(100);
            entity.Property(name => name.RawValue).IsRequired();
            entity.HasOne(name => name.Dealer).WithMany(dealer => dealer.Names)
                .HasForeignKey(name => name.DealerId).OnDelete(DeleteBehavior.Cascade);
            // Including the dealer ID prevents an observation from referencing another dealer's source record.
            entity.HasOne(name => name.SourceRecord).WithMany(source => source.Names)
                .HasForeignKey(name => new { name.SourceRecordId, name.DealerId })
                .HasPrincipalKey(source => new { source.Id, source.DealerId })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(name => new { name.SourceRecordId, name.SourceField, name.Occurrence }).IsUnique();
        });

        modelBuilder.Entity<DealerAddress>(entity =>
        {
            entity.Property(address => address.AddressType).IsRequired().HasMaxLength(32);
            entity.Property(address => address.SourceField).IsRequired().HasMaxLength(100);
            entity.Property(address => address.RawValue).IsRequired();
            entity.HasOne(address => address.Dealer).WithMany(dealer => dealer.Addresses)
                .HasForeignKey(address => address.DealerId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(address => address.SourceRecord).WithMany(source => source.Addresses)
                .HasForeignKey(address => new { address.SourceRecordId, address.DealerId })
                .HasPrincipalKey(source => new { source.Id, source.DealerId })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(address => new { address.SourceRecordId, address.SourceField, address.Occurrence }).IsUnique();
        });

        modelBuilder.Entity<DealerSourceFieldValue>(entity =>
        {
            entity.Property(value => value.FieldName).IsRequired().HasMaxLength(200);
            entity.Property(value => value.ConsolidatedFieldName).HasMaxLength(100);
            entity.Property(value => value.RawValue).IsRequired();
            entity.HasOne(value => value.SourceRecord).WithMany(source => source.FieldValues)
                .HasForeignKey(value => value.SourceRecordId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(value => new { value.SourceRecordId, value.FieldName, value.Occurrence })
                .IsUnique();
        });

    }
}
