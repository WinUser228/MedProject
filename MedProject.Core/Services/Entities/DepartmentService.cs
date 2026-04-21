using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Exceptions;
using HospitalStatistics.Core.Repository.Interfaces;
using HospitalStatistics.Core.Services.Interfaces.Entities;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace HospitalStatistics.Core.Services.Entities
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IDepartmentRepository _repository;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public DepartmentService(IDepartmentRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger.Info("DepartmentService инициализирован.");
        }

        public async Task<List<Departments>> GetAllDepartmentAsync(CancellationToken ct)
        {
            _logger.Info("Получение всех отделений...");
            try
            {
                var departments = await _repository.GetAllAsync(ct);
                _logger.Info("Найдено {0} отделений", departments.Count);
                return departments;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Получение всех отделений отменено");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при получении всех отделений");
                throw;
            }
        }

        public async Task<Departments?> GetDepartmentByIdAsync(int id, CancellationToken ct)
        {
            _logger.Info("Получение отделения с ID={0}", id);
            try
            {
                var department = await _repository.GetByIdAsync(id, ct);
                if (department == null)
                {
                    _logger.Warn("Отделение с ID={0} не найдено", id);
                }
                else
                {
                    _logger.Info("Отделение с ID={0} успешно получено", id);
                }
                return department;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Получение отделения ID={0} отменено", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при получении отделения ID={0}", id);
                throw;
            }
        }

        public async Task<int> CreateDepartmentAsync(Departments department, CancellationToken ct)
        {
            if (department == null) throw new ArgumentNullException(nameof(department));

            _logger.Info("Создание нового отделения: {0}", department.DepartmentName);
            try
            {
                var id = await _repository.CreateAsync(department, ct);
                _logger.Info("Отделение создано с ID={0}", id);
                return id;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Создание отделения отменено");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при создании отделения {0}", department.DepartmentName);
                throw;
            }
        }

        public async Task<int> UpdateDepartmentAsync(int id, Departments department, CancellationToken ct)
        {
            if (department == null) throw new ArgumentNullException(nameof(department));

            _logger.Info("Обновление отделения ID={0}", id);
            var existing = await _repository.GetByIdAsync(id, ct);
            if (existing == null)
            {
                _logger.Warn("Отделение ID={0} не найдено для обновления", id);
                throw new ArgumentException($"Отделение с ID={id} не найдено");
            }

            existing.DepartmentName = department.DepartmentName;
            existing.Beds = department.Beds;

            try
            {
                await _repository.UpdateAsync(existing, ct);
                _logger.Info("Отделение ID={0} успешно обновлено", id);
                return id;
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Обновление отделения ID={0} отменено", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при обновлении отделения ID={0}", id);
                throw;
            }
        }

        public async Task<bool> DeleteDepartmentAsync(int id, CancellationToken ct)
        {
            _logger.Info("Удаление отделения ID={0}", id);
            var department = await _repository.GetByIdAsync(id, ct);
            if (department == null)
            {
                _logger.Warn("Отделение ID={0} не найдено для удаления", id);
                return false;
            }

            try
            {
                await _repository.DeleteAsync(department, ct);
                _logger.Info("Отделение ID={0} успешно удалено", id);
                return true;
            }
            catch (DbUpdateException dbEx) when (IsForeignKeyViolation(dbEx))
            {
                _logger.Warn(dbEx, "Попытка удалить отделение ID={0}, которое имеет связанные записи.", id);
                throw new RelatedRecordsExistException(
                    "Невозможно удалить отделение, так как к нему привязаны записи о койках.", dbEx.InnerException ?? dbEx);
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Удаление отделения ID={0} отменено", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при удалении отделения ID={0}", id);
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
