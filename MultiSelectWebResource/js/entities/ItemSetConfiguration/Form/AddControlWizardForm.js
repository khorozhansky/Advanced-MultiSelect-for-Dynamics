/// <reference path="../../../js-vsdoc/Xrm.Page.js" />
/// <reference path="../../../FormTypes.js" />
/// <reference path="../../../libraries/jquery.js" />
/// <reference path="../../../CrmWebApiFacade.js" />
/// <reference path="../../../FormUtils.js" />
/// <reference path="../../../AdvFindUtils.js" />
/// <reference path="../../../XmlUtils.js" />
var AdvancedMultiSelect = window.AdvancedMultiSelect || { __namespace: true };
AdvancedMultiSelect.ItemSetConfiguration = AdvancedMultiSelect.ItemSetConfiguration || { __namespace: true };
AdvancedMultiSelect.ItemSetConfiguration.Form = function () {
  var dataLoadingIsInProgress = false;
  var addingControlInProgress = false;

  var p, fu, af, xl;

  var formList, templateList;

  var newSectionLocationTypes = {
    BeforeSection: 10,
    AfterSection: 20,
    FirstOnTab: 30
  };

  var ctrls = {
    entityName: null,
    itemSetName: null,
    savingAttributeName: null,

    form: null,
    tabName: null,
    sectionName: null,
    sectionLabel: null,
    showSectionLabel: null,
    showSectionLine: null,
    sectionLocation: null,
    nearSectionName: null,
    template: null,
    numberOfRows: null
  };

  var attrs = {
    entityName: null,
    itemSetName: null,
    savingAttributeName: null,

    form: null,
    tabName: null,
    sectionName: null,
    sectionLabel: null,
    showSectionLabel: null,
    showSectionLine: null,
    sectionLocation: null,
    nearSectionName: null,
    template: null,
    numberOfRows: null,
    newSectionName: null
};

  var initTypeAliases = function() {
    p = Xrm.Page;
    fu = AdvancedMultiSelect.FormUtils;
    af = AdvancedMultiSelect.AdvFindUtils;
    xl = AdvancedMultiSelect.XmlUtils;
  };

  var initElements = function () {
    ctrls.entityName = p.getControl("pavelkh_entityname");
    ctrls.itemSetName = p.getControl("pavelkh_itemsetname");
    ctrls.savingAttributeName = p.getControl("pavelkh_dummysavingfield");

    ctrls.form = p.getControl("pavelkh_addtoform");
    ctrls.tabName = p.getControl("pavelkh_addtotab");
    ctrls.sectionName = p.getControl("pavelkh_newsectionname");
    ctrls.sectionLabel = p.getControl("pavelkh_sectionlabel");
    ctrls.showSectionLabel = p.getControl("pavelkh_showsectionlabel");
    ctrls.showSectionLine = p.getControl("pavelkh_showsectionline");
    ctrls.sectionLocation = p.getControl("pavelkh_newsectionlocation");
    ctrls.nearSectionName = p.getControl("pavelkh_addnearsection");
    ctrls.template = p.getControl("pavelkh_webresource");
    ctrls.numberOfRows = p.getControl("pavelkh_numberofrows");

    attrs.entityName = ctrls.entityName.getAttribute();
    attrs.itemSetName = ctrls.itemSetName.getAttribute();
    attrs.savingAttributeName = ctrls.savingAttributeName.getAttribute();

    attrs.form = ctrls.form.getAttribute();
    attrs.tabName = ctrls.tabName.getAttribute();
    attrs.sectionName = ctrls.sectionName.getAttribute();
    attrs.sectionLabel = ctrls.sectionLabel.getAttribute();
    attrs.showSectionLabel = ctrls.showSectionLabel.getAttribute();
    attrs.showSectionLine = ctrls.showSectionLine.getAttribute();
    attrs.sectionLocation = ctrls.sectionLocation.getAttribute();
    attrs.nearSectionName = ctrls.nearSectionName.getAttribute();
    attrs.template = ctrls.template.getAttribute();
    attrs.numberOfRows = ctrls.numberOfRows.getAttribute();
    attrs.newSectionName = p.getAttribute("pavelkh_newsectionname");

    attrs.form.setSubmitMode("never");
    attrs.tabName.setSubmitMode("never");
    attrs.sectionName.setSubmitMode("never");
    attrs.sectionLabel.setSubmitMode("never");
    attrs.showSectionLabel.setSubmitMode("never");
    attrs.showSectionLine.setSubmitMode("never");
    attrs.sectionLocation.setSubmitMode("never");
    attrs.nearSectionName.setSubmitMode("never");
    attrs.template.setSubmitMode("never");
    attrs.numberOfRows.setSubmitMode("never");
  };

  var getTemplate = function(name) {
    if (!name) {
      return null;
    }

    for (var i = 0; i < templateList.length; i++) {
      if (templateList[i].Name === name) {
        return templateList[i];
      }
    }

    return null;
  };

  var getForm = function (fullFormName) {
    if (!fullFormName) {
      return null;
    }

    fullFormName = fullFormName.trim().toLowerCase();

    for (var i = 0; i < formList.length; i++) {
      if (formList[i].FullName.trim().toLowerCase() === fullFormName) {
        return formList[i];
      }
    }

    return null;
  }

  var getTabs = function (fullFormName, ignoreCase) {
    var form = getForm(fullFormName, ignoreCase);
    if (!form) {
      return [];
    }

    return form.Tabs;
  }

  var getTab = function (fullFormName, tabName) {
    var tabs = getTabs(fullFormName);
    if (tabs.length === 0) {
      return null;
    }

    if (!tabName) {
      return null;
    }

    tabName = tabName.trim().toLowerCase();
    for (var i = 0; i < tabs.length; i++) {
      if (tabs[i].Name.trim().toLowerCase() === tabName) {
        return tabs[i];
      }
    }

    return null;
  }

  var getSections = function (fullFormName, tabName) {
    var entity = getTab(fullFormName, tabName);
    if (!entity) {
      return [];
    }

    return entity.Sections;
  };

  var getSection = function (fullFormName, tabName, sectionName) {
    var sections = getSections(fullFormName, tabName);
    if (!sections) {
      return null;
    }

    sectionName = sectionName.trim();
    for (var i = 0; i < sections.length; i++) {
      if (sections[i].Name.trim() === sectionName) {
        return sections[i];
      }
    }

    return null;
  };

  var lockAttributes = function (lock) {
    ctrls.form.setDisabled(lock);
    ctrls.tabName.setDisabled(lock);
    ctrls.sectionName.setDisabled(lock);
    ctrls.sectionLabel.setDisabled(lock);
    ctrls.showSectionLabel.setDisabled(lock);
    ctrls.showSectionLine.setDisabled(lock);
    ctrls.sectionLocation.setDisabled(lock);
    ctrls.nearSectionName.setDisabled(lock);
    ctrls.template.setDisabled(lock);
    ctrls.numberOfRows.setDisabled(lock);
  };

  var showAddControlError = function (show, message) {
    var addingControlActionErrorNotificationId = "AddingControlError";
    if (show) {
      p.ui.setFormNotification(message, "ERROR", addingControlActionErrorNotificationId);
    } else {
      p.ui.clearFormNotification(addingControlActionErrorNotificationId);
    }
  };

  var showAddControlCompleteNotification = function (show, message) {
    var addingControlActionErrorNotificationId = "AddingControlComplete";
    if (show) {
      p.ui.setFormNotification(message, "INFO", addingControlActionErrorNotificationId);
    } else {
      p.ui.clearFormNotification(addingControlActionErrorNotificationId);
    }
  };

  var getDataLoadingIsInProgress = function () {
    return dataLoadingIsInProgress;
  };

  var startDataLoadingNotification = function() {
    dataLoadingIsInProgress = true;
    fu.ShowWaitingNotification("Loading metadata", getDataLoadingIsInProgress, 200, "Loading metadata, please wait");
  };

  var stopDataLoadingNotification = function () {
    dataLoadingIsInProgress = false;
  };

  var getAddingControlIsInProgress = function () {
    return addingControlInProgress;
  };

  var startAddingControlNotification = function () {
    addingControlInProgress = true;
    lockAttributes(true);
    fu.ShowWaitingNotification("Adding Control", getAddingControlIsInProgress, 200, "MultiSelect control is being added on the form, please wait");
  };

  var stopAddingControlNotification = function () {
    lockAttributes(false);
    addingControlInProgress = false;
  };

  var showMetadataLoadError = function (error) {
    var baseMessage = "An error occurred while getting metadata.";
    p.ui.setFormNotification(baseMessage, "ERROR", "MetadataLoadError");
    Xrm.Utility.alertDialog(baseMessage + "\nDetails:\n" + error.message);
  };

  var loadFormList = function () {
    var dfd = xrmjQuery.Deferred();
    var query = "pavelkh_ItemSetConfigurationGetAddControlWizardData";
    var dataParams = {
      "EntityLogicalName": attrs.entityName.getValue()
    };

    CrmWebApiFacade.ExecuteAction(query, dataParams).then(function (result) {
      formList = JSON.parse(result["FormList"]);
      templateList = JSON.parse(result["TemplateList"]);
      dfd.resolve();
    }, function (error) {
      dfd.reject(error);
    });

    return dfd.promise();
  };

  var validateForm = function() {
    return fu.ValidateAutocomplete(ctrls.form, function(value) {
      var record = getForm(value);
      return !!record && record.FullName.toLowerCase() === value.toLowerCase();
    });
  };

  var validateTabName = function () {
    return fu.ValidateAutocomplete(ctrls.tabName, function (value) {
      var record = getTab(attrs.form.getValue(), value);
      return !!record && record.Name.toLowerCase() === value.toLowerCase();
    });
  };

  var validateNewSectionName = function () {
    return !!attrs.sectionName.getValue();
  };

  var validateNewSectionLabel = function () {
    return !!attrs.sectionLabel.getValue();
  };

  var validateShowSectionLabel = function () {
    return attrs.showSectionLabel.getValue() != null;
  };

  var validateShowSectionLine = function () {
    return attrs.showSectionLine.getValue() != null;
  };

  var validateNearSectionName = function () {
    return fu.ValidateAutocomplete(ctrls.nearSectionName, function (value) {
      var record = getSection(attrs.form.getValue(), attrs.tabName.getValue(), value);
      return !!record && record.Name.toLowerCase() === value.toLowerCase();
    });
  }

  var validateTemplate = function () {
    return fu.ValidateAutocomplete(ctrls.template, function (value) {
      var record = getTemplate(value);
      return !!record;
    });
  }

  var validateNumberOfRows = function () {
    var numberOfRows = attrs.numberOfRows.getValue();
    return numberOfRows != null && numberOfRows > 0 && numberOfRows < 51;
  };

  var onSectionLocationChange = function () {
    var sectionLocation = attrs.sectionLocation.getValue();
    var showNearSectionAttribute =
      sectionLocation === newSectionLocationTypes.AfterSection
      || sectionLocation === newSectionLocationTypes.BeforeSection;

    attrs.nearSectionName.setRequiredLevel(showNearSectionAttribute ? "required" : "none");
    if (!showNearSectionAttribute) {
      if (!attrs.nearSectionName.getValue()) {
        attrs.nearSectionName.setValue(null);
      }
    }

    ctrls.nearSectionName.setVisible(showNearSectionAttribute);
  };

  var onTemplateChange = function() {
    validateTemplate();
  }

  var onNearSectionChange = function () {
    validateNearSectionName();
  }

  var onTabChange = function () {
    validateTabName();
    onNearSectionChange();
  }

  var onFormChange = function () {
    validateForm();
    onTabChange();
  }

  var onTemplateKeyPress = function () {
    fu.ShowAutocomplete(ctrls.template, templateList, function (userInput, recordIndex) {
      var record = templateList[recordIndex];
      var name = record.Name;
      var displayName = record.DisplayName;
      var icon = "/_imgs/ico_16_9333.gif";
      var addItem =
        userInput.length === 0 || name.indexOf(userInput.toLowerCase()) >= 0;
      if (addItem) {
        return { id: recordIndex, fields: [name, displayName], icon: icon };
      }

      return null;
    });
  };

  var onFormKeyPress = function() {
    fu.ShowAutocomplete(ctrls.form, formList, function (userInput, recordIndex) {
      var record = formList[recordIndex];
      var fullName = record.FullName.toLowerCase();
      var description = record.Description;
      var icon = "/_imgs/ico_16_forms.png";
      var addItem =
        userInput.length === 0 || fullName.indexOf(userInput.toLowerCase()) >= 0;
      if (addItem) {
        return { id: recordIndex, fields: [fullName, description], icon: icon };
      }

      return null;
    });
  };

  var onTabKeyPress = function () {
    var fullFormName = attrs.form.getValue();
    var tabs = getTabs(fullFormName);
    fu.ShowAutocomplete(ctrls.tabName, tabs, function (userInput, recordIndex) {
      var record = tabs[recordIndex];
      var name = record.Name;
      var displayName = record.DisplayName;
      var addItem =
        userInput.length === 0 || name.toLowerCase().indexOf(userInput.toLowerCase()) >= 0 || displayName.toLowerCase().indexOf(userInput.toLowerCase()) >= 0;
      if (addItem) {
        return { id: recordIndex, fields: [name, displayName]};
      }

      return null;
    });
  };

  var onNearSectionNameKeyPress = function () {
    var fullFormName = attrs.form.getValue();
    var tabName = attrs.tabName.getValue();
    var sections = getSections(fullFormName, tabName);
    fu.ShowAutocomplete(ctrls.nearSectionName, sections, function (userInput, recordIndex) {
      var record = sections[recordIndex];
      var name = record.Name;
      var displayName = record.DisplayName;
      var addItem =
        userInput.length === 0 || name.toLowerCase().indexOf(userInput.toLowerCase()) >= 0 || displayName.toLowerCase().indexOf(userInput.toLowerCase()) >= 0;
      if (addItem) {
        return { id: recordIndex, fields: [name, displayName] };
      }

      return null;
    });
  };

  var initOnKeyPressHandlers = function() {
    ctrls.form.addOnKeyPress(onFormKeyPress);
    ctrls.tabName.addOnKeyPress(onTabKeyPress);
    ctrls.nearSectionName.addOnKeyPress(onNearSectionNameKeyPress);
    ctrls.template.addOnKeyPress(onTemplateKeyPress);
  };

  var initialDataLoad = function () {
    startDataLoadingNotification();
    var dfd = xrmjQuery.Deferred();
    loadFormList().then(function () {
      stopDataLoadingNotification();
      dfd.resolve();
    }, function (error) {
      stopDataLoadingNotification();
      dfd.reject(error);
    });

    return dfd.promise();
  }

  var showAutoSaveNotification = function() {
    p.ui.setFormNotification("Save is disabled for this form.", "INFO", "autosaveinfo");
  };

  var attachAutocompleteAttributesToOnFocus = function() {
    try {
      fu.GetInputElemByControl(ctrls.form).onfocus = function () { onFormKeyPress(); }
      fu.GetInputElemByControl(ctrls.tabName).onfocus = function () { onTabKeyPress(); }
      fu.GetInputElemByControl(ctrls.nearSectionName).onfocus = function () { onNearSectionNameKeyPress(); }
      fu.GetInputElemByControl(ctrls.template).onfocus = function () { onTemplateKeyPress(); }
    } catch (e) {
    } 
  };

  var setFocusOnLoad = function () {
    ctrls.form.setFocus();
  };

  var setRequiredOnLoad = function() {
    attrs.form.setRequiredLevel("required");
    attrs.tabName.setRequiredLevel("required");
    attrs.sectionName.setRequiredLevel("required");
    attrs.sectionLabel.setRequiredLevel("required");
    attrs.showSectionLabel.setRequiredLevel("required");
    attrs.showSectionLine.setRequiredLevel("required");
    attrs.sectionLocation.setRequiredLevel("required");
    attrs.nearSectionName.setRequiredLevel("required");
    attrs.template.setRequiredLevel("required");
    attrs.numberOfRows.setRequiredLevel("required");
  };

  var setDefaultValues = function() {
    attrs.sectionLocation.setValue(newSectionLocationTypes.BeforeSection);
    attrs.numberOfRows.setValue(5);
  };

  var navigateToMainForm = function () {
    var formSelector = p.ui.formSelector;
    var forms = formSelector.items.get();
    var mainFormId = "45281719-08EE-40E5-ADAD-0F264D571548";
    for (var i = 0; i < forms.length; i++) {
      var f = forms[i];
      if (f.getId().toUpperCase() === mainFormId) {
        f.navigate();
        return;
      }
    }
  };

  var validateFormOpen = function () {
    var params = p.context.getQueryStringParameters();
    if (!params["parameter_validopen"]) {
      navigateToMainForm();
      return false;
    }

    return true;
  };

  var onLoad = function () {
    initTypeAliases();
    initElements();

    if (!validateFormOpen()) {
      return;
    }

    setDefaultValues();

    initialDataLoad().then(function () {
      showAutoSaveNotification();
      setRequiredOnLoad();
      lockAttributes(false);
      initOnKeyPressHandlers();
      attachAutocompleteAttributesToOnFocus();
      setFocusOnLoad();
    }, function (error) {
      showMetadataLoadError(error);
    });
  };

  var executeAddControlAction = function () {
    var dfd = xrmjQuery.Deferred();
    var query = "pavelkh_ItemSetConfigurationAddControlOnForm";
    var form = getForm(attrs.form.getValue());
    var dataParams = {
      "EntityLogicalName": attrs.entityName.getValue(),
      "ItemSetName": attrs.itemSetName.getValue(),
      "DummySavingAttributeName": attrs.savingAttributeName.getValue(),
      "FormId": form.Id,
      "TabName": attrs.tabName.getValue(),
      "NewSectionLocation": attrs.sectionLocation.getValue(),
      "NearSectionName": attrs.nearSectionName.getValue(),
      "WebResource": attrs.template.getValue(),
      "NewSectionName": attrs.sectionName.getValue(),
      "NewSectionLabel": attrs.sectionLabel.getValue(),
      "ShowLabel": attrs.showSectionLabel.getValue(),
      "ShowLine": attrs.showSectionLine.getValue(),
      "NumberOfRows": attrs.numberOfRows.getValue()
    };

    CrmWebApiFacade.ExecuteAction(query, dataParams).then(function () {
      dfd.resolve();
    }, function (error) {
      dfd.reject(error);
    });

    return dfd.promise();
  };

  var validateAddControlAction = function () {
    var valid = validateForm() && validateTabName()
      && validateNewSectionName() && validateNewSectionLabel()
      && validateShowSectionLabel() && validateShowSectionLine
      && validateNearSectionName() && validateTemplate()
      && validateNumberOfRows();

    return valid;
  };

  var addAdvancedMultiSelectControlOnForm = function () {
    if (getAddingControlIsInProgress()) {
      return false;
    }

    showAddControlError(false);
    showAddControlCompleteNotification(false);
    if (!validateAddControlAction()) {
      showAddControlError(true, "Please provide correct values for the required fields.");
      return false;
    }

    startAddingControlNotification();
    var dfd = xrmjQuery.Deferred();
    executeAddControlAction().then(function () {
      var message = "The MultiSelect control has been successfully added on the form. You can also add the control on another form as needed.";
      stopAddingControlNotification();
      showAddControlCompleteNotification(true, message);
      Xrm.Utility.alertDialog(message);
      dfd.resolve();
    }, function (error) {
      stopAddingControlNotification();
      dfd.reject(error);
      var message = "An error occured while adding the MultiSelect control to the form. Details: " + error.message;
      showAddControlError(true, message);
      Xrm.Utility.alertDialog(message);
    });

    return dfd.promise();
  };

  var onSave = function (context) {
    var eventArgs = context.getEventArgs();
    eventArgs.preventDefault();
    return false;
  };

  var alignNewSectionName = function () {
    var value = attrs.newSectionName.getValue();
    if (value == null) {
      return;
    }
    value = value.replace(/[^a-zA-Z0-9]/g, '');
    attrs.newSectionName.setValue(value);
  }

  var onNewSectionNameChange = function () {
    alignNewSectionName();
  };

  return {
    OnFormLoad: onLoad,
    OnFormSave: onSave,
    OnFormChange: onFormChange,
    OnTabChange: onTabChange,
    OnNewSectionNameChange: onNewSectionNameChange,
    OnSectionLocationChange: onSectionLocationChange,
    OnNearSectionChange: onNearSectionChange,
    OnTemplateChange: onTemplateChange,
    AddAdvancedMultiSelectControlOnForm: addAdvancedMultiSelectControlOnForm,
    NavigateToMainForm: navigateToMainForm
  };
}();
