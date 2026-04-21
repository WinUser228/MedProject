using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Repository.Interfaces;
using HospitalStatistics.Core.Services.Interfaces.Entities;
using NLog;

namespace HospitalStatistics.Core.Services.Entities
{
    public class ListPatientMovementService : IListPatientMovementService
    {
        private readonly IListPatientMovementRepository _repository;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public ListPatientMovementService(IListPatientMovementRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger.Info("ListPatientMovementService инициализирован.");
        }

        public async Task<int> CreateListPatientMovementAsync(ListPatientMovements entity, CancellationToken ct)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            _logger.Info("Попытка создать запись движения пациентов для койки ID={0} на дату {1}", entity.BedId, entity.ReportDate);
            try
            {
                var id = await _repository.CreateAsync(entity, ct);
                _logger.Info("Запись успешно создана с ID={0}", id);
                return id;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Создание записи отменено.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при создании записи движения пациентов.");
                throw;
            }
        }

        public async Task<int> UpdateListPatientMovementAsync(int id, ListPatientMovements entity, CancellationToken ct)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            _logger.Info("Попытка обновить запись движения пациентов ID={0}", id);
            var existing = await _repository.GetByIdAsync(id, ct);
            if (existing == null)
            {
                _logger.Warn("Запись ID={0} не найдена для обновления", id);
                throw new ArgumentException("Запись не найдена");
            }

            // Обновляем поля напрямую
            existing.PatientsAtStartOfDay = entity.PatientsAtStartOfDay;
            existing.TotalAdmitted = entity.TotalAdmitted;
            existing.FromVillage = entity.FromVillage;
            existing.ChildrenUnder14 = entity.ChildrenUnder14;
            existing.TransferredFromOtherDepartments = entity.TransferredFromOtherDepartments;
            existing.TransferredToOtherDepartments = entity.TransferredToOtherDepartments;
            existing.TotalDischarged = entity.TotalDischarged;
            existing.DischargedByTransfer = entity.DischargedByTransfer;
            existing.Died = entity.Died;
            existing.FromVillagePatient = entity.FromVillagePatient;
            existing.MothersWithPatients = entity.MothersWithPatients;
            existing.MothersDischarged = entity.MothersDischarged;
            existing.TotalPatients = entity.TotalPatients;

            try
            {
                await _repository.UpdateAsync(existing, ct);
                _logger.Info("Запись ID={0} успешно обновлена", id);
                return id;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Обновление записи ID={0} отменено", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при обновлении записи ID={0}", id);
                throw;
            }
        }

        public async Task<ListPatientMovements?> GetListPatientMovementByIdAsync(int id, CancellationToken ct)
        {
            _logger.Info("Получение записи движения пациентов по ID={0}", id);
            try
            {
                var entity = await _repository.GetByIdAsync(id, ct);
                if (entity == null)
                {
                    _logger.Warn("Запись ID={0} не найдена", id);
                }
                else
                {
                    _logger.Info("Запись ID={0} успешно получена", id);
                }
                return entity;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Получение записи ID={0} отменено", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при получении записи ID={0}", id);
                throw;
            }
        }

        public async Task<List<ListPatientMovements>> GetAllListPatientMovementAsync(CancellationToken ct)
        {
            _logger.Info("Получение всех записей движения пациентов...");
            try
            {
                var list = await _repository.GetAllAsync(ct);
                _logger.Info("Найдено {0} записей", list.Count);
                return list;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Получение всех записей отменено");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при получении всех записей движения пациентов");
                throw;
            }
        }

        public async Task<bool> DeleteListPatientMovementAsync(int id, CancellationToken ct)
        {
            _logger.Info("Попытка удалить запись движения пациентов ID={0}", id);
            var entity = await _repository.GetByIdAsync(id, ct);
            if (entity == null)
            {
                _logger.Warn("Запись ID={0} не найдена для удаления", id);
                return false;
            }

            try
            {
                await _repository.DeleteAsync(entity, ct);
                _logger.Info("Запись ID={0} успешно удалена", id);
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Удаление записи ID={0} отменено", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при удалении записи ID={0}", id);
                throw;
            }
        }

        public async Task SaveListPatientMovementsAsync(List<ListPatientMovements> entries, CancellationToken ct)
        {
            if (entries == null || !entries.Any())
                throw new ArgumentException("Нет данных для сохранения.");

            _logger.Info("Сохранение {0} записей движения пациентов", entries.Count);

            foreach (var entry in entries)
            {
                var existingEntity = await _repository.GetByBedIdAndReportDateAsync(entry.BedId, entry.ReportDate, ct);

                if (existingEntity != null)
                {
                    // Обновляем существующую запись
                    existingEntity.PatientsAtStartOfDay = entry.PatientsAtStartOfDay;
                    existingEntity.TotalAdmitted = entry.TotalAdmitted;
                    existingEntity.FromVillage = entry.FromVillage;
                    existingEntity.ChildrenUnder14 = entry.ChildrenUnder14;
                    existingEntity.TransferredFromOtherDepartments = entry.TransferredFromOtherDepartments;
                    existingEntity.TransferredToOtherDepartments = entry.TransferredToOtherDepartments;
                    existingEntity.TotalDischarged = entry.TotalDischarged;
                    existingEntity.DischargedByTransfer = entry.DischargedByTransfer;
                    existingEntity.Died = entry.Died;
                    existingEntity.FromVillagePatient = entry.FromVillagePatient;
                    existingEntity.MothersWithPatients = entry.MothersWithPatients;
                    existingEntity.MothersDischarged = entry.MothersDischarged;
                    existingEntity.TotalPatients = entry.TotalPatients;

                    await _repository.UpdateAsync(existingEntity, ct);
                    _logger.Debug("Обновлена запись для койки ID={0} на дату {1}", entry.BedId, entry.ReportDate);
                }
                else
                {
                    // Создаем новую запись напрямую
                    await _repository.CreateAsync(entry, ct);
                    _logger.Debug("Создана новая запись для койки ID={0} на дату {1}", entry.BedId, entry.ReportDate);
                }
            }
        }

        public async Task<List<ListPatientMovements>> GetListPatientMovementsByDateAndBedIdsAsync(DateOnly date, List<int> bedIds, CancellationToken ct)
        {
            _logger.Info("Получение записей движения пациентов за {0} для койк ID: {1}", date, string.Join(",", bedIds));
            return await _repository.GetByDateAndBedIdsAsync(date, bedIds, ct);
        }

        public async Task<ListPatientMovements?> GetListPatientMovementByBedAndDateAsync(int bedId, DateOnly date, CancellationToken ct)
        {
            _logger.Info("Получение записи движения пациентов для койки ID={0} на дату {1}", bedId, date);
            return await _repository.GetByBedIdAndReportDateAsync(bedId, date, ct);
        }

        public IListPatientMovementRepository GetMovementRepositoryForDate(DateOnly date)
        {
            return _repository;
        }

        public async Task<bool> HasMovementsForBedAsync(int bedId, CancellationToken ct)
        {
            _logger.Info("Проверка наличия движений пациентов для койки ID={0}", bedId);
            return await _repository.HasMovementsForBedAsync(bedId, ct);
        }
    }



}
