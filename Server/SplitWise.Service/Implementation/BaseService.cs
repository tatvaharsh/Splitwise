using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SplitWise.Domain.Generic.Entity;
using SplitWise.Repository.Interface;
using SplitWise.Service.Interface;

namespace SplitWise.Service.Implementation;

public class BaseService<T>(IBaseRepository<T> repository) : IBaseService<T> where T : class
{
    private readonly IBaseRepository<T> _repository = repository;

    public async Task<T> AddAsync(T model) => await _repository.AddAsync(model);

    public async Task<T> UpdateAsync(T model) => await _repository.UpdateAsync(model);

    public async Task<string> DeleteAsync(Guid id) => await _repository.DeleteAsync(id);

    public async Task AddRangeAsync(List<T> models) => await _repository.AddRangeAsync(models);

    public async Task UpdateRangeAsync(List<T> models) => await _repository.UpdateRangeAsync(models);

    public Task DeleteRangeAsync(List<Guid> ids) => _repository.DeleteRangeAsync(ids);

    public async Task<T?> GetOneAsync(Expression<Func<T, bool>> predicate) => await _repository.GetAsync(predicate).FirstOrDefaultAsync();

    public async Task<List<T>> GetListAsync(Expression<Func<T, bool>> predicate) => await _repository.GetAsync(predicate).ToListAsync();

    // public async Task<T?> GetOneAsync(Expression<Func<T, bool>>? predicate, params Expression<Func<T, object>>[] includes)
    //     => await _repository.GetAsync(predicate, includes).FirstOrDefaultAsync();

    public async Task<T?> GetOneAsync(
    Expression<Func<T, bool>>? predicate,
    Func<IQueryable<T>, IQueryable<T>>? include = null)
    {
        return await _repository.GetAsync(predicate, include).FirstOrDefaultAsync();
    }

    public async Task<List<T>> GetListAsync(Expression<Func<T, bool>>? predicate, params Expression<Func<T, object>>[] includes)
        => await _repository.GetAsync(predicate, includes).ToListAsync();

    public async Task<T> GetByIdAsync(Guid id)
        => await _repository.GetByIdAsync(id)
        ?? throw new NotFoundException();
}
