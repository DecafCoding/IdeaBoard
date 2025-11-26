using System.Reflection;
using IdeaBoard.Shared.DataEntities;
using IdeaBoard.Shared.Services;

namespace IdeaBoard.Shared.DataServices;

public abstract class BaseDataService<TEntity> where TEntity : class
{
    protected readonly SupabaseService SupabaseService;
    protected readonly string TableName;

    protected BaseDataService(SupabaseService supabaseService)
    {
        SupabaseService = supabaseService;
        TableName = GetTableName();
    }

    /// <summary>
    /// Gets the table name from the [Table] attribute on the entity.
    /// </summary>
    private string GetTableName()
    {
        var tableAttribute = typeof(TEntity).GetCustomAttribute<TableAttribute>();
        if (tableAttribute == null)
        {
            throw new InvalidOperationException(
                $"Entity {typeof(TEntity).Name} must have a [Table] attribute.");
        }
        return tableAttribute.Name;
    }

    /// <summary>
    /// Gets all records for the current user (RLS enforced by auth token).
    /// </summary>
    public virtual async Task<List<TEntity>> GetAllAsync()
    {
        return await SupabaseService.GetAsync<TEntity>(TableName);
    }

    /// <summary>
    /// Gets a single record by ID (RLS enforced by auth token).
    /// </summary>
    public virtual async Task<TEntity?> GetByIdAsync(Guid id)
    {
        return await SupabaseService.GetByIdAsync<TEntity>(TableName, id);
    }

    /// <summary>
    /// Creates a new record with automatic timestamp management.
    /// </summary>
    public virtual async Task<TEntity> CreateAsync(TEntity entity)
    {
        // Set timestamps
        SetTimestamps(entity, isNew: true);

        return await SupabaseService.PostAsync(TableName, entity);
    }

    /// <summary>
    /// Updates an existing record with automatic timestamp management.
    /// </summary>
    public virtual async Task<TEntity> UpdateAsync(Guid id, TEntity entity)
    {
        // Update timestamp
        SetTimestamps(entity, isNew: false);

        return await SupabaseService.UpdateByIdAsync(TableName, id, entity);
    }

    /// <summary>
    /// Deletes a record (RLS enforced by auth token).
    /// </summary>
    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        return await SupabaseService.DeleteByIdAsync(TableName, id);
    }

    /// <summary>
    /// Sets CreatedAt and UpdatedAt timestamps using reflection.
    /// </summary>
    private void SetTimestamps(TEntity entity, bool isNew)
    {
        var now = DateTime.UtcNow;
        var type = typeof(TEntity);

        if (isNew)
        {
            var createdAtProp = type.GetProperty("CreatedAt");
            if (createdAtProp != null && createdAtProp.CanWrite)
            {
                createdAtProp.SetValue(entity, now);
            }
        }

        var updatedAtProp = type.GetProperty("UpdatedAt");
        if (updatedAtProp != null && updatedAtProp.CanWrite)
        {
            updatedAtProp.SetValue(entity, now);
        }
    }
}
