namespace TuneMultiSelect.Plugins.Common.TuneMultiSelect
{
  using Logic.TuneMultiSelect;
  using Microsoft.Xrm.Sdk;

  // ReSharper disable once RedundantExtendsListEntry
  public class SaveCheckboxSet : PluginBase<Entity>, IPlugin
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="SaveCheckboxSet" /> class.
    /// </summary>
    public SaveCheckboxSet()
      : base(typeof(SaveCheckboxSet))
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
