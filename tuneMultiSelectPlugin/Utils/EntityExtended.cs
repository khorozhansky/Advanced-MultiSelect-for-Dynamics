namespace TuneMultiSelect.Utils
{
  using System;
  using System.Linq.Expressions;
  using Microsoft.Xrm.Sdk;

  public class EntityExtended<TEntity> where TEntity : Entity, new() 
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityExtended{TEntity}"/> class. 
    /// </summary>
    /// <param name="target">
    /// The target.
    /// </param>
    /// <param name="preImage">
    /// The pre image.
    /// </param>
    public EntityExtended(TEntity target, TEntity preImage)
    {
      this.Target = target;
      this.PreImage = preImage;
    }

    /// <summary>
    /// Gets the change entity.
    /// </summary>
    /// <value>
    /// The change entity.
    /// </value>
    public TEntity Target { get; }

    /// <summary>
    /// Gets the pre image.
    /// </summary>
    /// <value>
    /// The pre image.
    /// </value>
    public TEntity PreImage { get; }

    /// <summary>
    /// Gets the id.
    /// </summary>
    /// <value>The id.</value>
    public Guid Id => this.PreImage?.Id ?? this.Target.Id;

    /// <summary>
    /// Gets the name of the logical.
    /// </summary>
    /// <value>The name of the logical.</value>
    public string LogicalName => this.PreImage == null ? this.Target.LogicalName : this.PreImage.LogicalName;

    /// <summary>
    /// To the entity reference.
    /// </summary>
    /// <returns>EntityReference.</returns>
    public EntityReference ToEntityReference()
    {
      return new EntityReference(this.LogicalName, this.Id);
    }

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <typeparam name="T">Entity Field Type</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <returns>
    /// Extended Field Value
    /// </returns>
    public ValueExtended<T> GetValue<T>(Expression<Func<T>> propertyExpression)
    {
      var propertyName = propertyExpression.ExtractPropertyName();
      return this.GetValue<T>(propertyName);
    }

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <typeparam name="T">Entity Field Type</typeparam>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns>
    /// Extended Field Value
    /// </returns>
    public ValueExtended<T> GetValue<T>(string propertyName)
    {
      var attributeName = propertyName.ToLower();
      var result = new ValueExtended<T>();
      var preImageEntity = this.PreImage;
      if (preImageEntity != null)
      {
        var oldValue = default(T);
        var attributeExists = preImageEntity.Contains(attributeName);
        if (attributeExists)
        {
          oldValue = preImageEntity.GetAttributeValue<T>(attributeName);
        }

        result.OldValue = oldValue;
        result.OldValueFormatted = preImageEntity.GetFormattedValue(attributeName);
      }

      var entity = this.Target;
      if (entity != null)
      {
        result.IsSpecified = entity.Contains(attributeName);

        var newValue = default(T);
        if (result.IsSpecified)
        {
          newValue = entity.GetAttributeValue<T>(attributeName);
        }

        result.NewValue = newValue;
        result.NewValueFormatted = entity.GetFormattedValue(attributeName);
      }

      return result;
    }
  }
}
