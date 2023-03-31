using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Benday.Common;

using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.SqlServer
{
    /// <summary>
    /// Base class implementation of the repository pattern for an EF Core entity data type stored in SQL Server that 
    /// supports searching using Benday.Common.Search functionality.
    /// </summary>
    /// <typeparam name="TEntity">Entity data type managed by this repository implementation. Must be an instance of IInt32Identity.</typeparam>
    /// <typeparam name="TDbContext">EF Core DbContext data type that manages this entity</typeparam>
    public abstract class SqlEntityFrameworkSearchableRepositoryBase<TEntity, TDbContext> :
        SqlEntityFrameworkCrudRepositoryBase<TEntity, TDbContext>, ISearchableRepository<TEntity>
        where TEntity : class, IEntityBase
        where TDbContext : DbContext
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">EF Core DbContext to wrap</param>
        public SqlEntityFrameworkSearchableRepositoryBase(
            TDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Execute a search for this entity type and return the matching result.
        /// </summary>
        /// <param name="search">Search definition with arguments, search operation methods, and sorting information</param>
        /// <returns>The result of the search</returns>
        public virtual SearchResult<TEntity> Search(Search search)
        {
            var returnValue = new SearchResult<TEntity>
            {
                SearchRequest = search
            };

            if (search == null)
            {
                returnValue.Results = new List<TEntity>();
            }
            else
            {
                var whereClausePredicate = GetWhereClause(search);

                IQueryable<TEntity> query;
                if (whereClausePredicate == null)
                {
                    query = EntityDbSet.AsQueryable();
                }
                else
                {
                    query = EntityDbSet.Where(whereClausePredicate);
                }

                query = AddIncludes(query);

                query = AddSorts(search, query);

                query = BeforeSearch(query, search);

                if (search.MaxNumberOfResults == -1)
                {
                    returnValue.Results = query.ToList();
                }
                else
                {
                    returnValue.Results = query.Take(search.MaxNumberOfResults).ToList();
                }
            }

            return returnValue;
        }

        /// <summary>
        /// Takes the available query instance and makes sure that it's IOrderedQueryable
        /// </summary>
        /// <param name="query">The original query</param>
        /// <returns>The original instance or a new wrapped instance of that query as IOrderedQueryable</returns>
        protected virtual IOrderedQueryable<TEntity> EnsureIsOrderedQueryable(IQueryable<TEntity> query)
        {
            if (query is IOrderedQueryable<TEntity> queryable)
            {
                return queryable;
            }
            else
            {
                return query.OrderBy(x => 0);
            }
        }

        /// <summary>
        /// Adds a sort directive to the query
        /// </summary>
        /// <param name="query">Query instance</param>
        /// <param name="sort">Sort information</param>
        /// <param name="isFirstSort">Is this the first sort call to be added?</param>
        /// <returns></returns>
        protected virtual IOrderedQueryable<TEntity> AddSort(IOrderedQueryable<TEntity> query, SortBy sort, bool isFirstSort)
        {
            if (sort.Direction == SearchConstants.SortDirectionAscending)
            {
                return AddSortAscending(query, sort.PropertyName, isFirstSort);
            }
            else
            {
                return AddSortDescending(query, sort.PropertyName, isFirstSort);
            }
        }

        /// <summary>
        /// Abstract method for adding a descending sort to a repository search query
        /// </summary>
        /// <param name="query">The query instance</param>
        /// <param name="propertyName">Property name to add</param>
        /// <param name="isFirstSort">Is this the first sort call to be added?</param>
        /// <returns>The updated query</returns>
        protected abstract IOrderedQueryable<TEntity> AddSortDescending(IOrderedQueryable<TEntity> query, string propertyName, bool isFirstSort);

        /// <summary>
        /// Abstract method for adding a ascending sort to a repository search query
        /// </summary>
        /// <param name="query">The query instance</param>
        /// <param name="propertyName">Property name to add</param>
        /// <param name="isFirstSort">Is this the first sort call to be added?</param>
        /// <returns>The updated query</returns>
        protected abstract IOrderedQueryable<TEntity> AddSortAscending(IOrderedQueryable<TEntity> query, string propertyName, bool isFirstSort);

        /// <summary>
        /// Add multiple sorts to a repository search query
        /// </summary>
        /// <param name="search">The search request</param>
        /// <param name="query">The query instance to update</param>
        /// <returns>The updated query</returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected virtual IQueryable<TEntity> AddSorts(Search search, IQueryable<TEntity> query)
        {
            if (search is null)
            {
                throw new ArgumentNullException(nameof(search));
            }

            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (search.Sorts == null || search.Sorts.Count == 0)
            {
                return query;
            }
            else if (search.Sorts.Count == 1)
            {
                if (string.IsNullOrWhiteSpace(search.Sorts[0].PropertyName) == false)
                {
                    var returnValue = AddSort(EnsureIsOrderedQueryable(query), search.Sorts[0], true);

                    return returnValue;
                }
                else
                {
                    return query;
                }
            }
            else
            {
                var isFirst = true;

                foreach (var item in search.Sorts)
                {
                    if (string.IsNullOrWhiteSpace(item.PropertyName) == false)
                    {
                        query = AddSort(EnsureIsOrderedQueryable(query), item, isFirst);

                        isFirst = false;
                    }
                }

                return query;
            }
        }

        private Expression<Func<TEntity, bool>>? GetWhereClause(Search search)
        {
            Expression<Func<TEntity, bool>>? whereClausePredicate = null;
            Expression<Func<TEntity, bool>>? predicate = null;

            foreach (var arg in search.Arguments)
            {
                if (arg.Method == SearchMethod.Contains)
                {
                    predicate = GetPredicateForContains(arg);
                }
                else if (arg.Method == SearchMethod.StartsWith)
                {
                    predicate = GetPredicateForStartsWith(arg);
                }
                else if (arg.Method == SearchMethod.EndsWith)
                {
                    predicate = GetPredicateForEndsWith(arg);
                }
                else if (arg.Method == SearchMethod.Equals)
                {
                    predicate = GetPredicateForEquals(arg);
                }
                else if (arg.Method == SearchMethod.IsNotEqual)
                {
                    predicate = GetPredicateForIsNotEqualTo(arg);
                }
                else if (arg.Method == SearchMethod.DoesNotContain)
                {
                    predicate = GetPredicateForDoesNotContain(arg);
                }

                if (predicate == null)
                {
                    // if predicate is null, the implementer chose to ignore this
                    // search argument and returned null as an indication to skip
                    continue;
                }
                else if (whereClausePredicate == null)
                {
                    whereClausePredicate = predicate;
                }
                else if (arg.Operator == SearchOperator.Or)
                {
                    whereClausePredicate = whereClausePredicate.Or(predicate);
                }
                else if (arg.Operator == SearchOperator.And)
                {
                    whereClausePredicate = whereClausePredicate.And(predicate);
                }
                else
                {
                    throw new InvalidOperationException(
                        string.Format("Search operator '{0}' is not supported.", arg.Operator));
                }
            }

            return whereClausePredicate;
        }

        /// <summary>
        /// Template method that allows custom logic before a search is executed
        /// </summary>
        /// <param name="query"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        protected virtual IQueryable<TEntity> BeforeSearch(IQueryable<TEntity> query, Search search)
        {
            return query;
        }


        /// <summary>
        /// Abstract method to get an EF Core query predicate for processing a 'does not contain' operation
        /// </summary>
        /// <param name="arg">Search argument definition</param>
        /// <returns>The EF Core query predicate</returns>
        protected abstract Expression<Func<TEntity, bool>> GetPredicateForDoesNotContain(
            SearchArgument arg);

        /// <summary>
        /// Abstract method to get an EF Core query predicate for processing a 'not equal' operation
        /// </summary>
        /// <param name="arg">Search argument definition</param>
        /// <returns>The EF Core query predicate</returns>
        protected abstract Expression<Func<TEntity, bool>> GetPredicateForIsNotEqualTo(
            SearchArgument arg);

        /// <summary>
        /// Abstract method to get an EF Core query predicate for processing an 'equals' operation
        /// </summary>
        /// <param name="arg">Search argument definition</param>
        /// <returns>The EF Core query predicate</returns>
        protected abstract Expression<Func<TEntity, bool>> GetPredicateForEquals(
            SearchArgument arg);

        /// <summary>
        /// Abstract method to get an EF Core query predicate for processing a 'ends with' operation
        /// </summary>
        /// <param name="arg">Search argument definition</param>
        /// <returns>The EF Core query predicate</returns>
        protected abstract Expression<Func<TEntity, bool>> GetPredicateForEndsWith(
            SearchArgument arg);

        /// <summary>
        /// Abstract method to get an EF Core query predicate for processing a 'starts with' operation
        /// </summary>
        /// <param name="arg">Search argument definition</param>
        /// <returns>The EF Core query predicate</returns>
        protected abstract Expression<Func<TEntity, bool>> GetPredicateForStartsWith(
            SearchArgument arg);

        /// <summary>
        /// Abstract method to get an EF Core query predicate for processing a 'contains' operation
        /// </summary>
        /// <param name="arg">Search argument definition</param>
        /// <returns>The EF Core query predicate</returns>
        protected abstract Expression<Func<TEntity, bool>> GetPredicateForContains(
            SearchArgument arg);
    }
}
