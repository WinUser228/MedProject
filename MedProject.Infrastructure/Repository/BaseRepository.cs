using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Repository.Interfaces;
using HospitalStatistics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NLog;


namespace HospitalStatistics.Infrastructure.Repository
{
    public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : BaseEntity
    {
        #region Поля

        /// <summary>
        /// Фабрика контекста базы данных.
        /// </summary>
        protected readonly IDbContextFactory<HospitalContext> _contextFactory;
        protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Конструктор

        /// <summary>
        /// Инициализирует экземпляр <see cref="BaseRepository{TEntity}"/>.
        /// </summary>
        /// <param name="contextFactory">Фабрика контекста базы данных.</param>
        public BaseRepository(IDbContextFactory<HospitalContext> contextFactory)
        {
            _contextFactory = contextFactory;
            Logger.Debug("Создан экземпляр BaseRepository<{0}>", typeof(TEntity).Name);
        }

        #endregion

        /// <summary>
        /// Получает все сущности.
        /// </summary>
        public virtual async Task<List<TEntity>> GetAllAsync(CancellationToken ct)
        {
            Logger.Debug("Вызван GetAllAsync<{0}>", typeof(TEntity).Name);
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var result = await context.Set<TEntity>().ToListAsync(ct);
                Logger.Info("Получено {0} сущностей типа {1}", result.Count, typeof(TEntity).Name);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка при получении всех сущностей типа {0}", typeof(TEntity).Name);
                throw;
            }
        }

        /// <summary>
        /// Получает сущность по идентификатору.
        /// </summary>
        public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken ct)
        {
            Logger.Debug("Вызван GetByIdAsync<{0}> для id={1}", typeof(TEntity).Name, id);
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var entity = await context.Set<TEntity>().FindAsync(new object?[] { id }, ct);
                if (entity == null)
                    Logger.Warn("Сущность {0} с id={1} не найдена.", typeof(TEntity).Name, id);
                else
                    Logger.Info("Получена сущность {0} с id={1}", typeof(TEntity).Name, id);
                return entity;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка при получении {0} по id={1}", typeof(TEntity).Name, id);
                throw;
            }
        }

        /// <summary>
        /// Добавляет новую сущность.
        /// </summary>
        public virtual async Task<int> CreateAsync(TEntity entity, CancellationToken ct)
        {
            Logger.Debug("Вызван CreateAsync<{0}>", typeof(TEntity).Name);
            try
            {
                using var context = _contextFactory.CreateDbContext();
                await context.Set<TEntity>().AddAsync(entity, ct);
                await context.SaveChangesAsync(ct);
                Logger.Info("Создана новая сущность {0} с id={1}", typeof(TEntity).Name, entity.Id);
                return entity.Id;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка при создании сущности {0}", typeof(TEntity).Name);
                throw;
            }
        }

        /// <summary>
        /// Обновляет существующую сущность.
        /// </summary>
        public virtual async Task UpdateAsync(TEntity entity, CancellationToken ct)
        {
            Logger.Debug("Вызван UpdateAsync<{0}> для id={1}", typeof(TEntity).Name, entity.Id);
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var keyValues = context.Entry(entity).Metadata.FindPrimaryKey()
                    .Properties.Select(p => p.PropertyInfo.GetValue(entity)).ToArray();

                var trackedEntity = await context.Set<TEntity>().FindAsync(keyValues, ct);

                if (trackedEntity == null)
                {
                    Logger.Warn("Обновляемая сущность {0} c id={1} не найдена в контексте, будет добавлена через Attach.", typeof(TEntity).Name, entity.Id);
                    context.Set<TEntity>().Attach(entity);
                    context.Entry(entity).State = EntityState.Modified;
                }
                else
                {
                    context.Entry(trackedEntity).CurrentValues.SetValues(entity);
                }

                await context.SaveChangesAsync(ct);
                Logger.Info("Обновлена сущность {0} с id={1}", typeof(TEntity).Name, entity.Id);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка при обновлении {0} с id={1}", typeof(TEntity).Name, entity.Id);
                throw;
            }
        }

        /// <summary>
        /// Удаляет сущность из базы данных по идентификатору.
        /// </summary>
        public virtual async Task DeleteByIdAsync(int id, CancellationToken ct)
        {
            Logger.Debug("Вызван DeleteByIdAsync<{0}> для id={1}", typeof(TEntity).Name, id);
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var entity = await context.Set<TEntity>().FindAsync(new object?[] { id }, ct);
                if (entity == null)
                {
                    Logger.Warn("Удаляемая сущность {0} с id={1} не найдена.", typeof(TEntity).Name, id);
                    throw new ArgumentException($"Entity with id {id} not found.");
                }
                context.Set<TEntity>().Remove(entity);
                await context.SaveChangesAsync(ct);
                Logger.Info("Удалена сущность {0} с id={1}", typeof(TEntity).Name, id);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка при удалении {0} с id={1}", typeof(TEntity).Name, id);
                throw;
            }
        }

        /// <summary>
        /// Удаляет сущность из базы данных.
        /// </summary>
        public virtual async Task DeleteAsync(TEntity entity, CancellationToken ct)
        {
            Logger.Debug("Вызван DeleteAsync<{0}> для id={1}", typeof(TEntity).Name, entity.Id);
            try
            {
                using var context = _contextFactory.CreateDbContext();
                context.Set<TEntity>().Remove(entity);
                await context.SaveChangesAsync(ct);
                Logger.Info("Удалена сущность {0} с id={1}", typeof(TEntity).Name, entity.Id);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка при удалении {0} с id={1}", typeof(TEntity).Name, entity.Id);
                throw;
            }
        }
    }
}