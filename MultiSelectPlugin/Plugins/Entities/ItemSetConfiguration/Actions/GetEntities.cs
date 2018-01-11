namespace AdvancedMultiSelect.Plugins.Entities.ItemSetConfiguration.Actions
{
  using Logic.ItemSetConfiguration;

  using Microsoft.Xrm.Sdk;

  // ReSharper disable once RedundantExtendsListEntry
  public class GetEntities : PluginBase<Entity>, IPlugin
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="GetEntities" /> class.
    /// </summary>
    public GetEntities()
      : base(typeof(GetEntities))
    {
      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PostOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.pavelkh_ItemSetConfigurationGetEntities,
        Handler = this.Process
      });
    }

    private void Process(PluginContext pluginContext)
    {
      var manager = new ItemSetConfigurationActionManager(pluginContext);
      manager.ProcessGetEntitiesAction();
    }
  }
}
