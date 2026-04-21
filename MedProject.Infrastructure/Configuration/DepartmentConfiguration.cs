// DepartmentConfiguration.cs
using HospitalStatistics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalStatistics.Infrastructure.Configuration
{
    public class DepartmentConfiguration : IEntityTypeConfiguration<Departments>
    {
        public void Configure(EntityTypeBuilder<Departments> builder)
        {
            builder.ToTable("Departments");

            builder.HasKey(d => d.Id);

            // Указываем, что Id генерируется автоматически базой данных
            builder.Property(d => d.Id)
                   .ValueGeneratedOnAdd();

            builder.Property(d => d.DepartmentName)
                   .IsRequired()
                   .HasMaxLength(100);
        }
    }


}
