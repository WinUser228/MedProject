using HospitalStatistics.Core.Entities;

namespace HospitalStatistics.Core.Repository.Interfaces
{

    public interface IListPatientMovementRepository : IBaseRepository<ListPatientMovements>
    {
        Task<ListPatientMovements?> GetByBedIdAndReportDateAsync(int bedId, DateOnly reportDate, CancellationToken ct);
        Task<List<ListPatientMovements>> GetByDateAndBedIdsAsync(DateOnly date, List<int> bedIds, CancellationToken ct);
        Task<bool> HasMovementsForBedAsync(int bedId, CancellationToken ct);
    }
}
