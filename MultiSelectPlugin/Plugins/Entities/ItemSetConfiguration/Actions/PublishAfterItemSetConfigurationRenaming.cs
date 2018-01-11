namespace AdvancedMultiSelect.Plugins.Entities.ItemSetConfiguration.Actions
{
  using Logic.ItemSetConfiguration;

  using Microsoft.Xrm.Sdk;

  // ReSharper disable once RedundantExtendsListEntry
  public class PublishAfterItemSetConfigurationRenaming : PluginBase<Entity>, IPlugin
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="PublishAfterItemSetConfigurationRenaming" /> class.
    /// </summary>
    public PublishAfterItemSetConfigurationRenaming()
      : base(typeof(PublishAfterItemSetConfigurationRenaming))
    {
      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PostOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.pavelkh_PublishAfterItemSetConfigurationRenaming,
        Handler = this.Process
      });
    }

    private void Process(PluginContext pluginContext)
    {
      var manager = new ItemSetConfigurationActionManager(pluginContext);
      manager.PublishAfterRenaming();
    }
  }
}
