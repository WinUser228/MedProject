using HospitalStatistics.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HospitalStatistics.Infrastructure.Data
{

    public class HospitalContext : DbContext
    {
        public DbSet<Departments> Departments { get; set; }
        public DbSet<Beds> Beds { get; set; }
        public DbSet<ListPatientMovements> PatientMovements { get; set; }
        public DbSet<BedTypes> BedTypes { get; set; }

        public HospitalContext(DbContextOptions<HospitalContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(HospitalContext).Assembly);
        }
    }

}
