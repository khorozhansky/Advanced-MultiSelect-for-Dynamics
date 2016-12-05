namespace TuneMultiSelect.Logic.TuneMultiSelect
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
      var itemSetName = pluginContext.GetInputParameter<string>(ItemSetChangeManager.CustomSaveHandlerItemSetParamName);
      if (string.IsNullOrWhiteSpace(itemSetName))
      {
        throw new InvalidPluginExecutionException("ItemSetName parameter is not passed to the " + pluginContext.ExecContext.MessageName + " action.");
      }

      this.EntityRef = pluginContext.GetInputParameter<EntityReference>(ItemSetChangeManager.CustomSaveHandlerEntityRefParamName);
      if (this.EntityRef == null)
      {
        throw new InvalidPluginExecutionException("EntityRef parameter is not passed to the " + pluginContext.ExecContext.MessageName + " action.");
      }

      var selectedIds = pluginContext.GetInputParameter<string>(ItemSetChangeManager.CustomSaveHandlerSelectedIdListParamName);
      var unselectedIds = pluginContext.GetInputParameter<string>(ItemSetChangeManager.CustomSaveHandlerUnselectedIdListParamName);

      this.SelectedIdList = ItemSetChangeManager.ParseIdList(selectedIds);
      this.UnselectedIdList = ItemSetChangeManager.ParseIdList(unselectedIds);
    }

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
    /// Gets the list of unselected identifiers.
    /// </summary>
    /// <value>
    /// The unselected identifier list.
    /// </value>
    protected IList<Guid> UnselectedIdList { get; private set; }

    /// <summary>
    /// Processes the item set changes. 
    /// <remarks>
    /// Place your logic in it
    /// </remarks>
    /// </summary>
    public abstract void ProcessItemSetChanges();
  }
}