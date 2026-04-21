namespace HospitalStatistics.Core.Entities
{
    public class Departments : BaseEntity
    {
        public string DepartmentName { get; set; }
        public List<Beds> Beds { get; set; }


    }
}
