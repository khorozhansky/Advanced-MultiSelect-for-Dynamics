namespace AdvancedMultiSelect.Plugins.Entities.ItemSetConfiguration
{
  using System;

  using AdvancedMultiSelect;
  using AdvancedMultiSelect.CrmProxy;
  using AdvancedMultiSelect.Logic.ItemSetConfiguration;

  public abstract class Base : PluginBase<pavelkh_advancedmultiselectitemsetconfiguration>
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
