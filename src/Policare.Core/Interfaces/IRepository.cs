using System.Linq.Expressions;

namespace PoliCare.Core.Interfaces;

public interface IRepository<T> where T : class
{
    // Query methods - ASYNC DIRETTAMENTE NEL REPOSITORY
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize);

    // CAMBIATI: Metodi async diretti invece di IQueryable
    Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> GetWhereAsync(Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> GetWhereWithDeletedAsync(Expression<Func<T, bool>> predicate);

    Task<bool> ExistsAsync(Expression<Func<T, bool>> expression);
    Task<int> CountAsync(Expression<Func<T, bool>>? expression = null);

    // Command methods
    Task<T> AddAsync(T entity);
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> DeleteRangeAsync(Expression<Func<T, bool>> expression);
    Task<bool> RestoreAsync(Guid id);
    Task<bool> HardDeleteAsync(Guid id);
}