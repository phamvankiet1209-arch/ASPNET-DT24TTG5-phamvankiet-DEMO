using System.Linq.Expressions;
using HotelBookingManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingManagementSystem.Repositories
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> Query();
        Task<T?> GetByIdAsync(object id);
        Task<List<T>> GetAllAsync();
        Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
        Task<int> SaveChangesAsync();
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    }

    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _set;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _set = context.Set<T>();
        }

        public IQueryable<T> Query() => _set.AsQueryable();

        public async Task<T?> GetByIdAsync(object id) => await _set.FindAsync(id);

        public async Task<List<T>> GetAllAsync() => await _set.ToListAsync();

        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
            await _set.Where(predicate).ToListAsync();

        public async Task AddAsync(T entity) => await _set.AddAsync(entity);

        public void Update(T entity) => _set.Update(entity);

        public void Remove(T entity) => _set.Remove(entity);

        public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null) =>
            predicate == null ? await _set.CountAsync() : await _set.CountAsync(predicate);
    }
}
