/// <reference path="../../../MultiSelectWebResource/js/libraries/jquery.js" />
/// <reference path="../../../MultiSelectWebResource/js/libraries/knockout.js" />
/// <reference path="../../../MultiSelectWebResource/js/CrmWebApiFacade.js" />
var AdvancedMultiSelect = window.AdvancedMultiSelect || { __namespace: true };
AdvancedMultiSelect.InstallDemoData = function () {
  var viewModel = function() {
    var self = this;
    self.Initialized = ko.observable(false);
    self.Installed = ko.observable(false);
    self.InProgress = ko.observable(true);
    var execAction = function(installDemoData) {
      self.InProgress(installDemoData || !self.Initialized());
      var query = "pavelkh_DemoInstallDemoData";
      var dataParams = {
        "InstallDemoData": installDemoData
      };

      CrmWebApiFacade.ExecuteAction(query, dataParams).then(function (result) {
        if (installDemoData) {
          alert("Sample data is installed.");
        }

        self.Initialized(true);
        self.InProgress(false);
        self.Installed(result["DemoDataExists"]);
      }, function (error) {
        self.InProgress(false);
        alert("An error occured." + "\nDetails: " + error.message);
      });
    };

    self.OnClick = function (e) {
      e.target.value = "";
    };

    self.Initialize = function() {
      execAction(false);
    };

    self.Install = function () {
      execAction(true);
    };
  };

  return {
    ViewModel: viewModel
  };
}();
