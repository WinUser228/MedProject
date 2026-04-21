using HospitalStatistics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalStatistics.Infrastructure.Configuration
{
    public class BedTypeConfiguration : IEntityTypeConfiguration<BedTypes>
    {
        public void Configure(EntityTypeBuilder<BedTypes> builder)
        {
            builder.ToTable("BedTypes");

            builder.HasKey(bt => bt.Id);

            // Указываем, что Id генерируется автоматически базой данных
            builder.Property(bt => bt.Id)
                   .ValueGeneratedOnAdd();

            builder.Property(bt => bt.BedTypeName)
                   .IsRequired()
                   .HasMaxLength(100); // длина имени типа кровати

        }
    }
}
