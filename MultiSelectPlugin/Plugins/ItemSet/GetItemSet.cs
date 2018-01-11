namespace AdvancedMultiSelect.Plugins.ItemSet
{
  using Microsoft.Xrm.Sdk;

  using Logic.ItemSet;

  // ReSharper disable once RedundantExtendsListEntry
  public class GetItemSet : PluginBase<Entity>, IPlugin
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="GetItemSet" /> class.
    /// </summary>
    public GetItemSet()
      : base(typeof(GetItemSet))
    {
      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PostOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.pavelkh_GetItemSet,
        Handler = this.ProcessGetItemSet
      });
    }

    private void ProcessGetItemSet(PluginContext pluginContext)
    {
      var manager = new ItemSetBuilder(pluginContext);
      manager.BuildItemSet();
    }
  }
}
