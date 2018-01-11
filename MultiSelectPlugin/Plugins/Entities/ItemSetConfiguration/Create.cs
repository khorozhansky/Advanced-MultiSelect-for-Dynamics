namespace AdvancedMultiSelect.Plugins.Entities.ItemSetConfiguration
{
  using Microsoft.Xrm.Sdk;
  using AdvancedMultiSelect;

  // ReSharper disable once RedundantExtendsListEntry
  public class Create : Base, IPlugin
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="Create"/> class.
    /// </summary>
    public Create()
      : base(typeof(Create))
    {
      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PreValidate,
        Mode = Mode.Synchronous,
        MessageName = MessageName.Create,
        Handler = this.CreatePreValidationSync
      });

      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PostOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.Create,
        Handler = this.CreatePostOperationSync
      });
    }

    private void CreatePreValidationSync(PluginContext pluginContext)
    {
      this.RunInManager(pluginContext, manager => manager.CreatePreValidationSync());
    }

    private void CreatePostOperationSync(PluginContext pluginContext)
    {
      this.RunInManager(pluginContext, manager => manager.CreatePostOperationSync());
    }
  }
}
