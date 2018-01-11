namespace AdvancedMultiSelect.Utils
{
  using System;
  using System.ServiceModel;
  using System.Text;
  using Microsoft.Xrm.Sdk;

  public static class ExceptionHelper
  {
    public static InvalidPluginExecutionException BuildInvalidPluginExecutionException(Exception e, Type type, ITracingService tracingService)
    {
      if (tracingService != null)
      {
        tracingService.Trace(e.Message);
        tracingService.Trace(e.StackTrace);
      }

      var additionalInfo = new StringBuilder();

      var executionException = e as InvalidPluginExecutionException;
      if (executionException != null)
      {
        var exc = executionException;
        return exc;
      }

      var exception = e as FaultException<OrganizationServiceFault>;
      if (exception != null)
      {
        var exc = exception;
        additionalInfo.AppendFormat("\nTimestamp: {0}", exc.Detail.Timestamp);
        additionalInfo.AppendFormat("\nCode: {0}", exc.Detail.ErrorCode);
        additionalInfo.AppendFormat("\nMessage: {0}", exc.Detail.Message);
        additionalInfo.AppendFormat("\nTrace: {0}", exc.Detail.TraceText);
        if (exc.Detail.InnerFault != null)
        {
          additionalInfo.AppendFormat("\nInner Fault Message: {0}", exc.Detail.InnerFault.Message);
          additionalInfo.AppendFormat("\nInner Fault Trace: {0}", exc.Detail.InnerFault.TraceText);
        }
      }
      else
      {
        var timeoutException = e as TimeoutException;
        if (timeoutException != null)
        {
          var exc = timeoutException;
          additionalInfo.AppendFormat("\nMessage: {0}", exc.Message);
          additionalInfo.AppendFormat("\nTrace: {0}", exc.StackTrace);
          if (exc.InnerException != null)
          {
            additionalInfo.AppendFormat("\nInner Exception Message: {0}", exc.InnerException.Message);
            additionalInfo.AppendFormat("\nInner Exception Trace: {0}", exc.InnerException.StackTrace);
          }
        }
        else
        {
          additionalInfo.AppendFormat("\nMessage: {0}", e.Message);
          additionalInfo.AppendFormat("\nTrace: {0}", e.StackTrace);
          if (e.InnerException != null)
          {
            additionalInfo.AppendFormat("\nInner Exception Message: {0}", e.InnerException.Message);
            additionalInfo.AppendFormat("\nInner Exception Trace: {0}", e.InnerException.StackTrace);
            var exc = e.InnerException as FaultException<OrganizationServiceFault>;
            if (exc != null)
            {
              additionalInfo.AppendFormat("\nTimestamp: {0}", exc.Detail.Timestamp);
              additionalInfo.AppendFormat("\nCode: {0}", exc.Detail.ErrorCode);
              additionalInfo.AppendFormat("\nMessage: {0}", exc.Detail.Message);
              additionalInfo.AppendFormat("\nTrace: {0}", exc.Detail.TraceText);
            }
          }
        }
      }

      const string MessageFormatString =
        "An error occurred in the {0} plug-in. Exception: {1} Details: {2}";

      var message = string.Format(MessageFormatString, type, e.Message, additionalInfo);
      tracingService?.Trace(message);

      var result = new InvalidPluginExecutionException(message, e);
      return result;
    }
  }
}
