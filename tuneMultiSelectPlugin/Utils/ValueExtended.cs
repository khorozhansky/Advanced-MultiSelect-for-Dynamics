namespace TuneMultiSelect.Utils
{
  using System.Collections;
  using System.Linq;
  using Microsoft.Xrm.Sdk;

  public class ValueExtended<T>
  {
    private T oldValue;
    private T newValue;

    /// <summary>
    /// Gets or sets the old value.
    /// </summary>
    /// <value>The old value.</value>
    public T OldValue
    {
      get
      {
        return this.oldValue;
      }

      set
      {
        if (value != null && typeof(T) == typeof(string))
        {
          if (value.ToString() == string.Empty)
          {
            value = default(T);
          }
        }

        this.oldValue = value;
      }
    }

    /// <summary>
    /// Gets or sets the new value.
    /// </summary>
    /// <value>The new value.</value>
    public T NewValue
    {
      get
      {
        return this.newValue;
      }

      set
      {
        if (value != null && typeof(T) == typeof(string))
        {
          if (value.ToString() == string.Empty)
          {
            value = default(T);
          }
        }

        this.newValue = value;
      }
    }

    public bool IsSpecified { get; set; }

    /// <summary>
    /// Gets the value (the new one if it's specified, otherwise the old one ).
    /// </summary>
    /// <value>
    /// The value.
    /// </value>
    public T Value
    {
      get
      {
        return this.IsSpecified ? this.NewValue : this.OldValue;
      }
    }

    /// <summary>
    /// Gets or sets the old value formatted.
    /// </summary>
    /// <value>The old value formatted.</value>
    public string OldValueFormatted { get; set; }

    /// <summary>
    /// Gets or sets the new value formatted.
    /// </summary>
    /// <value>The new value formatted.</value>
    public string NewValueFormatted { get; set; }

    /// <summary>
    /// Gets the value formatted.
    /// </summary>
    /// <value>The value formatted.</value>
    public string ValueFormatted
    {
      get
      {
        return this.IsSpecified ? this.NewValueFormatted : this.OldValueFormatted;
      }
    }

    /// <summary>
    /// Gets a value indicating whether [the value is modified].
    /// </summary>
    /// <value>
    ///   <c>true</c> if [the value is modified]; otherwise, <c>false</c>.
    /// </value>
    public bool IsModified
    {
      get
      {
        if (!this.IsSpecified)
        {
          return false;
        }

        if (this.NewValue == null)
        {
          return this.OldValue != null;
        }

        if (this.OldValue == null)
        {
          return true;
        }

        var type = typeof(T);
        if (TypeHelper.IsGenericEnumerable(type))
        {
          var oldIdList = ((IEnumerable)this.OldValue)
            .Cast<Entity>()
            .Select(r => r.Id)
            .ToList();

          var newIdList = ((IEnumerable)this.NewValue)
            .Cast<Entity>()
            .Select(r => r.Id)
            .ToList();

          var difference = 
            oldIdList.Except(newIdList)
            .Union(newIdList.Except(oldIdList));

          return difference.Any();
        }

        return !this.NewValue.Equals(this.OldValue);
      }
    }

    /// <summary>
    /// Gets a value indicating whether [new value gets set to null].
    /// </summary>
    /// <value>
    ///   <c>true</c> if [new value gets set to null]; otherwise, <c>false</c>.
    /// </value>
    public bool IsSetToNull
    {
      get
      {
        return this.IsSpecified && this.NewValue == null;
      }
    }

    /// <summary>
    /// Gets a value indicating whether the value will be null after the action.
    /// </summary>
    /// <value>
    ///   <c>true</c> if [it will be null]; otherwise, <c>false</c>.
    /// </value>
    public bool IsNull
    {
      get
      {
        return this.Value == null;
      }
    }
  }
}
