namespace TuneMultiSelect.Plugins.Entities.ItemSetConfiguration
{
  using Microsoft.Xrm.Sdk;
  using TuneMultiSelect;

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
        Stage = Stage.PostOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.Update,
        Handler = this.UpdatePostOperationSync
      });

      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PreOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.Update,
        Handler = this.UpdatePreOperationSync
      });
    }

    /// <summary>
    /// Updates the pre operation synchronize.
    /// </summary>
    /// <param name="pluginContext">The local context.</param>
    private void UpdatePreOperationSync(PluginContext pluginContext)
    {
      this.RunInManager(pluginContext, manager => manager.UpdatePreOperationSync());
    }

    /// <summary>
    /// Updates the post synchronize.
    /// </summary>
    /// <param name="pluginContext">The local context.</param>
    private void UpdatePostOperationSync(PluginContext pluginContext)
    {
      this.RunInManager(pluginContext, manager => manager.UpdatePostOperationSync());
    }
  }
}
