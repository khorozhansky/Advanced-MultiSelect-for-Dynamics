namespace DemoForAdvancedMultiSelectPlugin.Plugins.InstallDemoData
{
  using AdvancedMultiSelect;

  using Logic.InstallDemoData;

  using Microsoft.Xrm.Sdk;

  // ReSharper disable once RedundantExtendsListEntry
  public class InstallDemoData : PluginBase<Entity>, IPlugin
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="InstallDemoData" /> class.
    /// </summary>
    public InstallDemoData()
      : base(typeof(InstallDemoData))
    {
      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PostOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.pavelkh_DemoInstallDemoData,
        Handler = this.ProcessGetItemSet
      });
    }

    private void ProcessGetItemSet(PluginContext pluginContext)
    {
      var manager = new InstallDemoDataManager(pluginContext);
      manager.InstallData();
    }
  }
}
