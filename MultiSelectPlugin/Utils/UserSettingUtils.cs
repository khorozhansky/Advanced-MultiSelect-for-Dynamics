namespace AdvancedMultiSelect.Utils
{
  using System.Linq;

  public static class UserSettingUtils
  {
    public static int GetCurrentUserLanguageCode(IPluginContext pluginContext)
    {
      var langCode = pluginContext.OrgCtx.UserSettingsSet
        .Where(r => r.SystemUserId == pluginContext.ExecContext.InitiatingUserId)
        .Select(r => r.UILanguageId)
        .FirstOrDefault();
      const int EnglishLangCode = 1033;
      return langCode ?? EnglishLangCode;
    }
  }
}
