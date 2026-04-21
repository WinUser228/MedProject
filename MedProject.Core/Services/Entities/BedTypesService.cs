using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Exceptions;
using HospitalStatistics.Core.Repository.Interfaces;
using HospitalStatistics.Core.Services.Interfaces.Entities;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace HospitalStatistics.Core.Services.Entities
{
    public class BedTypesService : IBedTypesService
    {
        private readonly IBedTypesRepository _repository;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public BedTypesService(IBedTypesRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger.Info("BedTypesService инициализирован.");
        }

        public async Task<int> CreateBedTypeAsync(BedTypes bedType, CancellationToken ct)
        {
            if (bedType == null) throw new ArgumentNullException(nameof(bedType));
            if (string.IsNullOrWhiteSpace(bedType.BedTypeName))
                throw new ArgumentException("Название типа койки не может быть пустым.", nameof(bedType.BedTypeName));

            _logger.Info("Попытка создать новый тип койки: {0}", bedType.BedTypeName);
            try
            {
                // Проверяем на дубликат
                var existing = await _repository.GetBedTypeByNameAsync(bedType.BedTypeName, ct);
                if (existing != null)
                {
                    throw new ArgumentException($"Тип койки с названием '{bedType.BedTypeName}' уже существует.");
                }

                var newId = await _repository.CreateAsync(bedType, ct);
                _logger.Info("Тип койки успешно создан с ID: {0}", newId);
                return newId;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Создание типа койки отменено.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при создании типа койки.");
                throw;
            }
        }

        public async Task<int> UpdateBedTypeAsync(int id, BedTypes bedType, CancellationToken ct)
        {
            if (bedType == null) throw new ArgumentNullException(nameof(bedType));
            if (string.IsNullOrWhiteSpace(bedType.BedTypeName))
                throw new ArgumentException("Название типа койки не может быть пустым.", nameof(bedType.BedTypeName));

            _logger.Info("Попытка обновить тип койки с ID: {0}", id);

            var existing = await _repository.GetByIdAsync(id, ct);
            if (existing == null)
            {
                _logger.Warn("Тип койки с ID {0} не найден для обновления.", id);
                throw new ArgumentException($"Тип койки с ID {id} не найден.");
            }

            // Проверяем на дубликат (кроме текущего)
            var duplicate = await _repository.GetBedTypeByNameAsync(bedType.BedTypeName, ct);
            if (duplicate != null && duplicate.Id != id)
            {
                throw new ArgumentException($"Тип койки с названием '{bedType.BedTypeName}' уже существует.");
            }

            try
            {
                existing.BedTypeName = bedType.BedTypeName;
                await _repository.UpdateAsync(existing, ct);
                _logger.Info("Тип койки с ID {0} успешно обновлен.", id);
                return id;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Обновление типа койки с ID {0} отменено.", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при обновлении типа койки с ID: {0}", id);
                throw;
            }
        }

        public async Task<BedTypes?> GetBedTypeByIdAsync(int id, CancellationToken ct)
        {
            _logger.Info("Попытка получить тип койки с ID: {0}", id);
            try
            {
                var bedType = await _repository.GetByIdAsync(id, ct);
                if (bedType == null)
                {
                    _logger.Warn("Тип койки с ID {0} не найден.", id);
                    return null;
                }
                _logger.Info("Тип койки с ID {0} успешно получен.", id);
                return bedType;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Получение типа койки с ID {0} отменено.", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при получении типа койки с ID: {0}", id);
                throw;
            }
        }

        public async Task<List<BedTypes>> GetAllBedTypesAsync(CancellationToken ct)
        {
            _logger.Info("Попытка получить все типы коек.");
            try
            {
                var bedTypes = await _repository.GetAllBedTypesAsync(ct);
                _logger.Info("Получено {0} типов коек.", bedTypes.Count);
                return bedTypes;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Получение всех типов коек отменено.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при получении всех типов коек.");
                throw;
            }
        }

        public async Task<bool> DeleteBedTypeAsync(int id, CancellationToken ct)
        {
            _logger.Info("Попытка удалить тип койки с ID: {0}", id);
            var bedType = await _repository.GetByIdAsync(id, ct);
            if (bedType == null)
            {
                _logger.Warn("Тип койки с ID {0} не найден для удаления.", id);
                return false;
            }

            try
            {
                await _repository.DeleteAsync(bedType, ct);
                _logger.Info("Тип койки с ID {0} успешно удален.", id);
                return true;
            }
            catch (DbUpdateException dbEx)
            {
                if (IsForeignKeyViolation(dbEx))
                {
                    _logger.Warn(dbEx, "Попытка удалить тип койки с ID {0}, который имеет связанные записи.", id);
                    throw new RelatedRecordsExistException(
                        "Невозможно удалить этот тип койки, так как есть связанные записи.", dbEx.InnerException ?? dbEx);
                }
                _logger.Error(dbEx, "Ошибка обновления базы при удалении типа койки с ID: {0}", id);
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Удаление типа койки с ID {0} отменено.", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Неожиданная ошибка при удалении типа койки с ID: {0}", id);
                throw;
            }
        }

        public async Task<BedTypes?> GetBedTypeByNameAsync(string bedTypeName, CancellationToken ct)
        {
            _logger.Info("Попытка получить тип койки по названию: {0}", bedTypeName);
            try
            {
                var bedType = await _repository.GetBedTypeByNameAsync(bedTypeName, ct);
                if (bedType == null)
                {
                    _logger.Warn("Тип койки с названием '{0}' не найден.", bedTypeName);
                    return null;
                }
                _logger.Info("Тип койки с названием '{0}' успешно получен.", bedTypeName);
                return bedType;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Получение типа койки по названию '{0}' отменено.", bedTypeName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при получении типа койки по названию: {0}", bedTypeName);
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

