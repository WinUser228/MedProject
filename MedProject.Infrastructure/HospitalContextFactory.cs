using System.IO;
using HospitalStatistics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HospitalStatistics.Infrastructure
{
    public class HospitalContextFactory : IDesignTimeDbContextFactory<HospitalContext>
    {
        /// <summary>
        /// Совпадает с MedProject.Utilities.AppPaths.DatabasePath — без ссылки на UI-проект.
        /// </summary>
        private static string DefaultDatabasePath
        {
            get
            {
                var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrWhiteSpace(root))
                    root = AppContext.BaseDirectory;
                return Path.Combine(root, "MedProject", "HospitalStatistics.db");
            }
        }

        public HospitalContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<HospitalContext>();
            // Используем ту же строку подключения, что и в основном приложении
            // В production это должно читаться из конфигурации
            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString
                ?? $"Data Source={DefaultDatabasePath}";
            optionsBuilder.UseSqlite(connectionString);

            return new HospitalContext(optionsBuilder.Options);
        }
    }
}