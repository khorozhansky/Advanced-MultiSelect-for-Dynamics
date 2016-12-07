namespace TuneMultiSelect.Plugins.Common.TuneMultiSelect
{
  using Microsoft.Xrm.Sdk;

  // ReSharper disable once RedundantExtendsListEntry
  public class ItemSetConfigurationGetEntities : PluginBase<Entity>, IPlugin
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemSetConfigurationGetEntities" /> class.
    /// </summary>
    public ItemSetConfigurationGetEntities()
      : base(typeof(ItemSetConfigurationGetEntities))
    {
      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PostOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.tunexrm_ItemSetConfigurationGetEntities,
        Handler = this.Process
      });
    }

    private void Process(PluginContext pluginContext)
    {
      var manager = new Logic.TuneMultiSelect.ItemSetConfigurationActionManager(pluginContext);
      manager.ProcessGetEntitiesAction();
    }
  }
}
