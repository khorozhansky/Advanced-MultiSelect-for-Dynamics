namespace AdvancedMultiSelect.Plugins.Entities.ItemSetConfiguration
{
  using Microsoft.Xrm.Sdk;
  using AdvancedMultiSelect;

  // ReSharper disable once RedundantExtendsListEntry
  public class Delete : Base, IPlugin
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="Delete"/> class.
    /// </summary>
    public Delete()
      : base(typeof(Delete))
    {
      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PostOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.Delete,
        Handler = this.DeletePostOperationSync
      });
    }

    private void DeletePostOperationSync(PluginContext pluginContext)
    {
      this.RunInManager(pluginContext, manager => manager.DeletePostOperationSync());
    }
  }
}
