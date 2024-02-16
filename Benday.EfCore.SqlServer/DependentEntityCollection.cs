using System;
using System.Collections.Generic;
using System.Linq;

using Benday.Repositories;

using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.SqlServer
{
    /// <summary>
    /// Collection of dependent child entities. Primarily used for triggering logic
    /// before save and after save events.
    /// </summary>
    /// <typeparam name="T">Data type for the entities</typeparam>
    public class DependentEntityCollection<T, TIdentity> :
        IDependentEntityCollection 
        where T : class, IEntityBase<TIdentity>
        where TIdentity : IComparable<TIdentity>
    {
        private readonly IList<T> _entities;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="entities">Dependent entity instances</param>
        /// <exception cref="ArgumentNullException"></exception>
        public DependentEntityCollection(IList<T> entities)
        {
            _entities = entities ?? throw new ArgumentNullException(nameof(entities), "Argument cannot be null.");
        }

        /// <summary>
        /// Logic to run before a save operation. By default this method looks for any wrapped dependent 
        /// entities that are marked for delete and issues a Remove() call on the data context.
        /// </summary>
        /// <param name="dbContext">EF Core DbContext for the save operation</param>
        public void BeforeSave(object? context)
        {
            if (context == null)
            {
                return;
            }
            else
            {
                if (context is DbContext)
                {
                    var dbContext = (DbContext)context;

                    foreach (var entity in _entities)
                    {
                        if (entity.IsMarkedForDelete == true)
                        {
                            RemoveFromDbSet(dbContext, entity);
                        }
                    }
                }                
            }
        }

        private void RemoveFromDbSet(DbContext dbContext, T entity)
        {
            dbContext.Remove<T>(entity);
        }

        /// <summary>
        /// Logic to run after a save operation has completed successfully.
        /// By default, this looks for entities that were marked for deletion and removes them from the 
        /// in-memory list of entities.
        /// </summary>
        public void AfterSave()
        {
            var deleteThese = _entities.Where(x => x.IsMarkedForDelete == true).ToList();

            deleteThese.ForEach(x => _entities.Remove(x));
        }
    }
}
