//using HospitalStatistics.Core.Entities;
//using HospitalStatistics.Core.Repository.Interfaces;
//using HospitalStatistics.Infrastructure.Data;
//using Microsoft.EntityFrameworkCore;

//namespace HospitalStatistics.Infrastructure.Repository
//{
//    public class BedPlanHistoryRepository : BaseRepository<BedPlans>, IBedPlanHistoryRepository
//    {
//        private readonly IDbContextFactory<HospitalContext> _contextFactory;

//        public BedPlanHistoryRepository(IDbContextFactory<HospitalContext> contextFactory)
//            : base(contextFactory)
//        {
//            _contextFactory = contextFactory;
//        }

//        public async Task<List<BedPlans>> GetAllHistoriesWithRelationsAsync(CancellationToken ct)
//        {
//            using var context = _contextFactory.CreateDbContext();
//            return await context.BedPlanHistory
//                .Include(bph => bph.Bed)
//                .ThenInclude(b => b.Department)
//                .ToListAsync(ct);
//        }

//        public async Task<BedPlans?> GetHistoryWithRelationsByIdAsync(int id, CancellationToken ct)
//        {
//            using var context = _contextFactory.CreateDbContext();
//            return await context.BedPlanHistory
//                .Include(bph => bph.Bed)
//                .ThenInclude(b => b.Department)
//                .FirstOrDefaultAsync(bph => bph.Id == id, ct);
//        }
//    }
//}