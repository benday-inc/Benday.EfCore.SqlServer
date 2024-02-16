using Benday.Common;

namespace Benday.Repositories
{
    /// <summary>
    /// Interface indicating that this repository provides search functionality for this entity type.
    /// </summary>
    /// <typeparam name="T">Entity data type managed by this repository</typeparam>
    public interface ISearchableRepository<T, TIdentity> : IRepository<T, TIdentity>
        where T : IIdentity<TIdentity>
        where TIdentity : IComparable<TIdentity>
    {
        /// <summary>
        /// Execute a search for this entity type and return the matching result.
        /// </summary>
        /// <param name="search">Search definition with arguments, search operation methods, and sorting information</param>
        /// <returns>The result of the search</returns>
        SearchResult<T> Search(Search search);
    }
}
