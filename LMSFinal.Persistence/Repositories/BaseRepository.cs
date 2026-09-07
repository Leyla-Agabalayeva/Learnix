using LMSFinal.Domain.Common;
using LMSFinal.Domain.Interfaces;
using LMSFinal.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Persistence.Repositories
{
    public class BaseRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseEntity
    {
        protected readonly AppDbContext Context;
        protected readonly DbSet<TEntity> DbSet;

        public BaseRepository(AppDbContext context)
        {
            Context = context;
            DbSet = context.Set<TEntity>();
        }

        public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            await DbSet.FindAsync(new object[] { id }, cancellationToken);

        public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await DbSet.AsNoTracking().ToListAsync(cancellationToken);

        public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
            await DbSet.AddAsync(entity, cancellationToken);

        public virtual void Update(TEntity entity) => DbSet.Update(entity);

        public virtual void Remove(TEntity entity) => DbSet.Remove(entity);
    }

}
