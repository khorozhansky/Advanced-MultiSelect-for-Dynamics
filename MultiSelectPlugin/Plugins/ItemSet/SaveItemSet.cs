namespace AdvancedMultiSelect.Plugins.ItemSet
{
  using Microsoft.Xrm.Sdk;

  using Logic.ItemSet;

  // ReSharper disable once RedundantExtendsListEntry
  public class SaveItemSet : PluginBase<Entity>, IPlugin
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="SaveItemSet" /> class.
    /// </summary>
    public SaveItemSet()
      : base(typeof(SaveItemSet))
    {
      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PreValidate,
        Mode = Mode.Synchronous,
        MessageName = MessageName.Create,
        Handler = this.PrepareRelationsUpdate
      });

      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PostOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.Create,
        Handler = this.ProcessRelationsUpdate
      });

      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PreValidate,
        Mode = Mode.Synchronous,
        MessageName = MessageName.Update,
        Handler = this.PrepareRelationsUpdate
      });

      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PostOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.Update,
        Handler = this.ProcessRelationsUpdate
      });
    }

    /// <summary>
    /// Prepares the relations update.
    /// </summary>
    /// <param name="pluginContext">The local context.</param>
    private void PrepareRelationsUpdate(PluginContext pluginContext)
    {
      var manager = new ItemSetChangeManager(pluginContext);
      manager.PrepareRelationsUpdate();
    }

    /// <summary>
    /// Processes the relations update.
    /// </summary>
    /// <param name="pluginContext">The local context.</param>
    private void ProcessRelationsUpdate(PluginContext pluginContext)
    {
      var manager = new ItemSetChangeManager(pluginContext);
      manager.UpdateRelations();
    }
  }
}
