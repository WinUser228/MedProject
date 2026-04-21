using HospitalStatistics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalStatistics.Infrastructure.Configuration
{
    public class ListPatientMovementConfiguration : IEntityTypeConfiguration<ListPatientMovements>
    {
        public void Configure(EntityTypeBuilder<ListPatientMovements> builder)
        {
            builder.ToTable("ListPatientMovements");

            builder.HasKey(p => p.Id);

            // Указываем, что Id генерируется автоматически базой данных
            builder.Property(p => p.Id)
                   .ValueGeneratedOnAdd();

            builder.Property(p => p.ReportDate)
                   .IsRequired();

            builder.HasOne(p => p.Bed)
                   .WithMany(b => b.PatientMovements)
                   .HasForeignKey(p => p.BedId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

}