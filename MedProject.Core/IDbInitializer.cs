namespace HospitalStatistics.Core
{
    public interface IDbInitializer
    {
        Task InitializeAsync(CancellationToken ct = default);
    }
}
