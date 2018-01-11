namespace AdvancedMultiSelect.Logic.ItemSet
{
  using System;
  using System.Collections.Generic;

  using Microsoft.Xrm.Sdk;

  /// <summary>
  /// This is a base class for all custom save actions handlers
  /// </summary>
  public abstract class CustomItemSetChangeManagerBase : ManagerBase<Entity>
  {
    protected CustomItemSetChangeManagerBase(PluginBase<Entity>.PluginContext pluginContext)
      : base(pluginContext)
    {
      this.ItemSetName = pluginContext.GetInputParameter<string>(ItemSetChangeManager.CustomSaveHandlerItemSetParamName);
      if (string.IsNullOrWhiteSpace(this.ItemSetName))
      {
        throw new InvalidPluginExecutionException("ItemSetName parameter is not passed to the " + pluginContext.ExecContext.MessageName + " action.");
      }

      this.EntityRef = pluginContext.GetInputParameter<EntityReference>(ItemSetChangeManager.CustomSaveHandlerEntityRefParamName);
      if (this.EntityRef == null)
      {
        throw new InvalidPluginExecutionException("EntityRef parameter is not passed to the " + pluginContext.ExecContext.MessageName + " action.");
      }

      var selectedIds = pluginContext.GetInputParameter<string>(ItemSetChangeManager.CustomSaveHandlerSelectedIdListParamName);

      this.SelectedIdList = ItemSetChangeManager.ParseIdList(selectedIds);
    }

    protected string ItemSetName { get; set; }

    /// <summary>
    /// Gets or sets the entity reference to the entity record being saved.
    /// </summary>
    /// <value>
    /// The entity reference.
    /// </value>
    protected EntityReference EntityRef { get; set; }

    /// <summary>
    /// Gets the list of selected identifiers.
    /// </summary>
    /// <value>
    /// The selected identifier list.
    /// </value>
    protected IList<Guid> SelectedIdList { get; private set; }

    /// <summary>
    /// Processes the item set changes. 
    /// <remarks>
    /// Place your logic in it
    /// </remarks>
    /// </summary>
    public abstract void ProcessItemSetChanges();
  }
}