using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.SqlServer
{
    /// <summary>
    /// Interface for dependent child entities that allows triggering logic
    /// before save and after save events.
    /// </summary>
    public interface IDependentEntityCollection
    {
        /// <summary>
        /// Call this method to perform logic after saving
        /// </summary>
        void AfterSave();
        
        /// <summary>
        /// Call this method to perform logic before saving
        /// </summary>
        /// <param name="dbContext">EF Core database context instance to be used by the save operation</param>
        void BeforeSave(DbContext dbContext);
    }
}
