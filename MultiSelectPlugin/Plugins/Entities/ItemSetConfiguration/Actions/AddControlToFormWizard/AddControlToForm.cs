namespace AdvancedMultiSelect.Plugins.Entities.ItemSetConfiguration.Actions.AddControlToFormWizard
{
  using Logic.ItemSetConfiguration.AddControlToFormWizard;

  using Microsoft.Xrm.Sdk;

  // ReSharper disable once RedundantExtendsListEntry
  public class AddControlToForm : PluginBase<Entity>, IPlugin
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="AddControlToForm" /> class.
    /// </summary>
    public AddControlToForm()
      : base(typeof(AddControlToForm))
    {
      this.RegisteredPluginSteps.Add(new PluginStepBase
      {
        Stage = Stage.PostOperation,
        Mode = Mode.Synchronous,
        MessageName = MessageName.pavelkh_ItemSetConfigurationAddControlOnForm,
        Handler = this.Process
      });
    }

    private void Process(PluginContext pluginContext)
    {
      var manager = new Manager(pluginContext);
      manager.ProcessAddControlOnFormAction();
    }
  }
}
