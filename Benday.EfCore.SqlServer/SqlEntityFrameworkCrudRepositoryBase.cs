using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.SqlServer
{
    /// <summary>
    /// Implementation of the repository pattern for an EF Core entity data type stored in SQL Server.
    /// Provides create, read, update, and delete logic.
    /// </summary>
    /// <typeparam name="TEntity">Entity data type managed by this repository implementation. Must be an instance of IInt32Identity.</typeparam>
    /// <typeparam name="TDbContext">EF Core DbContext data type that manages this entity</typeparam>
    public abstract class SqlEntityFrameworkCrudRepositoryBase<TEntity, TDbContext> :
        SqlEntityFrameworkRepositoryBase<TEntity, TDbContext>, IRepository<TEntity>
        where TEntity : class, IEntityBase
        where TDbContext : DbContext
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">Instance of the EF Core DbContext to use for data operations</param>
        public SqlEntityFrameworkCrudRepositoryBase(
            TDbContext context) : base(context)
        {
        }

        /// <summary>
        /// References to the EF Core DbSet for data operation for this entity
        /// </summary>
        protected abstract DbSet<TEntity> EntityDbSet
        {
            get;
        }

        /// <summary>
        /// Delete an entity from the DbContext. This method checks if the entity 
        /// has been detached from the DbContext instance and reattaches it if necessary 
        /// before triggering a save on the DbContext.
        /// </summary>
        /// <param name="deleteThis">Instance to delete</param>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual void Delete(TEntity deleteThis)
        {
            if (deleteThis == null)
                throw new ArgumentNullException(nameof(deleteThis), "deleteThis is null.");

            var entry = Context.Entry(deleteThis);

            if (entry.State == EntityState.Detached)
            {
                EntityDbSet.Attach(deleteThis);
            }

            EntityDbSet.Remove(deleteThis);

            BeforeDelete(deleteThis);

            Context.SaveChanges();

            AfterDelete(deleteThis);
        }

        /// <summary>
        /// Template method for adding logic before issuing the delete
        /// </summary>
        /// <param name="deleteThis">Entity that is being deleted</param>
        protected virtual void BeforeDelete(TEntity deleteThis)
        {
        }

        /// <summary>
        /// Template method for adding logic after issuing the delete
        /// </summary>
        /// <param name="deleteThis">Entity that is being deleted</param>
        protected virtual void AfterDelete(TEntity deleteThis)
        {
        }

        /// <summary>
        /// Names of entity properties that should be included during SELECT operations
        /// </summary>
        protected virtual List<string> Includes
        {
            get;
        } = new List<string>();

        /// <summary>
        /// Get all entities
        /// </summary>
        /// <returns></returns>
        public virtual IList<TEntity> GetAll()
        {
            return GetAll(-1, true);
        }

        /// <summary>
        /// Template method for adding logic before issuing the GetAll command.
        /// This method allows code to modify the query or replace the query before execution.
        /// </summary>
        /// <param name="query">Proposed query for the entity</param>
        /// <returns>Original or modified query to use for the GetAll command</returns>
        protected virtual IQueryable<TEntity> BeforeGetAll(IQueryable<TEntity> query)
        {
            return query;
        }

        /// <summary>
        /// Template method to add sorting logic to SELECT queries. This method allows code to modify the query or replace the query before execution.
        /// </summary>
        /// <param name="query">Proposed query for the entity</param>
        /// <returns>Original or modified query to use for the query</returns>
        protected virtual IQueryable<TEntity> AddDefaultSort(IQueryable<TEntity> query)
        {
            return query;
        }


        /// <summary>
        /// Get all entities with options for limiting the number of returned records and
        /// skipping population of dependency relationships. 
        /// </summary>
        /// <param name="maxNumberOfRows">Maximum number of records to return</param>
        /// <param name="noIncludes">If true, skip populating of dependent entity relationships. This can be helpful for optimizing performance.</param>
        /// <returns></returns>
        public virtual IList<TEntity> GetAll(int maxNumberOfRows, bool noIncludes)
        {
            var queryable = EntityDbSet.AsQueryable();

            if (noIncludes == false)
            {
                queryable = AddIncludes(queryable);
            }

            queryable = BeforeGetAll(queryable);

            queryable = AddDefaultSort(queryable);

            if (maxNumberOfRows == -1)
            {
                return queryable.ToList();
            }
            else
            {
                return queryable.Take(maxNumberOfRows).ToList();
            }
        }

        /// <summary>
        /// Populate eager loading includes for dependent objects
        /// </summary>
        /// <param name="queryable">Proposed query for the entity</param>
        /// <returns>Original or modified query to use for the query</returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected virtual IQueryable<TEntity> AddIncludes(IQueryable<TEntity> queryable)
        {
            if (queryable == null)
            {
                throw new ArgumentNullException(nameof(queryable));
            }

            if (Includes == null || Includes.Count == 0)
            {
                return queryable;
            }
            else
            {
                foreach (var item in Includes)
                {
                    queryable = queryable.Include(item);
                }

                return queryable;
            }
        }

        /// <summary>
        /// Retrieves a single matching entity with eager loading includes
        /// </summary>
        /// <param name="id">Entity Id to retrieve</param>
        /// <returns>The matching entity or null if not found</returns>
        public virtual TEntity? GetById(int id)
        {
            var query = from temp in EntityDbSet
                        where temp.Id == id
                        select temp;

            query = AddIncludes(query);

            query = BeforeGetById(query, id);

            return query.FirstOrDefault();
        }

        /// <summary>
        /// Template method for adding logic before issuing the GetById command.
        /// This method allows code to modify the query or replace the query before execution.
        /// </summary>
        /// <param name="query">Proposed query for the entity</param>
        /// <param name="id">Entity Id to retrieve</param>
        /// <returns>Original or modified query to use for the GetById command</returns>
        [SuppressMessage("csharp", "IDE0060")]
        private IQueryable<TEntity> BeforeGetById(IQueryable<TEntity> query, int id)
        {
            return query;
        }

        /// <summary>
        /// Save an entity and its dependent children. 
        /// This method verifies that the entities are attached to the DbContext. 
        /// Dependent children are accessed via the IDependentEntityCollection interface. 
        /// Template methods are available for customizing the logic before and after this save operation.
        /// </summary>
        /// <param name="saveThis">Entity to save</param>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual void Save(TEntity saveThis)
        {
            if (saveThis == null)
                throw new ArgumentNullException(nameof(saveThis), "saveThis is null.");

            VerifyItemIsAddedOrAttachedToDbSet(
                EntityDbSet, saveThis);

            BeforeSave(saveThis);

            BeforeSaveOnDependentEntities(saveThis);

            Context.SaveChanges();

            AfterSaveOnDependentEntities(saveThis);
            AfterSave(saveThis);
        }

        private void AfterSaveOnDependentEntities(TEntity saveThis)
        {
            var dependentEntityCollections = saveThis.GetDependentEntities();

            if (dependentEntityCollections == null ||
                dependentEntityCollections.Count == 0)
            {
                return;
            }
            else
            {
                foreach (var item in dependentEntityCollections)
                {
                    item.AfterSave();
                }
            }
        }

        /// <summary>
        /// Template method for performing dependent entity logic on the entity before saving.
        /// The default implementation calls GetDependentEntities() and calls BeforeSave() on 
        /// any of those entities.
        /// </summary>
        /// <param name="saveThis"></param>
        protected virtual void BeforeSaveOnDependentEntities(
            TEntity saveThis)
        {
            var dependentEntityCollections = saveThis.GetDependentEntities();

            if (dependentEntityCollections == null ||
                dependentEntityCollections.Count == 0)
            {
                return;
            }
            else
            {
                foreach (var item in dependentEntityCollections)
                {
                    item.BeforeSave(Context);
                }
            }
        }

        /// <summary>
        /// Template method for adding logic before saving. This implementation does nothing.
        /// </summary>
        /// <param name="saveThis">Entity to save</param>
        protected virtual void BeforeSave(TEntity saveThis)
        {
        }

        /// <summary>
        /// Template method for adding logic after saving. This implementation does nothing.
        /// </summary>
        /// <param name="saveThis">Entity to save</param>
        protected virtual void AfterSave(TEntity saveThis)
        {
        }
    }
}
