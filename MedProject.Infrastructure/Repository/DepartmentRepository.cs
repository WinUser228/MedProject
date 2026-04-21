using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Repository.Interfaces;
using HospitalStatistics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalStatistics.Infrastructure.Repository
{
    public class DepartmentRepository : BaseRepository<Departments>, IDepartmentRepository
    {
        public DepartmentRepository(IDbContextFactory<HospitalContext> contextFactory)
            : base(contextFactory)
        {
        }
    }
}