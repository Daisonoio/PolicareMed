using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore; // AGGIUNTO per IQueryable EF

namespace PoliCare.Core.Interfaces;

public interface IRepository<T> where T : class
{
    // Query methods
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize);

    // AGGIORNATO: Restituisce IQueryable per supportare Include e altri metodi EF
    IQueryable<T> Find(Expression<Func<T, bool>> expression);
    IQueryable<T> FindWithDeleted(Expression<Func<T, bool>> expression);

    // AGGIUNTO: Metodo per ottenere tutti gli elementi come IQueryable
    IQueryable<T> GetQueryable();

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