namespace TuneMultiSelect.Plugins.Entities.ItemSetConfiguration
{
  using Microsoft.Xrm.Sdk;
  using TuneMultiSelect;

  // ReSharper disable once RedundantExtendsListEntry
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
  }
}
