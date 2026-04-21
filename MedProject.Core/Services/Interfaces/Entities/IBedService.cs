
using HospitalStatistics.Core.Entities;

namespace HospitalStatistics.Core.Services.Interfaces.Entities
{
    public interface IBedService
    {
        Task<int> CreateBedAsync(Beds bedDto, CancellationToken ct);

        Task<int> UpdateBedAsync(int id, Beds bed, CancellationToken ct);

        Task<Beds?> GetBedByIdAsync(int id, CancellationToken ct);

        Task<List<Beds>> GetAllBedAsync(CancellationToken ct);

        Task<bool> DeleteBedAsync(int id, CancellationToken ct);

        Task<List<Beds>> GetBedsByDepartmentIdAsync(int departmentId, CancellationToken ct);

        Task<List<Beds>> GetAllBedsWithDepartmentsAsync(CancellationToken ct);

        Task<Beds?> GetBedWithDepartmentByIdAsync(int id, CancellationToken ct);




    }
}
