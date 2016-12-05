namespace TuneMultiSelect.Plugins.Common.TuneMultiSelect
{
  using Logic.TuneMultiSelect;
  using Microsoft.Xrm.Sdk;

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
        MessageName = MessageName.tunexrm_GetItemSet,
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
