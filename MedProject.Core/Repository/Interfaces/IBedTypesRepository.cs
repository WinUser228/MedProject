using HospitalStatistics.Core.Entities;

namespace HospitalStatistics.Core.Repository.Interfaces
{
    public interface IBedTypesRepository : IBaseRepository<BedTypes>
    {
        Task<List<BedTypes>> GetAllBedTypesAsync(CancellationToken ct);
        Task<BedTypes?> GetBedTypeByNameAsync(string bedTypeName, CancellationToken ct);
    }
}

