/// <reference path="../../../js-vsdoc/Xrm.Page.js" />
/// <reference path="../../../libraries/jquery.js" />
/// <reference path="../../../libraries/knockout.js" />
/// <reference path="../../../CrmWebApiFacade.js" />
var AdvancedMultiSelect = window.AdvancedMultiSelect || { __namespace: true };
AdvancedMultiSelect.ItemSetConfiguration = AdvancedMultiSelect.ItemSetConfiguration || { __namespace: true };
AdvancedMultiSelect.ItemSetConfiguration.Import = function () {
  var viewModel = function() {
    var fileContent;
    var self = this;
    var onClickAttached = false;
    self.IsReadyForImport = ko.observable(false);
    self.IsImportInProgress = ko.observable(false);
    self.ErrorMessage = ko.observable("");
    self.FileUpload = function (data, e) {
      self.IsReadyForImport(false);
      var file = e.target.files[0];
      var reader = new FileReader();
      reader.onloadend = function () {
        fileContent = reader.result;
        self.IsReadyForImport(true);
        if (!onClickAttached) {
          onClickAttached = true;
          e.target.onclick = self.OnClick;
        }
      }

      if (file) {
        reader.readAsText(file);
      }
    };

    self.OnClick = function (e) {
      e.target.value = "";
    };

    self.Import = function () {
      if (!fileContent) {
        alert("There is no content found in the file.");
        return;
      }

      var query = "pavelkh_ItemSetConfigurationImport";
      var dataParams = {
        "Configuration": fileContent
      };

      self.IsImportInProgress(true);
      CrmWebApiFacade.ExecuteAction(query, dataParams).then(function (result) {
        self.IsImportInProgress(false);
        var error = result["Errors"];
        self.ErrorMessage(error);
        try {
          if (error) {
            alert("Error(s) occured while importing configuration. Please review the details.");
            window.opener.location.reload();
          } else {
            alert("Import completed successfully.");
            window.opener.location.reload();
            window.close();
          }
        } catch (e) {}
      }, function (error) {
        self.IsImportInProgress(false);
        alert("" + "\nDetails: " + error.message);
      });
    };

    self.Close = function() {
      try {
        window.close();
      } catch (e) { }
    };
  };

  return {
    ViewModel: viewModel
  };
}();
