namespace HospitalStatistics.Core.Entities
{
    public class Beds : BaseEntity
    {
        public int DepartmentId { get; set; }
        public int BedTypeId { get; set; }
        public int Amount { get; set; }

        public DateOnly EffectiveDate { get; set; } // С какого дня действуют значения плана

        public int MonthlyPlan { get; set; }

        public DateOnly? NotUsedSince { get; set; } // Дата, с которой койка не используется

        public Departments Department { get; set; }

        public BedTypes BedType { get; set; }

        public List<ListPatientMovements> PatientMovements { get; set; }
    }

}
