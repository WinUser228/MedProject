namespace HospitalStatistics.Core.Models
{
    /// <summary>
    /// DTO для отчёта 7 (часть 2), содержащего показатели работы коечного фонда.
    /// </summary>
    public class ReportSevenPart2DTO
    {
        /// <summary>
        /// Название отделения.
        /// </summary>
        public string DepartmentName { get; set; } = string.Empty;

        /// <summary>
        /// Тип койки.
        /// Для итогов по отделению содержит значение "Total by department",
        /// для общего итога по больнице — пустое значение.
        /// </summary>
        public string BedType { get; set; } = string.Empty;

        /// <summary>
        /// Плановое количество койко-дней за выбранный период.
        /// </summary>
        public int PlanForPeriod { get; set; } // ИСПРАВЛЕНО: int

        /// <summary>
        /// Фактическое количество койко-дней за период (сколько реально использовались койки).
        /// </summary>
        public int FactBedDays { get; set; } // ИСПРАВЛЕНО: int

        /// <summary>
        /// Процент выполнения плана койко-дней.
        /// Формула: (FactBedDays / PlanForPeriod) * 100
        /// </summary>
        public decimal FulfillmentPercentage { get; set; } // ИСПРАВЛЕНО: Переименовано с FromPlan

        /// <summary>
        /// Количество пациентов из сельской местности.
        /// </summary>
        public int FromVillage { get; set; }

        /// <summary>
        /// Количество матерей, находившихся в стационаре с детьми.
        /// </summary>
        public int Mothers { get; set; }

        /// <summary>
        /// Среднее пребывание больного (в койко-днях).
        /// По текущей методике в системе:
        ///   AverageStay = FactBedDays / TreatedPatients,
        /// где TreatedPatients = Выписано + Умерло + Переведено в другие отделения.
        /// </summary>
        public decimal AverageStay { get; set; }

        /// <summary>
        /// Занятость койки (в долях).
        /// Формула: FactBedDays / (число коек * дней в периоде)
        /// </summary>
        public decimal BedOccupancy { get; set; }

        /// <summary>
        /// Оборот койки.
        /// По текущей методике в системе:
        ///   BedTurnover = TreatedPatients / среднее число коек,
        /// где TreatedPatients = Выписано + Умерло + Переведено в другие отделения.
        /// </summary>
        public decimal BedTurnover { get; set; }

        /// <summary>
        /// Средний простой койки.
        /// По текущей методике в системе используется формула:
        ///   AverageBedDowntime = (Дни в периоде - (Факт койко-дней / среднее число коек)) / BedTurnover,
        /// где BedTurnover рассчитывается по пролеченным пациентам.
        /// </summary>
        public decimal AverageBedDowntime { get; set; }

        /// <summary>
        /// Общая летальность (в процентах).
        /// Классическая формула:
        ///   MortalityRate = (Died / (Discharged + Died)) * 100.
        /// В числителе и знаменателе участвуют только выбывшие пациенты (выписано + умерло).
        /// </summary>
        public decimal MortalityRate { get; set; }

        /// <summary>
        /// Общее количество выбывших больных из отделения.
        /// По принятой в системе методике:
        ///   DischargedPatients = Выписано + Умерло (без учёта переведённых в другие отделения).
        /// Эти значения используются, в частности, для расчёта летальности.
        /// </summary>
        public int DischargedPatients { get; set; }

        /// <summary>
        /// Количество пользованных больных.
        /// Формула: (Admitted + Discharged + Died) / 2
        /// </summary>
        public decimal UsedPatients { get; set; }
    }
}

