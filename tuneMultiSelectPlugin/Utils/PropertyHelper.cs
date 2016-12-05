namespace TuneMultiSelect.Utils
{
  using System;
  using System.Linq.Expressions;
  using System.Reflection;

  public static class PropertyHelper
  {
    /// <summary>
    /// Extracts the name of the property.
    /// </summary>
    /// <typeparam name="T">Property Type</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <returns>
    /// Property Name
    /// </returns>
    /// <exception cref="System.ArgumentException">@Expression must be a MemberExpression.;propertyExpression</exception>
    public static string ExtractPropertyName<T>(this Expression<Func<T>> propertyExpression)
    {
      var memberExpression = propertyExpression.Body as MemberExpression;

      if (memberExpression == null)
      {
        throw new ArgumentException(@"Expression must be a MemberExpression.", "propertyExpression");
      }

      return memberExpression.Member.Name;
    }

    /// <summary>
    /// Extracts the property attribute.
    /// </summary>
    /// <typeparam name="T">Type of the property</typeparam>
    /// <typeparam name="TK">The type of the k.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <returns>Property attribute</returns>
    /// <exception cref="System.ArgumentException">
    /// @Expression must be a lambda expression like r=>r.SomeProperty.;propertyExpression
    /// or
    /// @Expression must be a lambda expression like r=>r.SomeProperty.;propertyExpression
    /// or
    /// The property does not have such attribute specified;propertyExpression
    /// </exception>
    public static TK ExtractPropertyAttribute<T, TK>(this Expression<Func<T>> propertyExpression)
    {
      var memberExpression = propertyExpression.Body as MemberExpression;
      if (memberExpression == null)
      {
        throw new ArgumentException(@"Expression must be a lambda expression like r=>r.SomeProperty.", "propertyExpression");
      }

      var property = memberExpression.Member as PropertyInfo;
      if (property == null)
      {
        throw new ArgumentException(@"Expression must be a lambda expression like r=>r.SomeProperty.", "propertyExpression");
      }

      var attributes = property.GetCustomAttributes(typeof(TK), false);
      var attributeNotFound = attributes.Length == 0;
      if (attributeNotFound)
      {
        throw new ArgumentException("The property does not have such attribute specified", "propertyExpression");
      }

      return (TK)attributes[0];
    }
  }
}
