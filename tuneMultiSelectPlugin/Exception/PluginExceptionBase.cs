namespace TuneMultiSelect.Exception
{
  using System;

  /// <summary>
  /// Custom Plugin Exception Base
  /// </summary>
  public class PluginExceptionBase : ApplicationException
  {
    public PluginExceptionBase(string message, Exception innerException = null)
      : base(message, innerException)
    {
    }
  }
}
