using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Repository.Interfaces;
using HospitalStatistics.Infrastructure.Data;
using HospitalStatistics.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;

public class BedRepository : BaseRepository<Beds>, IBedRepository
{
    private readonly IDbContextFactory<HospitalContext> _contextFactory;

    public BedRepository(IDbContextFactory<HospitalContext> contextFactory)
        : base(contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // Получение коек по отделению с загрузкой типа койки и информации об отделении
    public async Task<List<Beds>> GetBedsByDepartmentIdAsync(int departmentId, CancellationToken ct)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Beds
            .Include(b => b.BedType)       // тип койки
            .Include(b => b.Department)    // отделение
            .Where(b => b.DepartmentId == departmentId)
            .OrderBy(b => b.Id)
            .ToListAsync(ct);
    }

    // Получение всех коек с информацией об отделениях
    public async Task<List<Beds>> GetAllBedsWithDepartmentsAsync(CancellationToken ct)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Beds
            .Include(b => b.BedType)
            .Include(b => b.Department)
            .OrderBy(b => b.Id)
            .ToListAsync(ct);
    }

    // Получение конкретной койки с информацией об отделении и типе койки
    public async Task<Beds?> GetBedWithDepartmentByIdAsync(int id, CancellationToken ct)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Beds
            .Include(b => b.BedType)
            .Include(b => b.Department)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }
}
