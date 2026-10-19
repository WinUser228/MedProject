using HospitalStatistics.Core.Models;
using HospitalStatistics.Core.Services;
using HospitalStatistics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NLog;

namespace HospitalStatistics.Infrastructure.Services.Reports
{
    public class ReportSevenService : IReportSevenService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IDbContextFactory<HospitalContext> _contextFactory;

        public ReportSevenService(IDbContextFactory<HospitalContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        // =============================== PART 1 ======================================
        public async Task<List<ReportSevenDTO>> GetReportAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct)
        {
            Logger.Info("Запуск формирования отчёта 7 (часть 1) с {0} по {1}", startDate, endDate);

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync(ct);
                var isYearlyReport = IsYearlyReport(startDate, endDate);

                var movements = await context.PatientMovements
                    .AsNoTracking()
                    .Where(pm => pm.ReportDate >= startDate && pm.ReportDate <= endDate)
                    .Select(pm => new MovementRowPart1
                    {
                        ReportDate = pm.ReportDate,
                        BedId = pm.BedId,
                        PatientsAtStartOfDay = pm.PatientsAtStartOfDay ?? 0,
                        TotalAdmitted = pm.TotalAdmitted ?? 0,
                        FromVillage = pm.FromVillage ?? 0,
                        ChildrenUnder14 = pm.ChildrenUnder14 ?? 0,
                        TransferredFromOtherDepartments = pm.TransferredFromOtherDepartments ?? 0,
                        TransferredToOtherDepartments = pm.TransferredToOtherDepartments ?? 0,
                        TotalDischarged = pm.TotalDischarged ?? 0,
                        DischargedByTransfer = pm.DischargedByTransfer ?? 0,
                        Died = pm.Died ?? 0,
                        TotalPatients = pm.TotalPatients ?? 0,
                        FromVillagePatient = pm.FromVillagePatient ?? 0,
                        MothersWithPatients = pm.MothersWithPatients ?? 0
                    })
                    .ToListAsync(ct);

                if (movements.Count == 0)
                {
                    return new List<ReportSevenDTO>
                    {
                        new ReportSevenDTO
                        {
                            DepartmentName = "",
                            BedType = "",
                            BedCount = 0,
                            PatientsAtStartOfDay = 0,
                            TotalAdmitted = 0,
                            FromVillage = 0,
                            ChildrenUnder14 = 0,
                            TransferredFromOtherDepartments = 0,
                            TransferredToOtherDepartments = 0,
                            TotalDischarged = 0,
                            DischargedByTransfer = 0,
                            Died = 0,
                            TotalPatients = 0,
                            FromVillagePatient = 0,
                            MothersWithPatients = 0,
                            MonthlyPlanPercent = 0.0,
                            BedDaysTotal = 0,
                            BedDaysFromVillage = 0,
                            BedDaysMothers = 0
                        }
                    };
                }

                var firstDate = movements.Min(m => m.ReportDate);
                var lastDate = movements.Max(m => m.ReportDate);

                var beds = await context.Beds
                    .AsNoTracking()
                    .Select(b => new BedRow
                    {
                        Id = b.Id,
                        DepartmentId = b.DepartmentId,
                        BedTypeId = b.BedTypeId,
                        DepartmentName = b.Department.DepartmentName,
                        BedTypeName = b.BedType.BedTypeName,
                        Amount = b.Amount,
                        EffectiveDate = b.EffectiveDate,
                        MonthlyPlan = b.MonthlyPlan,
                        NotUsedSince = b.NotUsedSince
                    })
                    .ToListAsync(ct);

                var bedById = beds.ToDictionary(b => b.Id, b => b);

                var includedMovements = movements
                    .Where(m => bedById.TryGetValue(m.BedId, out var bed) && IsBedIncludedInPeriod(bed.NotUsedSince, startDate, endDate, isYearlyReport))
                    .Select(m => new MovementJoinedPart1(m, bedById[m.BedId]))
                    .ToList();

                if (includedMovements.Count == 0)
                {
                    return new List<ReportSevenDTO>();
                }

                var planTotalsByDeptBedType = CalculatePlanTotalsByDeptBedType(startDate, endDate, beds, isYearlyReport);
                var finalBedCountsByDeptBedType = CalculateFinalBedCountsByDeptBedType(startDate, endDate, beds, isYearlyReport);

                var typeRows = includedMovements
                    .GroupBy(x => new { x.DepartmentId, x.DepartmentName, x.BedTypeId, x.BedTypeName })
                    .Select(g =>
                    {
                        var key = g.Key;

                        var bedDaysTotal = g.Sum(x => x.TotalPatients);
                        var monthlyPlan = planTotalsByDeptBedType.TryGetValue((key.DepartmentId, key.BedTypeId), out var mp) ? mp : 0;
                        var monthlyPlanPercent = monthlyPlan > 0
                            ? RoundAwayFromZero((decimal)bedDaysTotal * 100m / monthlyPlan, 2)
                            : 0.0m;

                        var patientsAtStart = g.Where(x => x.ReportDate == firstDate).Select(x => x.PatientsAtStartOfDay).DefaultIfEmpty(0).Max();
                        var totalPatientsLast = g.Where(x => x.ReportDate == lastDate).Select(x => x.TotalPatients).DefaultIfEmpty(0).Max();
                        var fromVillagePatientLast = g.Where(x => x.ReportDate == lastDate).Select(x => x.FromVillagePatient).DefaultIfEmpty(0).Max();
                        var mothersWithPatientsLast = g.Where(x => x.ReportDate == lastDate).Select(x => x.MothersWithPatients).DefaultIfEmpty(0).Max();

                        var bedCount = finalBedCountsByDeptBedType.TryGetValue((key.DepartmentId, key.BedTypeId), out var bc) ? bc : 0;

                        return new TypeRowPart1
                        {
                            DepartmentName = key.DepartmentName,
                            BedType = key.BedTypeName,
                            BedCount = bedCount,
                            PatientsAtStartOfDay = patientsAtStart,
                            TotalAdmitted = g.Sum(x => x.TotalAdmitted),
                            FromVillage = g.Sum(x => x.FromVillage),
                            ChildrenUnder14 = g.Sum(x => x.ChildrenUnder14),
                            TransferredFromOtherDepartments = g.Sum(x => x.TransferredFromOtherDepartments),
                            TransferredToOtherDepartments = g.Sum(x => x.TransferredToOtherDepartments),
                            TotalDischarged = g.Sum(x => x.TotalDischarged),
                            DischargedByTransfer = g.Sum(x => x.DischargedByTransfer),
                            Died = g.Sum(x => x.Died),
                            TotalPatients = totalPatientsLast,
                            FromVillagePatient = fromVillagePatientLast,
                            MothersWithPatients = mothersWithPatientsLast,
                            MonthlyPlanPercent = (double)monthlyPlanPercent,
                            BedDaysTotal = bedDaysTotal,
                            BedDaysFromVillage = g.Sum(x => x.FromVillagePatient),
                            BedDaysMothers = g.Sum(x => x.MothersWithPatients),
                            MonthlyPlanForTotals = monthlyPlan
                        };
                    })
                    .ToList();

                var departmentNames = typeRows.Select(x => x.DepartmentName).Distinct().OrderBy(n => n).ToList();
                var result = new List<ReportSevenDTO>();

                foreach (var deptName in departmentNames)
                {
                    var deptTypes = typeRows.Where(x => x.DepartmentName == deptName).OrderBy(x => x.BedType).ToList();
                    result.AddRange(deptTypes.Select(x => new ReportSevenDTO
                    {
                        DepartmentName = x.DepartmentName,
                        BedType = x.BedType,
                        BedCount = x.BedCount,
                        PatientsAtStartOfDay = x.PatientsAtStartOfDay,
                        TotalAdmitted = x.TotalAdmitted,
                        FromVillage = x.FromVillage,
                        ChildrenUnder14 = x.ChildrenUnder14,
                        TransferredFromOtherDepartments = x.TransferredFromOtherDepartments,
                        TransferredToOtherDepartments = x.TransferredToOtherDepartments,
                        TotalDischarged = x.TotalDischarged,
                        DischargedByTransfer = x.DischargedByTransfer,
                        Died = x.Died,
                        TotalPatients = x.TotalPatients,
                        FromVillagePatient = x.FromVillagePatient,
                        MothersWithPatients = x.MothersWithPatients,
                        MonthlyPlanPercent = x.MonthlyPlanPercent,
                        BedDaysTotal = x.BedDaysTotal,
                        BedDaysFromVillage = x.BedDaysFromVillage,
                        BedDaysMothers = x.BedDaysMothers
                    }));

                    var deptBedDaysTotal = deptTypes.Sum(x => x.BedDaysTotal);
                    var deptMonthlyPlan = deptTypes.Sum(x => x.MonthlyPlanForTotals);
                    var deptPlanCompletionPercent = deptMonthlyPlan > 0
                        ? RoundAwayFromZero((decimal)deptBedDaysTotal * 100m / deptMonthlyPlan, 2)
                        : 0.0m;

                    result.Add(new ReportSevenDTO
                    {
                        DepartmentName = deptName,
                        BedType = "ИТОГО",
                        BedCount = deptTypes.Sum(x => x.BedCount),
                        PatientsAtStartOfDay = deptTypes.Sum(x => x.PatientsAtStartOfDay),
                        TotalAdmitted = deptTypes.Sum(x => x.TotalAdmitted),
                        FromVillage = deptTypes.Sum(x => x.FromVillage),
                        ChildrenUnder14 = deptTypes.Sum(x => x.ChildrenUnder14),
                        TransferredFromOtherDepartments = deptTypes.Sum(x => x.TransferredFromOtherDepartments),
                        TransferredToOtherDepartments = deptTypes.Sum(x => x.TransferredToOtherDepartments),
                        TotalDischarged = deptTypes.Sum(x => x.TotalDischarged),
                        DischargedByTransfer = deptTypes.Sum(x => x.DischargedByTransfer),
                        Died = deptTypes.Sum(x => x.Died),
                        TotalPatients = deptTypes.Sum(x => x.TotalPatients),
                        FromVillagePatient = deptTypes.Sum(x => x.FromVillagePatient),
                        MothersWithPatients = deptTypes.Sum(x => x.MothersWithPatients),
                        MonthlyPlanPercent = (double)deptPlanCompletionPercent,
                        BedDaysTotal = deptBedDaysTotal,
                        BedDaysFromVillage = deptTypes.Sum(x => x.BedDaysFromVillage),
                        BedDaysMothers = deptTypes.Sum(x => x.BedDaysMothers)
                    });
                }

                var totalBedDaysTotal = typeRows.Sum(x => x.BedDaysTotal);
                var totalMonthlyPlan = typeRows.Sum(x => x.MonthlyPlanForTotals);
                var totalPlanCompletionPercent = totalMonthlyPlan > 0
                    ? RoundAwayFromZero((decimal)totalBedDaysTotal * 100m / totalMonthlyPlan, 2)
                    : 0.0m;

                result.Add(new ReportSevenDTO
                {
                    DepartmentName = "ИТОГО",
                    BedType = "ИТОГО",
                    BedCount = typeRows.Sum(x => x.BedCount),
                    PatientsAtStartOfDay = typeRows.Sum(x => x.PatientsAtStartOfDay),
                    TotalAdmitted = typeRows.Sum(x => x.TotalAdmitted),
                    FromVillage = typeRows.Sum(x => x.FromVillage),
                    ChildrenUnder14 = typeRows.Sum(x => x.ChildrenUnder14),
                    TransferredFromOtherDepartments = typeRows.Sum(x => x.TransferredFromOtherDepartments),
                    TransferredToOtherDepartments = typeRows.Sum(x => x.TransferredToOtherDepartments),
                    TotalDischarged = typeRows.Sum(x => x.TotalDischarged),
                    DischargedByTransfer = typeRows.Sum(x => x.DischargedByTransfer),
                    Died = typeRows.Sum(x => x.Died),
                    TotalPatients = typeRows.Sum(x => x.TotalPatients),
                    FromVillagePatient = typeRows.Sum(x => x.FromVillagePatient),
                    MothersWithPatients = typeRows.Sum(x => x.MothersWithPatients),
                    MonthlyPlanPercent = (double)totalPlanCompletionPercent,
                    BedDaysTotal = totalBedDaysTotal,
                    BedDaysFromVillage = typeRows.Sum(x => x.BedDaysFromVillage),
                    BedDaysMothers = typeRows.Sum(x => x.BedDaysMothers)
                });

                Logger.Info("Отчёт 7 (часть 1) успешно сформирован. Количество записей: {0}", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка при формировании отчёта 7 (часть 1) за период {0} - {1}", startDate, endDate);
                throw;
            }
        }

        // =============================== PART 2 ======================================
        // Я предполагаю, что класс называется ReportService или аналогично
        public async Task<List<ReportSevenPart2DTO>> GetReportPart2Async(DateOnly startDate, DateOnly endDate, CancellationToken ct)
        {
            // Проверка наличия столбца "FromPlan" в DTO, так как его нет в SQL,
            // заменяю его на "FulfillmentPercentage" для корректного чтения.

            Logger.Info("Запуск формирования отчёта 7 (часть 2) с {0} по {1}", startDate, endDate);

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync(ct);
                var isYearlyReport = IsYearlyReport(startDate, endDate);
                var daysInPeriod = (endDate.ToDateTime(TimeOnly.MinValue) - startDate.ToDateTime(TimeOnly.MinValue)).Days + 1;

                var movements = await context.PatientMovements
                    .AsNoTracking()
                    .Where(pm => pm.ReportDate >= startDate && pm.ReportDate <= endDate)
                    .Select(pm => new MovementRowPart2
                    {
                        BedId = pm.BedId,
                        TotalPatients = pm.TotalPatients ?? 0,
                        TotalDischarged = pm.TotalDischarged ?? 0,
                        Died = pm.Died ?? 0,
                        TransferredToOtherDepartments = pm.TransferredToOtherDepartments ?? 0,
                        TotalAdmitted = pm.TotalAdmitted ?? 0,
                        FromVillagePatient = pm.FromVillagePatient ?? 0,
                        MothersWithPatients = pm.MothersWithPatients ?? 0,
                        MothersDischarged = pm.MothersDischarged ?? 0
                    })
                    .ToListAsync(ct);

                if (movements.Count == 0)
                {
                    Logger.Info("Отчёт 7 (часть 2) успешно сформирован. Количество записей: {0}", 0);
                    return new List<ReportSevenPart2DTO>();
                }

                var beds = await context.Beds
                    .AsNoTracking()
                    .Select(b => new BedRow
                    {
                        Id = b.Id,
                        DepartmentId = b.DepartmentId,
                        BedTypeId = b.BedTypeId,
                        DepartmentName = b.Department.DepartmentName,
                        BedTypeName = b.BedType.BedTypeName,
                        Amount = b.Amount,
                        EffectiveDate = b.EffectiveDate,
                        MonthlyPlan = b.MonthlyPlan,
                        NotUsedSince = b.NotUsedSince
                    })
                    .ToListAsync(ct);

                var bedById = beds.ToDictionary(b => b.Id, b => b);

                var includedMovements = movements
                    .Where(m => bedById.TryGetValue(m.BedId, out var bed) && IsBedIncludedInPeriod(bed.NotUsedSince, startDate, endDate, isYearlyReport))
                    .Select(m => new MovementJoinedPart2(m, bedById[m.BedId]))
                    .ToList();

                if (includedMovements.Count == 0)
                {
                    Logger.Info("Отчёт 7 (часть 2) успешно сформирован. Количество записей: {0}", 0);
                    return new List<ReportSevenPart2DTO>();
                }

                var planTotalsByDeptBedType = CalculatePlanTotalsByDeptBedType(startDate, endDate, beds, isYearlyReport);
                var averageBedCountByDeptBedType = CalculateAverageBedCountByDeptBedType(startDate, endDate, daysInPeriod, beds, isYearlyReport);

                var typeRows = includedMovements
                    .GroupBy(x => new { x.DepartmentId, x.DepartmentName, x.BedTypeId, x.BedTypeName })
                    .Select(g =>
                    {
                        var key = g.Key;

                        var factBedDays = g.Sum(x => x.TotalPatients);
                        var discharged = g.Sum(x => x.TotalDischarged);
                        var died = g.Sum(x => x.Died);
                        var transferred = g.Sum(x => x.TransferredToOtherDepartments);
                        var admitted = g.Sum(x => x.TotalAdmitted);
                        var fromVillage = g.Sum(x => x.FromVillagePatient);
                        var mothers = g.Sum(x => x.MothersWithPatients);
                        var dischargedMothers = g.Sum(x => x.MothersDischarged);

                        var dischargedPatients = discharged + died;
                        var treatedPatients = discharged + died + transferred;
                        var usedPatients = (admitted + discharged + died) / 2m;

                        var planForPeriod = planTotalsByDeptBedType.TryGetValue((key.DepartmentId, key.BedTypeId), out var plan) ? plan : 0;
                        var averageBedCount = averageBedCountByDeptBedType.TryGetValue((key.DepartmentId, key.BedTypeId), out var avg) ? avg : 0m;

                        decimal fulfillmentPercentage;
                        if (planForPeriod == 0 && factBedDays > 0) fulfillmentPercentage = 100m;
                        else if (planForPeriod == 0) fulfillmentPercentage = 0m;
                        else fulfillmentPercentage = RoundAwayFromZero((decimal)factBedDays * 100m / planForPeriod, 2);

                        var averageStay = treatedPatients == 0 ? 0m : RoundAwayFromZero((decimal)factBedDays / treatedPatients, 2);
                        var bedOccupancy = averageBedCount == 0m
                            ? 0m
                            : RoundAwayFromZero((decimal)factBedDays / (averageBedCount * daysInPeriod) * 100m, 2);

                        var bedTurnover = averageBedCount == 0m
                            ? 0m
                            : RoundAwayFromZero((decimal)treatedPatients / averageBedCount, 2);

                        var averageBedDowntime = (treatedPatients == 0 || averageBedCount == 0m)
                            ? 0m
                            : RoundAwayFromZero(
                                ((decimal)daysInPeriod - ((decimal)factBedDays / averageBedCount)) /
                                (((decimal)treatedPatients) / averageBedCount),
                                2);

                        var mortalityRate = dischargedPatients == 0
                            ? 0m
                            : RoundAwayFromZero((decimal)died * 100m / dischargedPatients, 2);

                        return new TypeRowPart2
                        {
                            DepartmentId = key.DepartmentId,
                            DepartmentName = key.DepartmentName,
                            BedTypeId = key.BedTypeId,
                            BedType = key.BedTypeName,

                            PlanForPeriod = planForPeriod,
                            FactBedDays = factBedDays,
                            FromVillage = fromVillage,
                            Mothers = mothers,
                            DischargedMothers = dischargedMothers,

                            FulfillmentPercentage = fulfillmentPercentage,
                            AverageStay = averageStay,
                            BedOccupancy = bedOccupancy,
                            BedTurnover = bedTurnover,
                            AverageBedDowntime = averageBedDowntime,
                            MortalityRate = mortalityRate,

                            Discharged = discharged,
                            Died = died,
                            DischargedPatients = dischargedPatients,
                            TreatedPatients = treatedPatients,
                            UsedPatients = usedPatients,
                            AverageBedCount = averageBedCount
                        };
                    })
                    .OrderBy(x => x.DepartmentName)
                    .ThenBy(x => x.BedType)
                    .ToList();

                var result = new List<ReportSevenPart2DTO>();
                var departmentNames = typeRows.Select(x => x.DepartmentName).Distinct().OrderBy(n => n).ToList();

                foreach (var deptName in departmentNames)
                {
                    var deptTypes = typeRows.Where(x => x.DepartmentName == deptName).OrderBy(x => x.BedType).ToList();

                    result.AddRange(deptTypes.Select(x => new ReportSevenPart2DTO
                    {
                        DepartmentName = x.DepartmentName,
                        BedType = x.BedType,
                        PlanForPeriod = x.PlanForPeriod,
                        FactBedDays = x.FactBedDays,
                        FromVillage = x.FromVillage,
                        Mothers = x.Mothers,
                        DischargedMothers = x.DischargedMothers,
                        FulfillmentPercentage = x.FulfillmentPercentage,
                        AverageStay = x.AverageStay,
                        BedOccupancy = x.BedOccupancy,
                        BedTurnover = x.BedTurnover,
                        AverageBedDowntime = x.AverageBedDowntime,
                        MortalityRate = x.MortalityRate,
                        DischargedPatients = x.DischargedPatients,
                        UsedPatients = x.UsedPatients
                    }));

                    var sumPlan = deptTypes.Sum(x => x.PlanForPeriod);
                    var sumFact = deptTypes.Sum(x => x.FactBedDays);
                    var sumFromVillage = deptTypes.Sum(x => x.FromVillage);
                    var sumMothers = deptTypes.Sum(x => x.Mothers);
                    var sumDischargedMothers = deptTypes.Sum(x => x.DischargedMothers);

                    var sumTreated = deptTypes.Sum(x => x.TreatedPatients);
                    var totalAverageBedCount = deptTypes.Sum(x => x.AverageBedCount);

                    var dischargedSum = deptTypes.Sum(x => x.Discharged);
                    var diedSum = deptTypes.Sum(x => x.Died);
                    var dischargedPatientsSum = dischargedSum + diedSum;

                    decimal deptFulfillmentPercentage;
                    if (sumPlan == 0 && sumFact > 0) deptFulfillmentPercentage = 100m;
                    else if (sumPlan == 0) deptFulfillmentPercentage = 0m;
                    else deptFulfillmentPercentage = RoundAwayFromZero((decimal)sumFact * 100m / sumPlan, 2);

                    var deptAverageStay = sumTreated == 0 ? 0m : RoundAwayFromZero((decimal)sumFact / sumTreated, 2);
                    var deptBedOccupancy = totalAverageBedCount == 0m
                        ? 0m
                        : RoundAwayFromZero((decimal)sumFact / (totalAverageBedCount * daysInPeriod) * 100m, 2);

                    var deptBedTurnover = totalAverageBedCount == 0m
                        ? 0m
                        : RoundAwayFromZero((decimal)sumTreated / totalAverageBedCount, 2);

                    var deptAverageBedDowntime = (sumTreated == 0 || totalAverageBedCount == 0m)
                        ? 0m
                        : RoundAwayFromZero(
                            ((decimal)daysInPeriod - ((decimal)sumFact / totalAverageBedCount)) /
                            (((decimal)sumTreated) / totalAverageBedCount),
                            2);

                    var deptMortalityRate = dischargedPatientsSum == 0
                        ? 0m
                        : RoundAwayFromZero((decimal)diedSum * 100m / dischargedPatientsSum, 2);

                    result.Add(new ReportSevenPart2DTO
                    {
                        DepartmentName = deptName,
                        BedType = "Total by department",
                        PlanForPeriod = sumPlan,
                        FactBedDays = sumFact,
                        FromVillage = sumFromVillage,
                        Mothers = sumMothers,
                        DischargedMothers = sumDischargedMothers,
                        FulfillmentPercentage = deptFulfillmentPercentage,
                        AverageStay = deptAverageStay,
                        BedOccupancy = deptBedOccupancy,
                        BedTurnover = deptBedTurnover,
                        AverageBedDowntime = deptAverageBedDowntime,
                        MortalityRate = deptMortalityRate,
                        DischargedPatients = dischargedPatientsSum,
                        UsedPatients = deptTypes.Sum(x => x.UsedPatients)
                    });
                }

                var generalTotalDepartmentName = "Total hospital";
                var generalPlan = typeRows.Sum(x => x.PlanForPeriod);
                var generalFact = typeRows.Sum(x => x.FactBedDays);
                var generalFromVillage = typeRows.Sum(x => x.FromVillage);
                var generalMothers = typeRows.Sum(x => x.Mothers);
                var generalDischargedMothers = typeRows.Sum(x => x.DischargedMothers);

                var generalTreated = typeRows.Sum(x => x.TreatedPatients);
                var hospitalAverageBedCount = typeRows.Sum(x => x.AverageBedCount);

                var dischargedSumAll = typeRows.Sum(x => x.Discharged);
                var diedSumAll = typeRows.Sum(x => x.Died);
                var dischargedPatientsAll = dischargedSumAll + diedSumAll;

                decimal fulfillmentPercentage;
                if (generalPlan == 0 && generalFact > 0) fulfillmentPercentage = 100m;
                else if (generalPlan == 0) fulfillmentPercentage = 0m;
                else fulfillmentPercentage = RoundAwayFromZero((decimal)generalFact * 100m / generalPlan, 2);

                var averageStay = generalTreated == 0 ? 0m : RoundAwayFromZero((decimal)generalFact / generalTreated, 2);
                var bedOccupancy = hospitalAverageBedCount == 0m
                    ? 0m
                    : RoundAwayFromZero((decimal)generalFact / (hospitalAverageBedCount * daysInPeriod) * 100m, 2);

                var bedTurnover = hospitalAverageBedCount == 0m
                    ? 0m
                    : RoundAwayFromZero((decimal)generalTreated / hospitalAverageBedCount, 2);

                var averageBedDowntime = (generalTreated == 0 || hospitalAverageBedCount == 0m)
                    ? 0m
                    : RoundAwayFromZero(
                        ((decimal)daysInPeriod - ((decimal)generalFact / hospitalAverageBedCount)) /
                        (((decimal)generalTreated) / hospitalAverageBedCount),
                        2);

                var mortalityRate = dischargedPatientsAll == 0
                    ? 0m
                    : RoundAwayFromZero((decimal)diedSumAll * 100m / dischargedPatientsAll, 2);

                result.Add(new ReportSevenPart2DTO
                {
                    DepartmentName = generalTotalDepartmentName,
                    BedType = "",
                    PlanForPeriod = generalPlan,
                    FactBedDays = generalFact,
                    FromVillage = generalFromVillage,
                    Mothers = generalMothers,
                    DischargedMothers = generalDischargedMothers,
                    FulfillmentPercentage = fulfillmentPercentage,
                    AverageStay = averageStay,
                    BedOccupancy = bedOccupancy,
                    BedTurnover = bedTurnover,
                    AverageBedDowntime = averageBedDowntime,
                    MortalityRate = mortalityRate,
                    DischargedPatients = dischargedPatientsAll,
                    UsedPatients = typeRows.Sum(x => x.UsedPatients)
                });

                Logger.Info("Отчёт 7 (часть 2) успешно сформирован. Количество записей: {0}", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка при формировании отчёта 7 (часть 2) за период {0} - {1}", startDate, endDate);
                throw;
            }
        }

        private static bool IsYearlyReport(DateOnly startDate, DateOnly endDate)
        {
            return startDate.Day == 1 &&
                   startDate.Month == 1 &&
                   endDate.Day == 31 &&
                   endDate.Month == 12 &&
                   startDate.Year == endDate.Year;
        }

        private static bool IsBedIncludedInPeriod(DateOnly? notUsedSince, DateOnly startDate, DateOnly endDate, bool isYearlyReport)
        {
            if (isYearlyReport)
                return true;

            if (notUsedSince == null)
                return true;

            if (notUsedSince.Value > endDate)
                return true;

            return notUsedSince.Value >= startDate && notUsedSince.Value <= endDate;
        }

        private static decimal RoundAwayFromZero(decimal value, int digits)
        {
            return Math.Round(value, digits, MidpointRounding.AwayFromZero);
        }

        private sealed class BedRow
        {
            public int Id { get; set; }
            public int DepartmentId { get; set; }
            public int BedTypeId { get; set; }
            public string DepartmentName { get; set; } = string.Empty;
            public string BedTypeName { get; set; } = string.Empty;
            public int Amount { get; set; }
            public DateOnly EffectiveDate { get; set; }
            public int MonthlyPlan { get; set; }
            public DateOnly? NotUsedSince { get; set; }
        }

        private sealed class MovementRowPart1
        {
            public DateOnly ReportDate { get; set; }
            public int BedId { get; set; }
            public int PatientsAtStartOfDay { get; set; }
            public int TotalAdmitted { get; set; }
            public int FromVillage { get; set; }
            public int ChildrenUnder14 { get; set; }
            public int TransferredFromOtherDepartments { get; set; }
            public int TransferredToOtherDepartments { get; set; }
            public int TotalDischarged { get; set; }
            public int DischargedByTransfer { get; set; }
            public int Died { get; set; }
            public int TotalPatients { get; set; }
            public int FromVillagePatient { get; set; }
            public int MothersWithPatients { get; set; }
        }

        private sealed class MovementRowPart2
        {
            public int BedId { get; set; }
            public int TotalPatients { get; set; }
            public int TotalDischarged { get; set; }
            public int Died { get; set; }
            public int TransferredToOtherDepartments { get; set; }
            public int TotalAdmitted { get; set; }
            public int FromVillagePatient { get; set; }
            public int MothersWithPatients { get; set; }
            public int MothersDischarged { get; set; }
        }

        private sealed class MovementJoinedPart1
        {
            public MovementJoinedPart1(MovementRowPart1 movement, BedRow bed)
            {
                Movement = movement;
                DepartmentId = bed.DepartmentId;
                BedTypeId = bed.BedTypeId;
                DepartmentName = bed.DepartmentName;
                BedTypeName = bed.BedTypeName;
            }

            public MovementRowPart1 Movement { get; }

            public int DepartmentId { get; }
            public int BedTypeId { get; }
            public string DepartmentName { get; }
            public string BedTypeName { get; }

            public DateOnly ReportDate => Movement.ReportDate;
            public int PatientsAtStartOfDay => Movement.PatientsAtStartOfDay;
            public int TotalAdmitted => Movement.TotalAdmitted;
            public int FromVillage => Movement.FromVillage;
            public int ChildrenUnder14 => Movement.ChildrenUnder14;
            public int TransferredFromOtherDepartments => Movement.TransferredFromOtherDepartments;
            public int TransferredToOtherDepartments => Movement.TransferredToOtherDepartments;
            public int TotalDischarged => Movement.TotalDischarged;
            public int DischargedByTransfer => Movement.DischargedByTransfer;
            public int Died => Movement.Died;
            public int TotalPatients => Movement.TotalPatients;
            public int FromVillagePatient => Movement.FromVillagePatient;
            public int MothersWithPatients => Movement.MothersWithPatients;
        }

        private sealed class MovementJoinedPart2
        {
            public MovementJoinedPart2(MovementRowPart2 movement, BedRow bed)
            {
                Movement = movement;
                DepartmentId = bed.DepartmentId;
                BedTypeId = bed.BedTypeId;
                DepartmentName = bed.DepartmentName;
                BedTypeName = bed.BedTypeName;
            }

            public MovementRowPart2 Movement { get; }

            public int DepartmentId { get; }
            public int BedTypeId { get; }
            public string DepartmentName { get; }
            public string BedTypeName { get; }

            public int TotalPatients => Movement.TotalPatients;
            public int TotalDischarged => Movement.TotalDischarged;
            public int Died => Movement.Died;
            public int TransferredToOtherDepartments => Movement.TransferredToOtherDepartments;
            public int TotalAdmitted => Movement.TotalAdmitted;
            public int FromVillagePatient => Movement.FromVillagePatient;
            public int MothersWithPatients => Movement.MothersWithPatients;
            public int MothersDischarged => Movement.MothersDischarged;
        }

        private sealed class TypeRowPart1
        {
            public string DepartmentName { get; set; } = string.Empty;
            public string BedType { get; set; } = string.Empty;
            public int BedCount { get; set; }
            public int PatientsAtStartOfDay { get; set; }
            public int TotalAdmitted { get; set; }
            public int FromVillage { get; set; }
            public int ChildrenUnder14 { get; set; }
            public int TransferredFromOtherDepartments { get; set; }
            public int TransferredToOtherDepartments { get; set; }
            public int TotalDischarged { get; set; }
            public int DischargedByTransfer { get; set; }
            public int Died { get; set; }
            public int TotalPatients { get; set; }
            public int FromVillagePatient { get; set; }
            public int MothersWithPatients { get; set; }
            public double MonthlyPlanPercent { get; set; }
            public int BedDaysTotal { get; set; }
            public int BedDaysFromVillage { get; set; }
            public int BedDaysMothers { get; set; }
            public int MonthlyPlanForTotals { get; set; }
        }

        private sealed class TypeRowPart2
        {
            public int DepartmentId { get; set; }
            public string DepartmentName { get; set; } = string.Empty;
            public int BedTypeId { get; set; }
            public string BedType { get; set; } = string.Empty;

            public int PlanForPeriod { get; set; }
            public int FactBedDays { get; set; }
            public int FromVillage { get; set; }
            public int Mothers { get; set; }
            public int DischargedMothers { get; set; }

            public decimal FulfillmentPercentage { get; set; }
            public decimal AverageStay { get; set; }
            public decimal BedOccupancy { get; set; }
            public decimal BedTurnover { get; set; }
            public decimal AverageBedDowntime { get; set; }
            public decimal MortalityRate { get; set; }

            public int Discharged { get; set; }
            public int Died { get; set; }
            public int DischargedPatients { get; set; }
            public int TreatedPatients { get; set; }
            public decimal UsedPatients { get; set; }
            public decimal AverageBedCount { get; set; }
        }

        private static IEnumerable<DateOnly> EnumerateDates(DateOnly startDate, DateOnly endDate)
        {
            for (var d = startDate; d <= endDate; d = d.AddDays(1))
                yield return d;
        }

        private Dictionary<(int DepartmentId, int BedTypeId), int> CalculatePlanTotalsByDeptBedType(
            DateOnly startDate,
            DateOnly endDate,
            List<BedRow> beds,
            bool isYearlyReport)
        {
            var result = new Dictionary<(int DepartmentId, int BedTypeId), int>();
            var days = EnumerateDates(startDate, endDate).ToList();

            var bedGroups = beds
                .GroupBy(b => new { b.DepartmentId, b.BedTypeId })
                .Select(g => new
                {
                    DepartmentId = g.Key.DepartmentId,
                    BedTypeId = g.Key.BedTypeId,
                    Records = g.OrderBy(r => r.EffectiveDate).ToList()
                })
                .ToList();

            foreach (var g in bedGroups)
            {
                int index = -1;
                decimal sumDailyPlan = 0m;

                for (int i = 0; i < days.Count; i++)
                {
                    var date = days[i];

                    while (index + 1 < g.Records.Count && g.Records[index + 1].EffectiveDate <= date)
                        index++;

                    var activeRecord = index >= 0 ? g.Records[index] : null;
                    if (activeRecord == null)
                        continue;

                    if (!IsBedIncludedInPeriod(activeRecord.NotUsedSince, startDate, endDate, isYearlyReport))
                        continue;

                    var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
                    sumDailyPlan += (decimal)activeRecord.MonthlyPlan / daysInMonth;
                }

                var rounded = RoundAwayFromZero(sumDailyPlan, 0);
                result[(g.DepartmentId, g.BedTypeId)] = (int)rounded;
            }

            return result;
        }

        private Dictionary<(int DepartmentId, int BedTypeId), int> CalculateFinalBedCountsByDeptBedType(
            DateOnly startDate,
            DateOnly endDate,
            List<BedRow> beds,
            bool isYearlyReport)
        {
            var result = new Dictionary<(int DepartmentId, int BedTypeId), int>();

            var bedGroups = beds
                .GroupBy(b => new { b.DepartmentId, b.BedTypeId })
                .Select(g => new
                {
                    DepartmentId = g.Key.DepartmentId,
                    BedTypeId = g.Key.BedTypeId,
                    Records = g.OrderBy(r => r.EffectiveDate).ToList()
                })
                .ToList();

            foreach (var g in bedGroups)
            {
                var activeRecord = g.Records
                    .Where(r => r.EffectiveDate <= endDate)
                    .OrderByDescending(r => r.EffectiveDate)
                    .FirstOrDefault();

                if (activeRecord == null)
                {
                    result[(g.DepartmentId, g.BedTypeId)] = 0;
                    continue;
                }

                var included = IsBedIncludedInPeriod(activeRecord.NotUsedSince, startDate, endDate, isYearlyReport);
                result[(g.DepartmentId, g.BedTypeId)] = included ? activeRecord.Amount : 0;
            }

            return result;
        }

        private Dictionary<(int DepartmentId, int BedTypeId), decimal> CalculateAverageBedCountByDeptBedType(
            DateOnly startDate,
            DateOnly endDate,
            int daysInPeriod,
            List<BedRow> beds,
            bool isYearlyReport)
        {
            var result = new Dictionary<(int DepartmentId, int BedTypeId), decimal>();
            var days = EnumerateDates(startDate, endDate).ToList();

            var bedGroups = beds
                .GroupBy(b => new { b.DepartmentId, b.BedTypeId })
                .Select(g => new
                {
                    DepartmentId = g.Key.DepartmentId,
                    BedTypeId = g.Key.BedTypeId,
                    Records = g.OrderBy(r => r.EffectiveDate).ToList()
                })
                .ToList();

            foreach (var g in bedGroups)
            {
                int index = -1;
                decimal sumBedCounts = 0m;

                for (int i = 0; i < days.Count; i++)
                {
                    var date = days[i];

                    while (index + 1 < g.Records.Count && g.Records[index + 1].EffectiveDate <= date)
                        index++;

                    var activeRecord = index >= 0 ? g.Records[index] : null;
                    if (activeRecord == null)
                        continue;

                    if (!IsBedIncludedInPeriod(activeRecord.NotUsedSince, startDate, endDate, isYearlyReport))
                        continue;

                    sumBedCounts += activeRecord.Amount;
                }

                var average = daysInPeriod == 0 ? 0m : sumBedCounts / daysInPeriod;
                result[(g.DepartmentId, g.BedTypeId)] = average;
            }

            return result;
        }

    }
}
