using System.Linq.Expressions;
using SplitWise.Domain.Generic.Entity;

namespace SplitWise.Repository.Interface;

public interface IBaseRepository<T> where T : class
{
    Task<T> AddAsync(T model);
    Task<T> UpdateAsync(T model);
    Task<string> DeleteAsync(Guid id);
    Task AddRangeAsync(IEnumerable<T> models);
    Task UpdateRangeAsync(IEnumerable<T> models);
    Task DeleteRangeAsync(IEnumerable<Guid> ids);
    Task<T?> GetByIdAsync(Guid id);
    // IQueryable<T> GetAsync(Expression<Func<T, bool>> predicate);
    IQueryable<T> GetAsync(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IQueryable<T>>? include = null);

    IQueryable<T> GetAsync(Expression<Func<T, bool>>? predicate, params Expression<Func<T, object>>[] includes);
}