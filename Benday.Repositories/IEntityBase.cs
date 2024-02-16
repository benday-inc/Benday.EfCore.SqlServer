using System.Collections.Generic;

using Benday.Common;

namespace Benday.Repositories
{
    /// <summary>
    /// Interface for entities
    /// </summary>
    public interface IEntityBase<TIdentity> : IDeleteable<TIdentity> where TIdentity : IComparable<TIdentity>
    {
        
    }
}
