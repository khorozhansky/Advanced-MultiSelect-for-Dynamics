namespace TuneMultiSelect.Plugins.Common.TuneMultiSelect
{
  using Logic.TuneMultiSelect;
  using Microsoft.Xrm.Sdk;

  // ReSharper disable once RedundantExtendsListEntry
  public class ProcessChangesForMarketingListItemSet : PluginBase<Entity>, IPlugin
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessChangesForMarketingListItemSet" /> class.
    /// </summary>
    public ProcessChangesForMarketingListItemSet()
      : base(typeof(ProcessChangesForMarketingListItemSet))
    {
      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PostOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.tunexrm_ProcessChangesForMarketingListItemSet,
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
