namespace HospitalStatistics.Core.Entities
{
    public class ListPatientMovements : BaseEntity
    {
        public DateOnly ReportDate { get; set; }           // Дата

        public int BedId { get; set; }                     // Код (койки/отделения)
        public Beds Bed { get; set; }

        // 2. Состояло больных на начало истекших суток
        public int? PatientsAtStartOfDay { get; set; }

        // 3. Поступило больных (без переведённых внутри)
        public int? TotalAdmitted { get; set; }            // Всего поступило
        public int? FromVillage { get; set; }              // Из них сельских жителей
        public int? ChildrenUnder14 { get; set; }          // Из них детей до 14 лет включительно

        // 4. Переведено б-х внутри б-цы
        public int? TransferredFromOtherDepartments { get; set; }   // Из других отделений
        public int? TransferredToOtherDepartments { get; set; }     // В другие отделения

        // 5. Выписано больных
        public int? TotalDischarged { get; set; }           // Всего выписано
        public int? DischargedByTransfer { get; set; }      // В т.ч. перевод в другие стац.
        public int? Died { get; set; }                      // Умерло

        // 6. На начало текущего дня – состоит больных
        public int? TotalPatients { get; set; }             // Всего (расчётное или сохранённое)
        public int? FromVillagePatient { get; set; }        // В т.ч. сельских жителей
        public int? MothersWithPatients { get; set; }       // Состоит матерей при больных детях

        // 7. Видов само матерей (отдельная графа справа)
        public int? MothersDischarged { get; set; }
    }

}
