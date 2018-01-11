namespace AdvancedMultiSelect.Plugins.Entities.ItemSetConfiguration
{
  using Microsoft.Xrm.Sdk;
  using AdvancedMultiSelect;

  // ReSharper disable once RedundantExtendsListEntry
  public class Update : Base, IPlugin
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="Update"/> class.
    /// </summary>
    public Update()
      : base(typeof(Update))
    {
      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PreValidate,
        Mode = Mode.Synchronous,
        MessageName = MessageName.Update,
        Handler = this.UpdatePreValidationSync
      });

      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PostOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.Update,
        Handler = this.UpdatePostOperationSync
      });
    }

    private void UpdatePreValidationSync(PluginContext pluginContext)
    {
      this.RunInManager(pluginContext, manager => manager.UpdatePreValidationSync());
    }

    private void UpdatePostOperationSync(PluginContext pluginContext)
    {
      this.RunInManager(pluginContext, manager => manager.UpdatePostOperationSync());
    }
  }
}
