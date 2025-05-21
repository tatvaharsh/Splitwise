using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SplitWise.Domain;
using SplitWise.Domain.Data;
using SplitWise.Repository.Interface;

namespace SplitWise.Repository.Implementation;

public class BaseRepository<T>(ApplicationContext context) : IBaseRepository<T> where T : class
{
    private readonly ApplicationContext _context = context;
    private readonly DbSet<T> _dbSet = context.Set<T>();

    public async Task<T> AddAsync(T model)
    {
        await _dbSet.AddAsync(model);
        await _context.SaveChangesAsync();
        return model;
    }

    public async Task AddRangeAsync(IEnumerable<T> models)
    {
        await _dbSet.AddRangeAsync(models);
        await _context.SaveChangesAsync();
    }

    public async Task<T> UpdateAsync(T model)
    {
        _dbSet.Update(model);
        await _context.SaveChangesAsync();
        return model;
    }

    public async Task UpdateRangeAsync(IEnumerable<T> models)
    {
        _dbSet.UpdateRange(models);
        await _context.SaveChangesAsync();
    }

    public async Task<string> DeleteAsync(Guid id)
    {
        var idProp = typeof(T).GetProperty("Id");
        if (idProp == null) throw new Exception("Property 'Id' not found.");

        var entity = (await _dbSet.ToListAsync()).FirstOrDefault(e => idProp.GetValue(e)?.Equals(id) == true);
        var prop = typeof(T).GetProperty("Isdeleted");
        if (prop == null) throw new Exception("Entity does not contain IsDeleted property");

        prop.SetValue(entity, true);

        await _context.SaveChangesAsync();
        return SplitWiseConstants.RECORD_DELETED;
    }
    public async Task DeleteRangeAsync(IEnumerable<Guid> ids)
    {
        var entities = await _dbSet
            .Where(e => ids.Contains(EF.Property<Guid>(e, "Id")))
            .ToListAsync();

        if (entities.Count == 0)
            throw new NotFoundException();

        var isDeletedProp = typeof(T).GetProperty("Isdeleted");
        if (isDeletedProp == null)
            throw new Exception("Entity does not contain IsDeleted property");

        foreach (var entity in entities)
        {
            isDeletedProp.SetValue(entity, true);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<T?> GetByIdAsync(Guid id) => await _dbSet.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);

    // public IQueryable<T> GetAsync(Expression<Func<T, bool>> predicate) => _dbSet.Where(predicate).AsQueryable();
    public IQueryable<T> GetAsync(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IQueryable<T>>? include = null)
    {
        var isDeletedProp = typeof(T).GetProperty("Isdeleted");
        IQueryable<T> query = _dbSet;
        if (isDeletedProp != null)
        {
            query = query.Where(e => EF.Property<bool>(e, "Isdeleted") == false);
        }
        if (include != null)
        {
            query = include(query);
        }

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return query;
    }
    public IQueryable<T> GetAsync(Expression<Func<T, bool>>? predicate, params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet;
        query = includes.Aggregate(query, (current, include) => current.Include(include));

        var isDeletedProp = typeof(T).GetProperty("Isdeleted");
        if (isDeletedProp != null)
        {
            query = query.Where(e => EF.Property<bool>(e, "Isdeleted") == false);
        }
        query = includes.Aggregate(query, (current, include) =>
        {
            return current.Include(include);
        });
        query = predicate != null ? query.Where(predicate) : _dbSet;    
        return query;
    }
}

