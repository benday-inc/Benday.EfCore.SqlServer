using System.Collections.Generic;

using Benday.Common;

namespace Benday.EfCore.SqlServer
{
    /// <summary>
    /// Interface for entities
    /// </summary>
    public interface IEntityBase : IInt32Identity, IDeleteable
    {
        /// <summary>
        /// Gets collection of dependent (aka. 'child') entities
        /// </summary>
        /// <returns></returns>
        IList<IDependentEntityCollection> GetDependentEntities();
    }
}
