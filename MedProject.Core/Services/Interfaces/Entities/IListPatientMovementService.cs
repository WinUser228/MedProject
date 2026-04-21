using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Repository.Interfaces;

namespace HospitalStatistics.Core.Services.Interfaces.Entities
{
    public interface IListPatientMovementService
    {
        Task<int> CreateListPatientMovementAsync(ListPatientMovements doctorDto, CancellationToken ct);

        Task<int> UpdateListPatientMovementAsync(int id, ListPatientMovements doctor, CancellationToken ct);

        Task<ListPatientMovements?> GetListPatientMovementByIdAsync(int id, CancellationToken ct);

        Task<List<ListPatientMovements>> GetAllListPatientMovementAsync(CancellationToken ct);

        Task<bool> DeleteListPatientMovementAsync(int id, CancellationToken ct);
        Task SaveListPatientMovementsAsync(List<ListPatientMovements> entries, CancellationToken ct);

        Task<List<ListPatientMovements>> GetListPatientMovementsByDateAndBedIdsAsync(DateOnly date, List<int> bedIds, CancellationToken ct);

        Task<ListPatientMovements?> GetListPatientMovementByBedAndDateAsync(int bedId, DateOnly date, CancellationToken ct);

        IListPatientMovementRepository GetMovementRepositoryForDate(DateOnly date);

        Task<bool> HasMovementsForBedAsync(int bedId, CancellationToken ct);
    }
}
