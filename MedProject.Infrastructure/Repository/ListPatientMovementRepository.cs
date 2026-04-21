using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Repository.Interfaces;
using HospitalStatistics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalStatistics.Infrastructure.Repository
{
    public class ListPatientMovementRepository : BaseRepository<ListPatientMovements>, IListPatientMovementRepository
    {
        private readonly IDbContextFactory<HospitalContext> _contextFactory;

        public ListPatientMovementRepository(IDbContextFactory<HospitalContext> contextFactory)
            : base(contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<ListPatientMovements?> GetByBedIdAndReportDateAsync(int bedId, DateOnly reportDate, CancellationToken ct)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.PatientMovements
                .FirstOrDefaultAsync(pm => pm.BedId == bedId && pm.ReportDate == reportDate, ct);
        }

        public async Task<List<ListPatientMovements>> GetByDateAndBedIdsAsync(DateOnly date, List<int> bedIds, CancellationToken ct)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.PatientMovements
                .Where(pm => pm.ReportDate == date && bedIds.Contains(pm.BedId))
                .ToListAsync(ct);
        }

        public async Task<bool> HasMovementsForBedAsync(int bedId, CancellationToken ct)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.PatientMovements.AnyAsync(pm => pm.BedId == bedId, ct);
        }
    }
}