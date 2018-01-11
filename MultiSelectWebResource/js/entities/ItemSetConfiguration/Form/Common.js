/// <reference path="../../../js-vsdoc/Xrm.Page.js" />
var AdvancedMultiSelect = window.AdvancedMultiSelect || { __namespace: true };
AdvancedMultiSelect.ItemSetConfiguration = AdvancedMultiSelect.ItemSetConfiguration || { __namespace: true };
AdvancedMultiSelect.ItemSetConfiguration.Common = function () {
  var addControlWizardFormId = "b5fa4dfb-e897-45cf-abed-39a9b34a6e1a";
  var getIsAddControlWizard = function () {
    var currentForm = window.Xrm.Page.ui.formSelector.getCurrentItem();
    return currentForm.getId().toLowerCase() === addControlWizardFormId;

  };

  return {
    GetIsAddControlWizard: getIsAddControlWizard
  };
}();
