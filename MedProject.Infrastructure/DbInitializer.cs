using HospitalStatistics.Core;
using HospitalStatistics.Core.Entities;
using HospitalStatistics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NLog;

namespace HospitalStatistics.Infrastructure
{
    public class DbInitializer : IDbInitializer
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IDbContextFactory<HospitalContext> _contextFactory;

        public DbInitializer(IDbContextFactory<HospitalContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            try
            {
                Logger.Info("Запуск инициализации базы данных...");
                Logger.Info("Создание SQLite базы данных (EnsureCreated)...");
                await using var context = await _contextFactory.CreateDbContextAsync(ct);
                await context.Database.EnsureCreatedAsync(ct);
                Logger.Info("SQLite база данных готова.");

                // Заполнение seed данных
                Logger.Info("Заполнение seed данных...");
                await SeedDataAsync(context, ct);
                Logger.Info("Seed данные заполнены.");

                Logger.Info("Инициализация базы данных завершена.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка при инициализации базы данных.");
                throw;
            }
        }

        private static async Task SeedDataAsync(HospitalContext context, CancellationToken ct)
        {
            // Проверяем, есть ли уже данные (проверяем все таблицы)
            bool hasBedTypes = await context.BedTypes.AnyAsync(ct);
            bool hasDepartments = await context.Departments.AnyAsync(ct);
            bool hasBeds = await context.Beds.AnyAsync(ct);

            if (hasBedTypes || hasDepartments || hasBeds)
            {
                Logger.Info($"База данных уже содержит данные. BedTypes: {hasBedTypes}, Departments: {hasDepartments}, Beds: {hasBeds}. Seed пропущен.");
                return;
            }

            // Очищаем трекер изменений перед началом seed
            context.ChangeTracker.Clear();

            Logger.Info("Начало заполнения seed данных...");

            // 1. Создаем типы коек согласно документу
            var bedTypes = new List<BedTypes>
            {
                new BedTypes { BedTypeName = "Хирургические" },
                new BedTypes { BedTypeName = "Уроандрологические" },
                new BedTypes { BedTypeName = "Травматологические" },
                new BedTypes { BedTypeName = "Ортопедические" },
                new BedTypes { BedTypeName = "Офтальмологические" },
                new BedTypes { BedTypeName = "Отоларингологические" }
            };

            await context.BedTypes.AddRangeAsync(bedTypes, ct);
            await context.SaveChangesAsync(ct);
            Logger.Info($"Создано типов коек: {bedTypes.Count}");

            // Очищаем трекер и перезагружаем типы коек из БД, чтобы получить актуальные ID
            // Используем AsNoTracking, чтобы EF не отслеживал эти объекты
            context.ChangeTracker.Clear();
            var bedTypesFromDb = await context.BedTypes.AsNoTracking().ToListAsync(ct);
            var bedTypesDict = bedTypesFromDb.ToDictionary(bt => bt.BedTypeName, bt => bt);

            // 2. Создаем отделения согласно документу
            // Структура: отделение -> список типов коек
            // Если тип койки один, то он называется как отделение
            var departmentBedTypeMapping = new List<(string DepartmentName, List<string> BedTypeNames)>
            {
                ("Детское хирургическое", new List<string> { "Хирургические" }),
                ("Детское уролого-андрологическое", new List<string> { "Уроандрологические" }),
                ("Травматология и ортопедия", new List<string> { "Травматологические", "Ортопедические" }),
                ("Детское офтальмологическое", new List<string> { "Офтальмологические" }),
                ("Отоларингологическое", new List<string> { "Отоларингологические" })
            };

            var departments = new List<Departments>();
            var beds = new List<Beds>();
            var random = new Random(42); // Фиксированный seed для воспроизводимости

            // 3. Создаем отделения и соответствующие койки
            foreach (var (deptName, bedTypeNames) in departmentBedTypeMapping)
            {
                var department = new Departments { DepartmentName = deptName };
                departments.Add(department);
            }

            await context.Departments.AddRangeAsync(departments, ct);
            await context.SaveChangesAsync(ct);
            Logger.Info($"Создано отделений: {departments.Count}");

            // Очищаем трекер и перезагружаем отделения из БД, чтобы получить актуальные ID
            // Используем AsNoTracking, чтобы EF не отслеживал эти объекты
            context.ChangeTracker.Clear();
            var departmentsFromDb = await context.Departments.AsNoTracking().ToListAsync(ct);

            // 4. Создаем по одной записи для каждого типа койки с датой действия 1 января 2025
            // Всего должно быть 6 записей (по одной для каждого типа койки)
            foreach (var bedType in bedTypesFromDb)
            {
                // Находим отделение, которое использует этот тип койки
                var departmentMapping = departmentBedTypeMapping.FirstOrDefault(m => m.BedTypeNames.Contains(bedType.BedTypeName));
                if (departmentMapping.DepartmentName != null)
                {
                    var departmentIndex = departmentBedTypeMapping.IndexOf(departmentMapping);
                    if (departmentIndex >= 0 && departmentIndex < departmentsFromDb.Count)
                    {
                        var department = departmentsFromDb[departmentIndex];
                        int baseAmount = random.Next(15, 35);
                        int baseMonthlyPlan = random.Next(300, 600);

                        beds.Add(new Beds
                        {
                            DepartmentId = department.Id,
                            BedTypeId = bedType.Id,
                            Amount = baseAmount,
                            EffectiveDate = new DateOnly(2025, 1, 1),
                            MonthlyPlan = baseMonthlyPlan
                        });

                        Logger.Info($"Создана запись для типа койки {bedType.BedTypeName} в отделении {department.DepartmentName} с датой действия 01.01.2025");
                    }
                }
            }

            // 5. Добавляем 2 записи где план меняется (для 2 разных типов коек)
            // Первая запись с изменением плана - для "Хирургические" койки
            if (bedTypesDict.TryGetValue("Хирургические", out var surgicalBedType))
            {
                var surgicalDeptMapping = departmentBedTypeMapping.FirstOrDefault(m => m.BedTypeNames.Contains("Хирургические"));
                if (surgicalDeptMapping.DepartmentName != null)
                {
                    var surgicalDeptIndex = departmentBedTypeMapping.IndexOf(surgicalDeptMapping);
                    if (surgicalDeptIndex >= 0 && surgicalDeptIndex < departmentsFromDb.Count)
                    {
                        var surgicalDept = departmentsFromDb[surgicalDeptIndex];
                        beds.Add(new Beds
                        {
                            DepartmentId = surgicalDept.Id,
                            BedTypeId = surgicalBedType.Id,
                            Amount = random.Next(15, 35),
                            EffectiveDate = new DateOnly(2025, 6, 1), // План меняется с 1 июня
                            MonthlyPlan = random.Next(300, 600)
                        });
                        Logger.Info($"Добавлена запись с изменением плана для типа койки Хирургические в отделении {surgicalDept.DepartmentName} с датой действия {new DateOnly(2025, 6, 1):dd.MM.yyyy}");
                    }
                }
            }

            // Вторая запись с изменением плана - для "Офтальмологические" койки
            if (bedTypesDict.TryGetValue("Офтальмологические", out var ophthalmologicalBedType))
            {
                var ophthalmologicalDeptMapping = departmentBedTypeMapping.FirstOrDefault(m => m.BedTypeNames.Contains("Офтальмологические"));
                if (ophthalmologicalDeptMapping.DepartmentName != null)
                {
                    var ophthalmologicalDeptIndex = departmentBedTypeMapping.IndexOf(ophthalmologicalDeptMapping);
                    if (ophthalmologicalDeptIndex >= 0 && ophthalmologicalDeptIndex < departmentsFromDb.Count)
                    {
                        var ophthalmologicalDept = departmentsFromDb[ophthalmologicalDeptIndex];
                        beds.Add(new Beds
                        {
                            DepartmentId = ophthalmologicalDept.Id,
                            BedTypeId = ophthalmologicalBedType.Id,
                            Amount = random.Next(15, 35),
                            EffectiveDate = new DateOnly(2025, 9, 1), // План меняется с 1 сентября
                            MonthlyPlan = random.Next(300, 600)
                        });
                        Logger.Info($"Добавлена запись с изменением плана для типа койки Офтальмологические в отделении {ophthalmologicalDept.DepartmentName} с датой действия {new DateOnly(2025, 9, 1):dd.MM.yyyy}");
                    }
                }
            }

            // 6. Для отделения "Травматология и ортопедия" для одного из типов коек устанавливаем NotUsedSince
            // Выбираем "Травматологические" койки
            var traumaOrthoDeptMapping = departmentBedTypeMapping.FirstOrDefault(m => m.DepartmentName == "Травматология и ортопедия");
            if (traumaOrthoDeptMapping.DepartmentName != null)
            {
                var traumaOrthoDeptIndex = departmentBedTypeMapping.IndexOf(traumaOrthoDeptMapping);
                if (traumaOrthoDeptIndex >= 0 && traumaOrthoDeptIndex < departmentsFromDb.Count)
                {
                    var traumaOrthoDept = departmentsFromDb[traumaOrthoDeptIndex];

                    // Находим запись для "Травматологические" койки с датой 01.01.2025 и добавляем NotUsedSince
                    if (bedTypesDict.TryGetValue("Травматологические", out var traumaBedType))
                    {
                        // Находим существующую запись для травматологических коек с датой 01.01.2025
                        var existingTraumaBed = beds.FirstOrDefault(b =>
                            b.DepartmentId == traumaOrthoDept.Id &&
                            b.BedTypeId == traumaBedType.Id &&
                            b.EffectiveDate == new DateOnly(2025, 1, 1));

                        if (existingTraumaBed != null)
                        {
                            // Устанавливаем NotUsedSince для этой записи
                            existingTraumaBed.NotUsedSince = new DateOnly(2025, 3, 15); // Койка не используется с 15 марта
                            Logger.Info($"Установлен NotUsedSince для травматологических коек в отделении {traumaOrthoDept.DepartmentName} с датой {new DateOnly(2025, 3, 15):dd.MM.yyyy}");
                        }
                    }
                }
            }

            // Добавляем койки в базу данных
            // Проверка на существование уже выполнена в начале метода
            if (beds.Count > 0)
            {
                // Убеждаемся, что трекер очищен перед добавлением
                context.ChangeTracker.Clear();

                // Убеждаемся, что у всех объектов Beds ID = 0 (не задан явно)
                // И что навигационные свойства не установлены (чтобы избежать отслеживания)
                int bedsWithId = 0;
                foreach (var bed in beds)
                {
                    if (bed.Id != 0)
                    {
                        bedsWithId++;
                        Logger.Warn($"Обнаружен объект Beds с явно заданным ID={bed.Id}. Сбрасываем ID.");
                        bed.Id = 0; // Сбрасываем ID, чтобы EF сгенерировал его автоматически
                    }
                    // Убеждаемся, что навигационные свойства не установлены
                    bed.Department = null;
                    bed.BedType = null;
                    bed.PatientMovements = null;
                }

                if (bedsWithId > 0)
                {
                    Logger.Warn($"Обнаружено {bedsWithId} объектов Beds с явно заданными ID. Все ID сброшены.");
                }

                Logger.Info($"Добавление {beds.Count} коек в контекст...");

                // Добавляем объекты в контекст
                // AddRangeAsync автоматически устанавливает состояние как Added
                await context.Beds.AddRangeAsync(beds, ct);

                // Проверяем состояние объектов перед сохранением (для диагностики)
                var addedBeds = context.ChangeTracker.Entries<Beds>()
                    .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added)
                    .Count();
                Logger.Info($"Объектов Beds в состоянии Added: {addedBeds} из {beds.Count}");

                try
                {
                    await context.SaveChangesAsync(ct);
                    Logger.Info($"Создано коек: {beds.Count}");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Ошибка при сохранении коек. Количество объектов: {beds.Count}");
                    // Логируем детали о первых нескольких объектах для диагностики
                    for (int i = 0; i < Math.Min(3, beds.Count); i++)
                    {
                        var bed = beds[i];
                        Logger.Error($"Объект Beds #{i}: Id={bed.Id}, DepartmentId={bed.DepartmentId}, BedTypeId={bed.BedTypeId}, EffectiveDate={bed.EffectiveDate}");
                    }
                    throw;
                }
            }
            else
            {
                Logger.Info("Нет коек для создания.");
            }

            // Очищаем трекер и перезагружаем койки из БД, чтобы получить актуальные ID
            context.ChangeTracker.Clear();
            var bedsFromDb = await context.Beds.AsNoTracking().ToListAsync(ct);

            // 7. Создаем данные о движении пациентов за весь 2025 год (январь - ноябрь)
            var year = 2025;
            var movements = new List<ListPatientMovements>();

            // Массив с количеством дней в каждом месяце
            int[] daysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30 }; // Январь - ноябрь

            Logger.Info($"Начинаем создание данных о движении пациентов для {bedsFromDb.Count} коек...");

            foreach (var bed in bedsFromDb)
            {
                Logger.Info($"Обработка койки ID={bed.Id}, DepartmentId={bed.DepartmentId}, BedTypeId={bed.BedTypeId}");

                // Начальное количество пациентов на 1 января
                int currentPatients = random.Next(5, bed.Amount / 2 + 1);

                // Проходим по всем месяцам с января по ноябрь
                for (int month = 1; month <= 11; month++)
                {
                    int daysInCurrentMonth = daysInMonth[month - 1];

                    for (int day = 1; day <= daysInCurrentMonth; day++)
                    {
                        var reportDate = new DateOnly(year, month, day);

                        // Генерируем реалистичные данные с учетом всех правил валидации

                        // 1. PatientsAtStartOfDay: должно быть <= bed.Amount и >= 0
                        int patientsAtStart = Math.Min(Math.Max(0, currentPatients), bed.Amount);

                        // 2. TotalAdmitted: должно быть >= 0 и (PatientsAtStartOfDay + TotalAdmitted) <= bed.Amount
                        int maxAdmitted = Math.Max(0, bed.Amount - patientsAtStart);
                        int totalAdmitted = maxAdmitted > 0 ? random.Next(0, Math.Min(8, maxAdmitted + 1)) : 0;

                        // 3. FromVillage: должно быть <= TotalAdmitted
                        int fromVillage = totalAdmitted > 0 ? random.Next(0, totalAdmitted + 1) : 0;

                        // 4. ChildrenUnder14: должно быть <= TotalAdmitted
                        int childrenUnder14 = totalAdmitted > 0 ? random.Next(0, totalAdmitted + 1) : 0;

                        // 5. TransferredFromOtherDepartments: должно быть <= bed.Amount - (patientsAtStart + totalAdmitted)
                        int maxTransferredFrom = Math.Max(0, bed.Amount - (patientsAtStart + totalAdmitted));
                        int transferredFrom = maxTransferredFrom > 0 ? random.Next(0, Math.Min(3, maxTransferredFrom + 1)) : 0;

                        // 6. Рассчитываем TotalPatients для проверки TransferredTo
                        int tempTotalPatients = patientsAtStart + totalAdmitted + transferredFrom;
                        tempTotalPatients = Math.Max(0, Math.Min(tempTotalPatients, bed.Amount));

                        // 7. TransferredToOtherDepartments: должно быть <= TotalPatients
                        int maxTransferredTo = tempTotalPatients;
                        int transferredTo = maxTransferredTo > 0 ? random.Next(0, Math.Min(3, maxTransferredTo + 1)) : 0;

                        // 8. TotalDischarged: должно быть <= PatientsAtStartOfDay (правило валидации)
                        // Также не может быть больше общего количества пациентов
                        int maxDischarged = Math.Min(patientsAtStart, patientsAtStart + totalAdmitted + transferredFrom);
                        int totalDischarged = maxDischarged > 0 ? random.Next(0, Math.Min(7, maxDischarged + 1)) : 0;

                        // 9. DischargedByTransfer: должно быть <= TotalDischarged
                        int dischargedByTransfer = totalDischarged > 0 ? random.Next(0, Math.Min(totalDischarged + 1, 3)) : 0;

                        // 10. Died: должно быть <= PatientsAtStartOfDay и <= (patientsAtStart + totalAdmitted + transferredFrom)
                        int maxDied = Math.Min(patientsAtStart, patientsAtStart + totalAdmitted + transferredFrom);
                        int died = 0;
                        if (maxDied > 0 && random.Next(0, 100) < 5)
                        {
                            died = 1; // Максимум 1 смерть в день, только в 5% случаев
                        }

                        // 11. Рассчитываем финальное TotalPatients
                        int totalPatients = patientsAtStart + totalAdmitted + transferredFrom
                                          - totalDischarged - transferredTo - died;
                        totalPatients = Math.Max(0, Math.Min(totalPatients, bed.Amount));

                        // 12. FromVillagePatient: должно быть <= TotalPatients и <= bed.Amount
                        int maxFromVillagePatient = Math.Min(totalPatients, bed.Amount);
                        int fromVillagePatient = maxFromVillagePatient > 0
                            ? random.Next((int)(maxFromVillagePatient * 0.3), (int)(maxFromVillagePatient * 0.5) + 1)
                            : 0;
                        fromVillagePatient = Math.Min(fromVillagePatient, totalPatients);

                        // 13. MothersWithPatients: должно быть <= TotalPatients (все пациенты - дети в детском отделении)
                        int maxMothersWithPatients = totalPatients;
                        int mothersWithPatients = maxMothersWithPatients > 0
                            ? random.Next((int)(maxMothersWithPatients * 0.2), (int)(maxMothersWithPatients * 0.4) + 1)
                            : 0;
                        mothersWithPatients = Math.Min(mothersWithPatients, totalPatients);

                        // 14. MothersDischarged: должно быть <= TotalDischarged и <= MothersWithPatients
                        int maxMothersDischarged = Math.Min(totalDischarged, mothersWithPatients);
                        int mothersDischarged = maxMothersDischarged > 0
                            ? random.Next(0, Math.Min(maxMothersDischarged, 2) + 1)
                            : 0;
                        mothersDischarged = Math.Min(mothersDischarged, totalDischarged);

                        movements.Add(new ListPatientMovements
                        {
                            ReportDate = reportDate,
                            BedId = bed.Id,
                            PatientsAtStartOfDay = patientsAtStart,
                            TotalAdmitted = totalAdmitted,
                            FromVillage = fromVillage > 0 ? fromVillage : null,
                            ChildrenUnder14 = childrenUnder14 > 0 ? childrenUnder14 : null,
                            TransferredFromOtherDepartments = transferredFrom > 0 ? transferredFrom : null,
                            TransferredToOtherDepartments = transferredTo > 0 ? transferredTo : null,
                            TotalDischarged = totalDischarged,
                            DischargedByTransfer = dischargedByTransfer > 0 ? dischargedByTransfer : null,
                            Died = died > 0 ? died : null,
                            TotalPatients = totalPatients,
                            FromVillagePatient = fromVillagePatient > 0 ? fromVillagePatient : null,
                            MothersWithPatients = mothersWithPatients > 0 ? mothersWithPatients : null,
                            MothersDischarged = mothersDischarged > 0 ? mothersDischarged : null
                        });

                        // Обновляем текущее количество пациентов для следующего дня
                        currentPatients = totalPatients;
                    }
                }
            }

            // Сохраняем все движения пациентов после обработки всех коек
            if (movements.Count > 0)
            {
                Logger.Info($"Всего создано записей о движении пациентов: {movements.Count}. Начинаем сохранение...");

                // Разбиваем на батчи для более эффективной вставки
                const int batchSize = 1000;
                int totalSaved = 0;

                for (int i = 0; i < movements.Count; i += batchSize)
                {
                    var batch = movements.Skip(i).Take(batchSize).ToList();

                    await context.PatientMovements.AddRangeAsync(batch, ct);
                    await context.SaveChangesAsync(ct);

                    // Очищаем трекер после каждого батча для освобождения памяти
                    context.ChangeTracker.Clear();

                    totalSaved += batch.Count;
                    Logger.Info($"Добавлено записей о движении пациентов: {totalSaved} / {movements.Count}");
                }

                Logger.Info($"Создано записей о движении пациентов за 2025 год (январь - ноябрь): {movements.Count} для {bedsFromDb.Count} коек");
            }
            else
            {
                Logger.Info("Нет данных о движении пациентов для создания.");
            }
        }

    }
}
