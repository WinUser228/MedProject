using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Exceptions;
using HospitalStatistics.Core.Repository.Interfaces;
using HospitalStatistics.Core.Services.Interfaces.Entities;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace HospitalStatistics.Core.Services.Entities
{
    public class BedService : IBedService
    {
        private readonly IBedRepository _repository;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public BedService(IBedRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger.Info("BedService инициализирован.");
        }

        public async Task<int> CreateBedAsync(Beds bed, CancellationToken ct)
        {
            if (bed == null) throw new ArgumentNullException(nameof(bed));

            _logger.Info("Попытка создать новую койку.");
            try
            {
                // Закрываем все ранее открытые записи (NotUsedSince == null)
                // для той же пары отделение+тип с более ранней датой начала
                var existing = await _repository.GetBedsByDepartmentIdAsync(bed.DepartmentId, ct);
                var toClose = existing
                    .Where(b => b.BedTypeId == bed.BedTypeId
                             && b.NotUsedSince == null
                             && b.EffectiveDate < bed.EffectiveDate)
                    .ToList();

                foreach (var prev in toClose)
                {
                    prev.NotUsedSince = bed.EffectiveDate;
                    await _repository.UpdateAsync(prev, ct);
                    _logger.Info("Закрыта предыдущая запись ID={0}: NotUsedSince={1}", prev.Id, prev.NotUsedSince);
                }

                var newId = await _repository.CreateAsync(bed, ct);
                _logger.Info("Койка успешно создана с ID: {0}", newId);
                return newId;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Создание койки отменено.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при создании койки.");
                throw;
            }
        }

        public async Task<int> UpdateBedAsync(int id, Beds bed, CancellationToken ct)
        {
            if (bed == null) throw new ArgumentNullException(nameof(bed));

            _logger.Info("Попытка обновить койку с ID: {0}", id);

            var existing = await _repository.GetByIdAsync(id, ct);
            if (existing == null)
            {
                _logger.Warn("Койка с ID {0} не найдена для обновления.", id);
                throw new ArgumentException($"Койка с ID {id} не найдена.");
            }

            // Обновляем поля
            existing.DepartmentId = bed.DepartmentId;
            existing.BedTypeId = bed.BedTypeId;
            existing.Amount = bed.Amount;
            existing.EffectiveDate = bed.EffectiveDate;
            existing.MonthlyPlan = bed.MonthlyPlan;
            existing.NotUsedSince = bed.NotUsedSince;

            try
            {
                await _repository.UpdateAsync(existing, ct);
                _logger.Info("Койка с ID {0} успешно обновлена.", id);
                return id;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Обновление койки с ID {0} отменено.", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при обновлении койки с ID: {0}", id);
                throw;
            }
        }


        public async Task<Beds?> GetBedByIdAsync(int id, CancellationToken ct)
        {
            _logger.Info("Попытка получить койку с ID: {0}", id);
            try
            {
                var bed = await _repository.GetByIdAsync(id, ct);
                if (bed == null)
                {
                    _logger.Warn("Койка с ID {0} не найдена.", id);
                    return null;
                }
                _logger.Info("Койка с ID {0} успешно получена.", id);
                return bed;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Получение койки с ID {0} отменено.", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при получении койки с ID: {0}", id);
                throw;
            }
        }

        public async Task<List<Beds>> GetAllBedAsync(CancellationToken ct)
        {
            _logger.Info("Попытка получить все койки.");
            try
            {
                var beds = await _repository.GetAllAsync(ct);
                _logger.Info("Получено {0} коек.", beds.Count);
                return beds;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Получение всех коек отменено.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при получении всех коек.");
                throw;
            }
        }

        public async Task<bool> DeleteBedAsync(int id, CancellationToken ct)
        {
            _logger.Info("Попытка удалить койку с ID: {0}", id);
            var bed = await _repository.GetByIdAsync(id, ct);
            if (bed == null)
            {
                _logger.Warn("Койка с ID {0} не найдена для удаления.", id);
                return false;
            }

            try
            {
                await _repository.DeleteAsync(bed, ct);
                _logger.Info("Койка с ID {0} успешно удалена.", id);
                return true;
            }
            catch (DbUpdateException dbEx)
            {
                if (IsForeignKeyViolation(dbEx))
                {
                    _logger.Warn(dbEx, "Попытка удалить койку с ID {0}, которая имеет связанные записи.", id);
                    throw new RelatedRecordsExistException(
                        "Невозможно удалить эту запись коек, так как по ней есть данные о движении пациентов.", dbEx.InnerException ?? dbEx);
                }
                _logger.Error(dbEx, "Ошибка обновления базы при удалении койки с ID: {0}", id);
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Удаление койки с ID {0} отменено.", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Неожиданная ошибка при удалении койки с ID: {0}", id);
                throw;
            }
        }

        public async Task<List<Beds>> GetBedsByDepartmentIdAsync(int departmentId, CancellationToken ct)
        {
            _logger.Info("Попытка получить койки по ID отделения: {0}", departmentId);
            try
            {
                var beds = await _repository.GetBedsByDepartmentIdAsync(departmentId, ct);
                _logger.Info("Получено {0} коек для отделения ID {1}.", beds.Count, departmentId);
                return beds;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Получение коек по ID отделения {0} отменено.", departmentId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при получении коек по ID отделения: {0}", departmentId);
                throw;
            }
        }

        public async Task<List<Beds>> GetAllBedsWithDepartmentsAsync(CancellationToken ct)
        {
            _logger.Info("Попытка получить все койки с информацией об отделениях.");
            try
            {
                var beds = await _repository.GetAllBedsWithDepartmentsAsync(ct);
                _logger.Info("Получено {0} коек с информацией об отделениях.", beds.Count);
                return beds;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Получение всех коек с отделениями отменено.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при получении всех коек с информацией об отделениях.");
                throw;
            }
        }

        public async Task<Beds?> GetBedWithDepartmentByIdAsync(int id, CancellationToken ct)
        {
            _logger.Info("Попытка получить койку с отделением по ID: {0}", id);
            try
            {
                var bed = await _repository.GetBedWithDepartmentByIdAsync(id, ct);
                if (bed == null)
                {
                    _logger.Warn("Койка с отделением по ID {0} не найдена.", id);
                    return null;
                }
                _logger.Info("Койка с отделением по ID {0} успешно получена.", id);
                return bed;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Получение койки с отделением по ID {0} отменено.", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при получении койки с отделением по ID: {0}", id);
                throw;
            }
        }

        private static bool IsForeignKeyViolation(DbUpdateException dbEx)
        {
            var msg = dbEx.InnerException?.Message ?? dbEx.Message;
            return msg.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("547", StringComparison.Ordinal);
        }
    }
}