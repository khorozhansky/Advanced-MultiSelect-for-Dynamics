namespace TuneMultiSelect.Logic
{
  using System;
  using System.Collections.Generic;
  using Microsoft.Xrm.Sdk;

  /// <summary>
  /// Base class for entity manager classes
  /// </summary>
  /// <typeparam name="T">Entity</typeparam>
  public class ManagerBase<T> where T : Entity, new()
  {
    protected readonly PluginBase<T>.PluginContext PluginContext;

    /// <summary>
    /// Gets or sets the target validators.
    /// </summary>
    /// <value>
    /// The target validators.
    /// </value>
    private static readonly Dictionary<MessageName, Action<PluginBase<T>.PluginContext>[]> BaseMessageValidators;

    /// <summary>
    /// Initializes static members of the <see cref="ManagerBase{T}"/> class. 
    /// </summary>
    static ManagerBase()
    {
      BaseMessageValidators = new Dictionary<MessageName, Action<PluginBase<T>.PluginContext>[]>
      {
        {
          MessageName.Create, new Action<PluginBase<T>.PluginContext>[]
          {
            ValidateTargetAsEntity
          }
        },
        {
          MessageName.Update, new Action<PluginBase<T>.PluginContext>[]
          {
            ValidateTargetAsEntity
          }
        },
        {
          MessageName.Delete, new Action<PluginBase<T>.PluginContext>[]
          {
            ValidateTargetEntityReference
          }
        }
      };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagerBase{T}" /> class.
    /// </summary>
    /// <param name="pluginContext">The local context.</param>
    public ManagerBase(PluginBase<T>.PluginContext pluginContext)
    {
      this.PluginContext = pluginContext;
    }

    protected void ValidateByBaseValidators()
    {
      var messageName = this.PluginContext.ExecContext.MessageName;
      MessageName message;
      if (!Enum.TryParse(messageName, true, out message))
      {
        var errorMessage = string.Format("Plugin Message is not resolved: {0}", messageName);
        throw new NotImplementedException(errorMessage);
      }

      if (!BaseMessageValidators.ContainsKey(message))
      {
        return;
      }

      var validators = BaseMessageValidators[message];
      foreach (var validator in validators)
      {
        validator(this.PluginContext);
      }
    }

    /// <summary>
    /// Validates the target as entity.
    /// </summary>
    /// <param name="pluginContext">The local context.</param>
    /// <exception cref="NullReferenceException">InputTargetAsEntity</exception>
    private static void ValidateTargetAsEntity(PluginBase<T>.PluginContext pluginContext)
    {
      if (pluginContext.InputTargetAsEntity == null)
      {
        throw new NullReferenceException("InputTargetAsEntity");
      }
    }

    /// <summary>
    /// Validates the target entity reference.
    /// </summary>
    /// <param name="pluginContext">The local context.</param>
    private static void ValidateTargetEntityReference(PluginBase<T>.PluginContext pluginContext)
    {
      if (pluginContext.InputTargetEntityReference == null)
      {
        throw new NullReferenceException("InputTargetEntityReference");
      }
    }
  }
}
