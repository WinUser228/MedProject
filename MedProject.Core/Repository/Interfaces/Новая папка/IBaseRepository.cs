using HospitalStatistics.Core.Entities;

namespace HospitalStatistics.Core.Repository.Interfaces
{
    public interface IBaseRepository<TEntity> where TEntity : BaseEntity
    {
        Task<List<TEntity>> GetAllAsync(CancellationToken ct);
        Task<TEntity?> GetByIdAsync(int id, CancellationToken ct);
        Task<int> CreateAsync(TEntity entity, CancellationToken ct);

        Task UpdateAsync(TEntity entity, CancellationToken ct);
        Task DeleteByIdAsync(int id, CancellationToken ct);

        Task DeleteAsync(TEntity entity, CancellationToken ct);


    }
}
