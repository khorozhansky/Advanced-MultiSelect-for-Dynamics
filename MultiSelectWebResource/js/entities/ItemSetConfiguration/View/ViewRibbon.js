/// <reference path="../../../js-vsdoc/Xrm.Page.js" />
/// <reference path="../../../libraries/jquery.js" />
/// <reference path="../../../CrmWebApiFacade.js" />
var AdvancedMultiSelect = window.AdvancedMultiSelect || { __namespace: true };
AdvancedMultiSelect.ItemSetConfiguration = AdvancedMultiSelect.ItemSetConfiguration || { __namespace: true };
AdvancedMultiSelect.ItemSetConfiguration.View = function () {
  var exportConfiguration = function (selectedIds) {
    if (selectedIds.length === 0) {
      Xrm.Utility.alertDialog("Please select record(s) first.");
      return;
    }

    var getItemSetConfiguratoins = function () {
      var dfd = xrmjQuery.Deferred();
      var dataParams = { "SelectedIds": selectedIds.join(",") };
      CrmWebApiFacade.ExecuteAction("pavelkh_ItemSetConfigurationExport", dataParams).then(function (result) {
        var configuration = result["Configuration"];
        dfd.resolve(configuration);
      }, function (error) {
        dfd.reject(error);
      });

      return dfd.promise();
    };

    var saveConfigFile = function(content) {
      var blob = new Blob([content], { type: 'text/plain;charset=utf-8;' });
      var fileName = "AdvancedMultiSelect.cfg";
      if (navigator.msSaveBlob) { 
        navigator.msSaveBlob(blob, fileName);
      } else {
        var link = document.createElement("a");
        if (link.download !== undefined) {
          var url = URL.createObjectURL(blob);
          link.setAttribute("href", url);
          link.setAttribute("download", fileName);
          link.style.visibility = 'hidden';
          document.body.appendChild(link);
          link.click();
          document.body.removeChild(link);
        }
      }
    }
    
    getItemSetConfiguratoins().then(function(configuration) {
      saveConfigFile(configuration);
    }, function(error) {
      Xrm.Utility.alertDialog("An error occured while exporting configuration." + "\nDetails:\n" + error.message);
    });
  };

  var importConfiguration = function () {
    Xrm.Utility.openWebResource("pavelkh_/html/ImportConfigurations/import.html", null, 800, 450);
  };

  return {
    ExportConfiguration: exportConfiguration,
    ImportConfiguration: importConfiguration
};
}();
