namespace TuneMultiSelect.Plugins.Entities.ItemSetConfiguration
{
  using System;

  using Logic.TuneMultiSelect;
  using TuneMultiSelect;
  using TuneMultiSelect.Entities;

  public abstract class Base : PluginBase<tunexrm_tunemultiselectitemsetconfiguration>
  {
    protected Base(Type childClassName, string unsecureConfig = null, string secureConfig = null) : base(childClassName, unsecureConfig, secureConfig)
    {
    }

    protected void RunInManager(PluginContext pluginContext, Action<ItemSetConfigurationManager> action)
    {
      var manager = new ItemSetConfigurationManager(pluginContext);
      action(manager);
    }
  }
}
