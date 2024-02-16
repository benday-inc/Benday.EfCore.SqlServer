namespace Benday.Repositories;

public interface IEntityBaseWithDependents<TIdentity> : IEntityBase<TIdentity> where TIdentity : IComparable<TIdentity>
{
    IList<IDependentEntityCollection> GetDependentEntities();
}
