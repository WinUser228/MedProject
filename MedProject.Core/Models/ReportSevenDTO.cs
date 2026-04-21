namespace HospitalStatistics.Core.Models
{
    public class ReportSevenDTO
    {
        public string DepartmentName { get; set; }           // Название отделения
        public string BedType { get; set; }                  // Тип койки

        public int BedCount { get; set; }                    // Количество коек (Amount)
        public int PatientsAtStartOfDay { get; set; }        // Состояло на начало истекших суток

        public int TotalAdmitted { get; set; }               // Всего поступило
        public int FromVillage { get; set; }                 // Из деревни
        public int ChildrenUnder14 { get; set; }             // Дети до 14 лет
        public int TransferredFromOtherDepartments { get; set; }  // Переведены из других отделений
        public int TransferredToOtherDepartments { get; set; }    // Переведены в другие отделения
        public int TotalDischarged { get; set; }             // Всего выписано
        public int DischargedByTransfer { get; set; }        // Выписано с переводом
        public int Died { get; set; }                        // Умерло

        public int TotalPatients { get; set; }               // Состоит на начало текущего дня (по последней дате)
        public int FromVillagePatient { get; set; }          // Было пациентов из деревни на начало текущего дня
        public int MothersWithPatients { get; set; }         // Матерей при пациентах на начало текущего дня

        public double MonthlyPlanPercent { get; set; }       // Процент выполнения плана

        // Проведено койко-дней (за период)
        public int BedDaysTotal { get; set; }                // Проведено всего койко-дней за период
        public int BedDaysFromVillage { get; set; }          // Проведено койко-дней (деревня)
        public int BedDaysMothers { get; set; }              // Проведено койко-дней (матери)

    }
}
