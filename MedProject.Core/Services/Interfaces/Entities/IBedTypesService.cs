using HospitalStatistics.Core.Entities;

namespace HospitalStatistics.Core.Services.Interfaces.Entities
{
    public interface IBedTypesService
    {
        Task<int> CreateBedTypeAsync(BedTypes bedType, CancellationToken ct);
        Task<int> UpdateBedTypeAsync(int id, BedTypes bedType, CancellationToken ct);
        Task<BedTypes?> GetBedTypeByIdAsync(int id, CancellationToken ct);
        Task<List<BedTypes>> GetAllBedTypesAsync(CancellationToken ct);
        Task<bool> DeleteBedTypeAsync(int id, CancellationToken ct);
        Task<BedTypes?> GetBedTypeByNameAsync(string bedTypeName, CancellationToken ct);
    }
}

