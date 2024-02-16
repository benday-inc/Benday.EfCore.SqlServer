using System;
using System.Collections;
using System.Collections.Generic;

using Benday.Repositories;

using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.SqlServer
{
    /// <summary>
    /// Base class implementation of the repository pattern for an EF Core entity data type stored in SQL Server.
    /// This provides standardized access to the DbContext and implements IDispoable.
    /// </summary>
    /// <typeparam name="TEntity">Entity data type managed by this repository implementation. Must be an instance of IInt32Identity.</typeparam>
    /// <typeparam name="TDbContext">EF Core DbContext data type that manages this entity</typeparam>
    public abstract class SqlEntityFrameworkRepositoryBase<TEntity, TDbContext, TIdentity> :
        IDisposable where TEntity : class, IEntityBase<TIdentity>
        where TDbContext : DbContext
        where TIdentity : IComparable<TIdentity>
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

        /// <summary>
        /// The instance of EF Core DbContext used by the repository.
        /// </summary>
        protected TDbContext Context { get; }

        /// <summary>
        /// Verifies that an object is attached to the DbContext instance and attaches it if necessary.
        /// </summary>
        /// <param name="dbset">DbSet instance that should contain this entity</param>
        /// <param name="item">Entity instance to verify is attached</param>
        protected void VerifyItemIsAddedOrAttachedToDbSet(DbSet<TEntity> dbset, TEntity item)
        {
            if (item == null)
            {
                return;
            }
            else
            {
                if (Comparer<TIdentity>.Equals(item.Id, default(TIdentity)) == true)
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
