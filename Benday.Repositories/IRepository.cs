using System.Collections.Generic;
using System.Security.Principal;

using Benday.Common;

namespace Benday.Repositories
{
    /// <summary>
    /// Interface for basic repository operations
    /// </summary>
    /// <typeparam name="T">Entity data type managed by this repository implementation. Must be an instance of IInt32Identity.</typeparam>
    public interface IRepository<T, TIdentity> 
        where T : IIdentity<TIdentity> 
        where TIdentity : IComparable<TIdentity>
    {
        /// <summary>
        /// Get all records
        /// </summary>
        /// <returns></returns>
        IList<T> GetAll();
        
        /// <summary>
        /// Get all records with a maximum number of results. Optionally, request that 
        /// dependent entity references and collections are not populated.        /// 
        /// </summary>
        /// <param name="maxNumberOfRows">Maximum number of records to return</param>
        /// <param name="noIncludes">If true, do not include or populate dependent entities</param>
        /// <returns></returns>
        IList<T> GetAll(int maxNumberOfRows, bool noIncludes = true);
        
        /// <summary>
        /// Get a single entity by id value
        /// </summary>
        /// <param name="id">Identity value</param>
        /// <returns>The matching instance of the entity or null if not found.</returns>
        T? GetById(TIdentity id);
        
        /// <summary>
        /// Save or update the entity and any children.
        /// </summary>
        /// <param name="saveThis">Entity to save</param>
        void Save(T saveThis);
        
        /// <summary>
        /// Delete this entity
        /// </summary>
        /// <param name="deleteThis">Entity to delete</param>
        void Delete(T deleteThis);
    }
}
