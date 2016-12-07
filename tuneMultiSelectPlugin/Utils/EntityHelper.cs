namespace TuneMultiSelect.Utils
{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Linq.Expressions;
  using Microsoft.Xrm.Sdk;
  using Microsoft.Xrm.Sdk.Query;

  /// <summary>
  /// Defines a set of methods to extract Attribute/Field Name of CRM Entity
  /// </summary>
  public enum FieldNameExractMethod
  {
    /// <summary>
    /// Using property name
    /// </summary>
    UsingPropertyName, 

    /// <summary>
    /// Using value of the AttributeLogicalNameAttribute of the property
    /// </summary>
    UsingLogicalNameAttribute
  }

  /// <summary>
  /// The entity helper.
  /// </summary>
  public static class EntityHelper
  {
    /// <summary>
    /// Gets the attribute value.
    /// </summary>
    /// <typeparam name="T">
    /// Type of the attribute
    /// </typeparam>
    /// <param name="entity">
    /// The entity.
    /// </param>
    /// <param name="attributeName">
    /// Name of the attribute.
    /// </param>
    /// <returns>
    /// value of T type
    /// </returns>
    public static T GetAttributeValue<T>(this Entity entity, string attributeName)
    {
      var defaultValue = default(T);
      var attributeExists = entity.Contains(attributeName);
      if (!attributeExists)
      {
        return defaultValue;
      }

      var type = typeof(T);
      var underlyingType = Nullable.GetUnderlyingType(type);

      if (underlyingType != null && underlyingType.IsEnum)
      {
        var value = entity.Attributes[attributeName];
        if (value == null)
        {
          return defaultValue;
        }

        var optionSetValue = (OptionSetValue)value;

        return (T)Enum.ToObject(underlyingType, optionSetValue.Value);
      }

      if (TypeHelper.IsGenericEnumerable(type))
      {
        var value = entity.Attributes[attributeName];
        if (value == null)
        {
          return defaultValue;
        }

        var enumerableType = TypeHelper.GetEnumerableType(type);

        var collection = entity.GetAttributeValue<EntityCollection>(attributeName);

        if (collection?.Entities == null)
        {
          return defaultValue;
        }

        var entities = collection.Entities;
        var containerType = typeof(List<>).MakeGenericType(enumerableType);
        var containerList = (IList)Activator.CreateInstance(containerType);

        var toEntityMethod = typeof(Entity).GetMethod("ToEntity");
        var toEntityGenericMethod = toEntityMethod.MakeGenericMethod(enumerableType);

        foreach (var e in entities)
        {
          containerList.Add(toEntityGenericMethod.Invoke(e, null));
        }

        return (T)containerList;
      }

      return entity.GetAttributeValue<T>(attributeName);
    }

    /// <summary>
    /// Gets the aliased value.
    /// </summary>
    /// <typeparam name="T">
    /// Type of the attribute
    /// </typeparam>
    /// <param name="entity">
    /// The entity.
    /// </param>
    /// <param name="attributeName">
    /// Name of the attribute.
    /// </param>
    /// <returns>
    /// value of T type
    /// </returns>
    public static T GetAliasedValue<T>(this Entity entity, string attributeName)
    {
      var defaultValue = default(T);
      var aliasedValue = GetAttributeValue<AliasedValue>(entity, attributeName);
      if (aliasedValue == null)
      {
        return defaultValue;
      }

      return (T)aliasedValue.Value;
    }

    /// <summary>
    /// Sets the attribute.
    /// </summary>
    /// <typeparam name="T">
    /// Type of the attribute
    /// </typeparam>
    /// <param name="entity">
    /// The entity.
    /// </param>
    /// <param name="key">
    /// The key.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public static void SetAttribute<T>(this Entity entity, string key, T value)
    {
      if (entity.Attributes.ContainsKey(key))
      {
        entity[key] = value;
      }
      else
      {
        entity.Attributes.Add(key, value);
      }
    }

    /// <summary>
    /// Sets the attribute value.
    /// </summary>
    /// <typeparam name="T">
    /// Type of the attribute
    /// </typeparam>
    /// <param name="entity">
    /// The entity.
    /// </param>
    /// <param name="propertyExpression">
    /// The property expression.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public static void SetAttributeValue<T>(this Entity entity, Expression<Func<T>> propertyExpression, T value)
    {
      var propertyName = propertyExpression.ExtractPropertyName().ToLowerInvariant();
      entity.SetAttribute(propertyName, (object)value);
    }

    /// <summary>
    /// Removes the attribute.
    /// </summary>
    /// <param name="entity">
    /// The entity.
    /// </param>
    /// <param name="key">
    /// The key.
    /// </param>
    public static void RemoveAttribute(this Entity entity, string key)
    {
      if (entity.Attributes.ContainsKey(key))
      {
        entity.Attributes.Remove(key);
      }
    }

    /// <summary>
    /// Removes the attribute.
    /// </summary>
    /// <typeparam name="T">
    /// Type of the attribute
    /// </typeparam>
    /// <param name="entity">
    /// The entity.
    /// </param>
    /// <param name="propertyExpression">
    /// The property expression.
    /// </param>
    /// <param name="method">
    /// The method.
    /// </param>
    public static void RemoveAttribute<T>(this Entity entity, Expression<Func<T>> propertyExpression, FieldNameExractMethod method = FieldNameExractMethod.UsingPropertyName)
    {
      var entityAttributeName = propertyExpression.ExtractEntityAttributeName(method);
      entity.RemoveAttribute(entityAttributeName);
    }

    /// <summary>
    /// Determines whether the ColumnSet contains the column specified by lambda expression.
    /// </summary>
    /// <typeparam name="T">
    /// Type of the column
    /// </typeparam>
    /// <param name="columnSet">
    /// The column set.
    /// </param>
    /// <param name="propertyExpression">
    /// The property expression.
    /// </param>
    /// <param name="method">
    /// The method.
    /// </param>
    /// <returns>
    /// <c>True</c> if the column found
    /// </returns>
    public static bool Contains<T>(this ColumnSet columnSet, Expression<Func<T>> propertyExpression, FieldNameExractMethod method = FieldNameExractMethod.UsingPropertyName)
    {
      var entityAttributeName = propertyExpression.ExtractEntityAttributeName(method);
      return columnSet.Columns.Contains(entityAttributeName);
    }

    /// <summary>
    /// Adds the column to the column set.
    /// </summary>
    /// <typeparam name="T">
    /// Type of the column
    /// </typeparam>
    /// <param name="columnSet">
    /// The column set.
    /// </param>
    /// <param name="propertyExpression">
    /// The property expression.
    /// </param>
    /// <param name="method">
    /// The method.
    /// </param>
    public static void AddColumn<T>(this ColumnSet columnSet, Expression<Func<T>> propertyExpression, FieldNameExractMethod method = FieldNameExractMethod.UsingPropertyName)
    {
      var entityAttributeName = propertyExpression.ExtractEntityAttributeName(method);
      columnSet.AddColumn(entityAttributeName);
    }

    /// <summary>
    /// Removes the attribute.
    /// </summary>
    /// <typeparam name="T">
    /// Type of the column
    /// </typeparam>
    /// <param name="attributeCollection">
    /// The attribute collection.
    /// </param>
    /// <param name="propertyExpression">
    /// The property expression.
    /// </param>
    /// <param name="method">
    /// The method.
    /// </param>
    public static void Remove<T>(this AttributeCollection attributeCollection, Expression<Func<T>> propertyExpression, FieldNameExractMethod method = FieldNameExractMethod.UsingPropertyName)
    {
      var entityAttributeName = propertyExpression.ExtractEntityAttributeName(method);
      attributeCollection.Remove(entityAttributeName);
    }

    /// <summary>
    /// Gets the formatted value.
    /// </summary>
    /// <param name="entity">
    /// The entity.
    /// </param>
    /// <param name="fieldName">
    /// Name of the field.
    /// </param>
    /// <returns>
    /// The <see cref="string"/>.
    /// </returns>
    public static string GetFormattedValue(this Entity entity, string fieldName)
    {
      var formattedValues = entity.FormattedValues;
      var labelNotFound = formattedValues == null;
      if (labelNotFound)
      {
        return null;
      }

      return
        formattedValues.ContainsKey(fieldName) ? formattedValues[fieldName] : null;
    }

    /// <summary>
    /// Gets the formatted value.
    /// </summary>
    /// <typeparam name="T">
    /// Type of the attribute
    /// </typeparam>
    /// <param name="entity">
    /// The entity.
    /// </param>
    /// <param name="propertyExpression">
    /// The property expression.
    /// </param>
    /// <param name="method">
    /// The method.
    /// </param>
    /// <returns>
    /// The <see cref="string"/>.
    /// </returns>
    public static string GetFormattedValue<T>(this Entity entity, Expression<Func<T>> propertyExpression, FieldNameExractMethod method = FieldNameExractMethod.UsingPropertyName)
    {
      var propertyName = propertyExpression.ExtractPropertyName().ToLowerInvariant();
      return entity.GetFormattedValue(propertyName);
    }

    /// <summary>
    /// Gets the name of the CRM Entity attribute.
    /// </summary>
    /// <typeparam name="T">
    /// Type of the property
    /// </typeparam>
    /// <param name="propertyExpression">
    /// The property expression.
    /// </param>
    /// <param name="method">
    /// The method.
    /// </param>
    /// <returns>
    /// The name of the entity attribute
    /// </returns>
    /// <exception cref="NotImplementedException">
    /// Attribute name extraction method is not supported.
    /// </exception>
    public static string ExtractEntityAttributeName<T>(this Expression<Func<T>> propertyExpression, FieldNameExractMethod method)
    {
      switch (method)
      {
        case FieldNameExractMethod.UsingPropertyName:
          return propertyExpression.ExtractPropertyName().ToLowerInvariant();

        case FieldNameExractMethod.UsingLogicalNameAttribute:
          var propertyAttribute = propertyExpression.ExtractPropertyAttribute<T, AttributeLogicalNameAttribute>();
          return propertyAttribute.LogicalName.ToLowerInvariant();

        default:
          throw new NotImplementedException("Attribute name extraction method is not supported.");
      }
    }
  }
}
