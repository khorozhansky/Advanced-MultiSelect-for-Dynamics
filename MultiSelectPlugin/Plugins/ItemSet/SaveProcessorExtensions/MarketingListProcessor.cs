namespace AdvancedMultiSelect.Plugins.ItemSet.SaveProcessorExtensions
{
  using Microsoft.Xrm.Sdk;

  using Logic.ItemSet;

  // ReSharper disable once RedundantExtendsListEntry
  public class MarketingListProcessor : PluginBase<Entity>, IPlugin
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="MarketingListProcessor" /> class.
    /// </summary>
    public MarketingListProcessor()
      : base(typeof(MarketingListProcessor))
    {
      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PostOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.pavelkh_ProcessChangesForMarketingListItemSet,
        Handler = this.Process
      });
    }

    private void Process(PluginContext pluginContext)
    {
      var manager = new CustomItemSetChangeManager(pluginContext);
      manager.ProcessItemSetChanges();
    }
  }
}
