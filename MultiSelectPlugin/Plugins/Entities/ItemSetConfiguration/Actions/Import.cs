namespace AdvancedMultiSelect.Plugins.Entities.ItemSetConfiguration.Actions
{
  using Logic.ItemSetConfiguration;

  using Microsoft.Xrm.Sdk;

  // ReSharper disable once RedundantExtendsListEntry
  public class Import : PluginBase<Entity>, IPlugin
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="Import" /> class.
    /// </summary>
    public Import()
      : base(typeof(Import))
    {
      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PostOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.pavelkh_ItemSetConfigurationImport,
        Handler = this.Process
      });
    }

    private void Process(PluginContext pluginContext)
    {
      var manager = new ItemSetConfigurationActionManager(pluginContext);
      manager.Import();
    }
  }
}
