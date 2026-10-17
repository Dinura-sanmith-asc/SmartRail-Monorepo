using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Infrastructure.RailwayNetwork.Configurations;

public sealed class StationConfiguration : IEntityTypeConfiguration<Station>
{
    public void Configure(EntityTypeBuilder<Station> builder)
    {
        builder.ToTable("station", table => table.HasCheckConstraint("ck_station_name_not_blank", "length(trim(name)) > 0"));
        builder.HasKey(station => station.StationId).HasName("pk_station");
        builder.Property(station => station.StationId).HasColumnName("station_id").ValueGeneratedNever();
        builder.Property(station => station.Name).HasColumnName("name").IsRequired();
        builder.Property(station => station.IsPublished).HasColumnName("is_published").IsRequired();
        builder.Property(station => station.Version).HasColumnName("version").IsConcurrencyToken().IsRequired();
        builder.Property(station => station.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(station => station.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();

        var sourceIdentity = typeof(Station).GetProperty("SourceStationKey", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Station source identity property was not found.");
        builder.Property<string>(sourceIdentity.Name).HasColumnName("source_station_key").IsRequired();
        builder.HasIndex("SourceStationKey").IsUnique().HasDatabaseName("ux_station_source_station_key");

        var converter = new ValueConverter<IReadOnlyDictionary<string, string>, string>(
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
            value => JsonSerializer.Deserialize<Dictionary<string, string>>(value, (JsonSerializerOptions?)null)
                     ?? new Dictionary<string, string>(StringComparer.Ordinal));
        var detailsComparer = new ValueComparer<IReadOnlyDictionary<string, string>>(
            (left, right) => left != null && right != null && left.SequenceEqual(right),
            value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.Key, item.Value)),
            value => (IReadOnlyDictionary<string, string>)value.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal));
        builder.Property(station => station.VerifiedDetails)
            .HasColumnName("verified_details")
            .HasColumnType("jsonb")
            .HasConversion(converter)
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
    }
}