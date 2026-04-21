using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Repository.Interfaces;
using HospitalStatistics.Infrastructure.Data;
using HospitalStatistics.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;

public class BedTypesRepository : BaseRepository<BedTypes>, IBedTypesRepository
{
    private readonly IDbContextFactory<HospitalContext> _contextFactory;

    public BedTypesRepository(IDbContextFactory<HospitalContext> contextFactory)
        : base(contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<BedTypes>> GetAllBedTypesAsync(CancellationToken ct)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.BedTypes
            .OrderBy(bt => bt.BedTypeName)
            .ToListAsync(ct);
    }

    public async Task<BedTypes?> GetBedTypeByNameAsync(string bedTypeName, CancellationToken ct)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.BedTypes
            .FirstOrDefaultAsync(bt => bt.BedTypeName == bedTypeName, ct);
    }
}

