using System.Linq.Expressions;
using SplitWise.Domain.Generic.Entity;

namespace SplitWise.Service.Interface;

public interface IBaseService<T> where T : class
{
    Task<T> AddAsync(T model);
    Task<T> UpdateAsync(T model);
    Task<string> DeleteAsync(Guid id);
    Task AddRangeAsync(List<T> models);
    Task UpdateRangeAsync(List<T> models);
    Task DeleteRangeAsync(List<Guid> ids);
    Task<T> GetByIdAsync(Guid id);
    Task<T?> GetOneAsync(Expression<Func<T, bool>> predicate);
    Task<List<T>> GetListAsync(Expression<Func<T, bool>> predicate);
    // Task<T?> GetOneAsync(Expression<Func<T, bool>>? predicate, params Expression<Func<T, object>>[] includes);

    Task<T?> GetOneAsync(Expression<Func<T, bool>>? predicate,Func<IQueryable<T>, IQueryable<T>>? include = null);
    // Task<List<T>> GetListAsync(Expression<Func<T, bool>>? predicate, params Expression<Func<T, object>>[] includes);
    Task<List<T>> GetListAsync(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IQueryable<T>>? include = null);
}
