namespace AdvancedMultiSelect.Plugins.Entities.ItemSetConfiguration.Actions
{
  using Logic.ItemSetConfiguration;

  using Microsoft.Xrm.Sdk;

  // ReSharper disable once RedundantExtendsListEntry
  public class Export : PluginBase<Entity>, IPlugin
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="Export" /> class.
    /// </summary>
    public Export()
      : base(typeof(Export))
    {
      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PostOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.pavelkh_ItemSetConfigurationExport,
        Handler = this.Process
      });
    }

    private void Process(PluginContext pluginContext)
    {
      var manager = new ItemSetConfigurationActionManager(pluginContext);
      manager.Export();
    }
  }
}
