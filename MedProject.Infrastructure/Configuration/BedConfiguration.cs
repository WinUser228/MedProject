using HospitalStatistics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalStatistics.Infrastructure.Configuration
{
    public class BedConfiguration : IEntityTypeConfiguration<Beds>
    {
        public void Configure(EntityTypeBuilder<Beds> builder)
        {
            builder.ToTable("Beds");

            // Первичный ключ
            builder.HasKey(b => b.Id);

            // Указываем, что Id генерируется автоматически базой данных
            builder.Property(b => b.Id)
                   .ValueGeneratedOnAdd();

            // Свойства
            builder.Property(b => b.Amount)
                   .IsRequired();

            builder.Property(b => b.EffectiveDate)
                   .IsRequired();

            builder.Property(b => b.MonthlyPlan)
                   .IsRequired();

            builder.Property(b => b.NotUsedSince)
                   .IsRequired(false);

            // Внешние ключи и связи

            // Связь с Department
            builder.HasOne(b => b.Department)
                   .WithMany(d => d.Beds)
                   .HasForeignKey(b => b.DepartmentId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Связь с BedType
            builder.HasOne(b => b.BedType)
                   .WithMany() // можно указать .WithMany(bt => bt.Beds), если добавишь навигацию в BedType
                   .HasForeignKey(b => b.BedTypeId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Связь с PatientMovements (один ко многим)
            builder.HasMany(b => b.PatientMovements)
                   .WithOne(pm => pm.Bed)
                   .HasForeignKey(pm => pm.BedId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
