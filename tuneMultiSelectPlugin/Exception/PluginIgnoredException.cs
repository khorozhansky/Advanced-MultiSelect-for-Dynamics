namespace TuneMultiSelect.Exception
{
  using System;

  public class PluginIgnoredException : PluginExceptionBase
  {
    public PluginIgnoredException(string message, Exception innerException = null)
      : base(message, innerException)
    {
    }
  }
}
