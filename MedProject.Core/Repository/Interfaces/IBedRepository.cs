using HospitalStatistics.Core.Entities;


namespace HospitalStatistics.Core.Repository.Interfaces
{
    public interface IBedRepository : IBaseRepository<Beds>
    {

        Task<List<Beds>> GetBedsByDepartmentIdAsync(int departmentId, CancellationToken ct);

        Task<List<Beds>> GetAllBedsWithDepartmentsAsync(CancellationToken ct);

        Task<Beds?> GetBedWithDepartmentByIdAsync(int id, CancellationToken ct);

    }
}
