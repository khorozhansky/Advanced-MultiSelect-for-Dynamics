/// <reference path="libraries/jquery.js" />
window.CrmWebApiFacade = function () {
  var endpoint = '/api/data/v8.1/',
      globalContext = null,
      serverUrl = null;

  function getContext() {
    if (globalContext === null) {
      if (typeof window.GetGlobalContext !== "undefined") {
        globalContext = window.GetGlobalContext();
      } else {
        if (typeof window.Xrm !== "undefined") {
          globalContext = window.Xrm.Page.context;
        } else if (typeof window.parent.Xrm != "undefined") {
          globalContext = window.parent.Xrm.Page.context;
        } else {
          throw new Error("Context is not available.");
        }
      }
    }

    return globalContext;
  }

  function getServerUrl() {
    if (serverUrl === null) {
      var url,
        localserverUrl = window.location.protocol + "//" + window.location.host,
        context = getContext();

      if (context.getClientUrl !== undefined) {
        url = context.getClientUrl();
      } else if (context.isOutlookClient() && !context.isOutlookOnline()) {
        url = localserverUrl;
      } else {
        url = context.getServerUrl();
        url = url.replace(/^(http|https):\/\/([_a-zA-Z0-9\-\.]+)(:([0-9]{1,5}))?/, localserverUrl);
        url = url.replace(/\/$/, "");
      }

      serverUrl = url;
    }

    return serverUrl;
  }

  function getEndPointUrl() {
    return getServerUrl() + endpoint;
  }

  function executeActionRequest(query, data, async) {
    var url = getEndPointUrl() + query;
    async = (async === false) ? false : true;
    return xrmjQuery.ajax({
      type: 'POST',
      contentType: "application/json; charset=utf-8",
      datatype: "json",
      async: async,
      data: window.JSON.stringify(data),
      url: url,
      headers: {
        "Accept": "application/json",
        "Content-Type": "application/json; charset=utf-8",
        "OData-MaxVersion": "4.0",
        "OData-Version": "4.0"
      }
    });
  }

  var executeAction = function (query, data, async) {
    var dfd = xrmjQuery.Deferred();
    executeActionRequest(query, data, async).then(function(result) {
      dfd.resolve(result);
    }, function (jqXhr) {
      var errorResponse = JSON.parse(jqXhr.responseText);
      var error = { response: jqXhr, status: jqXhr.status, message: errorResponse.error.message };
      dfd.reject(error);
    });

    return dfd.promise();
  };

  return {
    ExecuteAction: executeAction,
    GetServerUrl: getServerUrl
  };
}();
 