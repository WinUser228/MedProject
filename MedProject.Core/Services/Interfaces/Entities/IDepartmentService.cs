using HospitalStatistics.Core.Entities;


namespace HospitalStatistics.Core.Services.Interfaces.Entities
{
    public interface IDepartmentService
    {
        Task<int> CreateDepartmentAsync(Departments doctorDto, CancellationToken ct);

        Task<int> UpdateDepartmentAsync(int id, Departments doctorDto, CancellationToken ct);

        Task<Departments?> GetDepartmentByIdAsync(int id, CancellationToken ct);

        Task<List<Departments>> GetAllDepartmentAsync(CancellationToken ct);

        Task<bool> DeleteDepartmentAsync(int id, CancellationToken ct);
    }
}

