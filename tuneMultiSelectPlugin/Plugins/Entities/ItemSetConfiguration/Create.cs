namespace TuneMultiSelect.Plugins.Entities.ItemSetConfiguration
{
  using Microsoft.Xrm.Sdk;
  using TuneMultiSelect;

  public class Create : Base, IPlugin
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="Create"/> class.
    /// </summary>
    public Create()
      : base(typeof(Create))
    {
      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PostOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.Create,
        Handler = this.CreatePostOperationSync
      });

      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PreOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.Create,
        Handler = this.CreatePreOperationSync
      });
    }

    /// <summary>
    /// Creates the pre operation synchronize.
    /// </summary>
    /// <param name="pluginContext">The local context.</param>
    private void CreatePreOperationSync(PluginContext pluginContext)
    {
      this.RunInManager(pluginContext, manager => manager.CreatePreOperationSync());
    }

    /// <summary>
    /// Creates the post synchronize.
    /// </summary>
    /// <param name="pluginContext">The local context.</param>
    private void CreatePostOperationSync(PluginContext pluginContext)
    {
      this.RunInManager(pluginContext, manager => manager.CreatePostOperationSync());
    }
  }
}
