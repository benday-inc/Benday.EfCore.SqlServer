using System;

using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.SqlServer
{
    public abstract class SqlEntityFrameworkRepositoryBase<TEntity, TDbContext> :
        IDisposable where TEntity : class, IEntityBase
        where TDbContext : DbContext
    {
        protected SqlEntityFrameworkRepositoryBase(
            TDbContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context), "context is null.");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _isDisposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                // free managed resources
                ((IDisposable)Context).Dispose();
            }

            _isDisposed = true;
        }

        protected TDbContext Context { get; }

        protected void VerifyItemIsAddedOrAttachedToDbSet(DbSet<TEntity> dbset, TEntity item)
        {
            if (item == null)
            {
                return;
            }
            else
            {
                if (item.Id == 0)
                {
                    dbset.Add(item);
                }
                else
                {
                    var entry = Context.Entry<TEntity>(item);

                    if (entry.State == EntityState.Detached)
                    {
                        dbset.Attach(item);
                    }

                    entry.State = EntityState.Modified;
                }
            }
        }
    }
}
