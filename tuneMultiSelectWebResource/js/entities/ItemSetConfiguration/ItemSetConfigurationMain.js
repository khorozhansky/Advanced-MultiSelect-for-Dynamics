/// <reference path="../../FormTypes.js" />
/// <reference path="../../libraries/jquery.js" />
/// <reference path="../../CrmWebApiFacade.js" />
/// <reference path="../../FormUtils.js" />
/// <reference path="../../AdvFindUtils.js" />
/// <reference path="../../XmlUtils.js" />
var TuneXrm = window.TuneXrm || { __namespace: true };
TuneXrm.ItemSetConfiguration = TuneXrm.ItemSetConfiguration || { __namespace: true };
TuneXrm.ItemSetConfiguration.Form = function () {
  var queryBuilderIframeName = "IFRAME_QueryBuilder";
  var dataLoadingIsInProgress = false;

  var p, fu, af, xl;

  var entityList;

  var ctrls = {
    entityName: null,
    itemSetName: null,
    relationshipName: null,
    savingAttributeName: null,
    labelAttributeName: null,
    saveChangesHandler: null,
    fetchXml: null,
    useQueryBuilder: null,
    queryBuilderIframe: null
};

  var attrs = {
    entityName: null,
    itemSetName: null,
    relationshipName: null,
    savingAttributeName: null,
    itemSetEntityName: null,
    labelAttributeName: null,
    saveChangesHandler: null,
    fetchXml: null,
    useQueryBuilder: null
  };

  var initTypeAliases = function() {
    p = Xrm.Page;
    fu = TuneXrm.FormUtils;
    af = TuneXrm.AdvFindUtils;
    xl = TuneXrm.XmlUtils;
  };

  var initElements = function () {
    ctrls.entityName = p.getControl("tunexrm_entityname");
    ctrls.itemSetName = p.getControl("tunexrm_itemsetname");
    ctrls.relationshipName = p.getControl("tunexrm_relationshipname");
    ctrls.savingAttributeName = p.getControl("tunexrm_dummysavingfield");
    ctrls.labelAttributeName = p.getControl("tunexrm_labelattributename");
    ctrls.saveChangesHandler = p.getControl("tunexrm_savechangeshandler");
    ctrls.fetchXml = p.getControl("tunexrm_fetchxml");
    ctrls.useQueryBuilder = p.getControl("tunexrm_usequerybuilder");

    attrs.entityName = ctrls.entityName.getAttribute();
    attrs.itemSetName = ctrls.itemSetName.getAttribute();
    attrs.relationshipName = ctrls.relationshipName.getAttribute();
    attrs.savingAttributeName = ctrls.savingAttributeName.getAttribute();
    attrs.itemSetEntityName = p.getAttribute("tunexrm_itemsetentityname");
    attrs.labelAttributeName = ctrls.labelAttributeName.getAttribute();
    attrs.saveChangesHandler = ctrls.saveChangesHandler.getAttribute();
    attrs.fetchXml = ctrls.fetchXml.getAttribute();
    attrs.useQueryBuilder = ctrls.useQueryBuilder.getAttribute();
    ctrls.queryBuilderIframe = p.getControl(queryBuilderIframeName);

    attrs.useQueryBuilder.setSubmitMode("never");
  };

  var getEntity = function (entityLogicalName, ignoreCase) {
    if (!entityLogicalName) {
      return null;
    }

    entityLogicalName = entityLogicalName.trim();
    ignoreCase = ignoreCase || false;
    if (ignoreCase) {
      entityLogicalName = entityLogicalName.toLowerCase();
    }
    
    for (var i = 0; i < entityList.length; i++) {
      if (entityList[i].LogicalName === entityLogicalName) {
        return entityList[i];
      }
    }

    return null;
  }

  var getEntityRelationships = function (entityLogicalName, ignoreCase) {
    var entity = getEntity(entityLogicalName, ignoreCase);
    if (!entity) {
      return [];
    }

    return entity.Relationships;
  };

  var getEntitySavingAttributes = function (entityLogicalName, ignoreCase) {
    var result = [];
    var entity = getEntity(entityLogicalName, ignoreCase);
    if (!entity) {
      return result;
    }

    for (var i = 0; i < entity.Attributes.length; i++) {
      var attr = entity.Attributes[i];
      var savingAttribute = attr.IsCustomAttribute && attr.MaxLength > 499;
      if (savingAttribute) {
        result.push(attr);
      }
    }

    return result;
  };

  var getEntityAttributes = function (entityLogicalName, ignoreCase) {
    var entity = getEntity(entityLogicalName, ignoreCase);
    if (!entity) {
      return [];
    }

    return entity.Attributes;
  };

  var getEntityRelationship = function (entityLogicalName, relationshipName, ignoreRelationshipNameCase) {
    var relationships = getEntityRelationships(entityLogicalName, false);
    if (!relationships) {
      return null;
    }

    for (var i = 0; i < relationships.length; i++) {
      var relationship = relationships[i].Relationship.trim();
      if (ignoreRelationshipNameCase) {
        relationshipName = relationshipName.toLowerCase();
        relationship = relationship.toLowerCase();
      }

      if (relationship === relationshipName) {
        return relationships[i];
      }
    }

    return null;
  };

  var getEntitySavingAttribute = function (entityLogicalName, savingAttributeLogicalName, ignoreSavingAttributeNameCase) {
    var savingAttributes = getEntitySavingAttributes(entityLogicalName, false);
    if (!savingAttributes) {
      return null;
    }

    for (var i = 0; i < savingAttributes.length; i++) {
      var attribute = savingAttributes[i].LogicalName.trim();
      if (ignoreSavingAttributeNameCase) {
        savingAttributeLogicalName = savingAttributeLogicalName.toLowerCase();
      }

      if (attribute === savingAttributeLogicalName) {
        return savingAttributes[i];
      }
    }

    return null;
  };

  var getEntityLabelAttribute = function (entityLogicalName, attributeLogicalName, ignoreAttributeNameCase) {
    var attributes = getEntityAttributes(entityLogicalName, false);
    if (!attributes) {
      return null;
    }

    for (var i = 0; i < attributes.length; i++) {
      var attribute = attributes[i].LogicalName.trim();
      if (ignoreAttributeNameCase) {
        attributeLogicalName = attributeLogicalName.toLowerCase();
      }

      if (attribute === attributeLogicalName) {
        return attributes[i];
      }
    }

    return null;
  };

  var getDataLoadingIsInProgress = function () {
    return dataLoadingIsInProgress;
  };

  var startDataLoadingNotification = function() {
    dataLoadingIsInProgress = true;
    fu.ShowLoadingNotification(getDataLoadingIsInProgress, 200, "Loading metadata, please wait");
  };

  var stopDataLoadingNotification = function () {
    dataLoadingIsInProgress = false;
  };

  var getWebResourceUrl = function(webResourceName) {
    try {
      return window.Mscrm.CrmUri.create(String.format("$webresource:{0}", webResourceName)).toString();
    } catch (e) {
      return "";
    }
  };

  var showMetadataLoadError = function (error) {
    var baseMessage = "An error occurred while getting metadata.";
    p.ui.setFormNotification(baseMessage, "ERROR", "MetadataLoadError");
    Xrm.Utility.alertDialog(baseMessage + "\nDetails:\n" + error.message);
  };

  var loadEntityList = function () {
    var dfd = xrmjQuery.Deferred();
    var query = "tunexrm_ItemSetConfigurationGetEntities";
    CrmWebApiFacade.ExecuteAction(query).then(function (result) {
      entityList = JSON.parse(result["EntityList"]);
      dfd.resolve();
    }, function (error) {
      dfd.reject(error);
    });

    return dfd.promise();
  };

  var validateEntityName = function() {
    return fu.ValidateAutocomplete(ctrls.entityName, function(value) {
      var record = getEntity(value);
      return !!record && record.LogicalName === value;
    });
  };

  var validateRelationshipName = function () {
    return fu.ValidateAutocomplete(ctrls.relationshipName, function (value) {
      var record = getEntityRelationship(attrs.entityName.getValue(), value);
      return !!record && record.Relationship === value;
    });
  };

  var validateSavingAttributeName = function () {
    return fu.ValidateAutocomplete(ctrls.savingAttributeName, function (value) {
      var record = getEntitySavingAttribute(attrs.entityName.getValue(), value);
      return !!record && record.LogicalName === value;
    });
  }

  var validateFetchXml = function () {
    var notificationId = "FetchXml Validation";
    var ctrl = ctrls.fetchXml;
    var processInvalidValue = function() {
      ctrl.setNotification("Please provide a valid value for Fetch Xml", notificationId);
    }

    try {
      var fetchXml = attrs.fetchXml.getValue();
      if (!fetchXml || fetchXml.trim().length === 0) {
        processInvalidValue();
        return false;
      }

      var xmlParsed = xrmjQuery.parseXML(fetchXml);
      ctrl.clearNotification(notificationId);
      return true;
    } catch (e) {
      processInvalidValue();
      return false;
    }
  };

  var validateLabelAttributeName = function (ctx) {
    return fu.ValidateAutocomplete(ctrls.labelAttributeName, function (value) {
      var record = getEntityLabelAttribute(attrs.itemSetEntityName.getValue(), value);
      return !!record && record.LogicalName === value;
    });
  };

  var onFetchXmlChange = function(ctx) {
    var valid = validateFetchXml();
  };

  var onQueryEditModeChange = function () {
    var itemSetEntityName = attrs.itemSetEntityName.getValue();
    if (!itemSetEntityName) {
      attrs.useQueryBuilder.setValue(false);
      ctrls.fetchXml.setDisabled(false);
    }

    var useQueryBuilder = attrs.useQueryBuilder.getValue();
    ctrls.fetchXml.setDisabled(useQueryBuilder);
    if (!useQueryBuilder) {
      af.ClearSrc(queryBuilderIframeName);
      ctrls.queryBuilderIframe.setVisible(false);
      return;
    }

    ctrls.queryBuilderIframe.setVisible(true);
    var fetchXmlText = attrs.fetchXml.getValue();
    var onQueryBuilderLoadErrorHandler = function () {
      attrs.useQueryBuilder.setValue(false);
      onQueryEditModeChange();
    };

    af.SetSrc(queryBuilderIframeName, itemSetEntityName, fetchXmlText, onQueryBuilderLoadErrorHandler);
  };

  var buildBaseFetchXml = function() {
    var entityName = attrs.itemSetEntityName.getValue();
    var labelAttributeName = attrs.labelAttributeName.getValue();
    if (!entityName || !labelAttributeName) {
      return null;
    }

    var fetchXml = "" +
      "<fetch no-lock='true' distinct='false'>" +
      "  <entity name='" + entityName + "'>" +
      "    <attribute name='" + labelAttributeName + "'/>" +
      "	   <order attribute='" + labelAttributeName + "' descending='false'/>" +
      "  </entity>" +
      "</fetch>";

    return fetchXml;
  };

  var getAdjustedFetchXml = function() {
    var entityName = attrs.itemSetEntityName.getValue();
    var labelAttributeName = attrs.labelAttributeName.getValue();
    if (!entityName || !labelAttributeName) {
      return null;
    }

    var fetchXml = attrs.fetchXml.getValue();

    if (!fetchXml || fetchXml.trim().length === 0) {
      return buildBaseFetchXml();
    }

    try {
      var fxml = xrmjQuery.parseXML(fetchXml);
      var entityNode = fxml.find("entity");
      if (entityNode.attr("name") !== entityName) {
        return buildBaseFetchXml();
      }

      entityNode.children.each(function(idx) {
        var el = xrmjQuery(this);
        if (el.nodeName === "attribute") el.remove();
      });

      var labelAttrNode = xrmjQuery("<attribute>", entityNode);
      labelAttrNode.attr("name", labelAttributeName);
      return  TuneXrm.XmlUtils.XmlToString(fxml);
    } catch (e) {
      return buildBaseFetchXml();
    } 
  };

  var onLabelAttributeNameChange = function (ctx) {
    var valid = validateLabelAttributeName();
    ctrls.fetchXml.setDisabled(!valid);
    ctrls.useQueryBuilder.setDisabled(!valid);
    var useQueryBuilder = attrs.useQueryBuilder.getValue();
    if (useQueryBuilder) {
      attrs.useQueryBuilder.setValue(false);
      onQueryEditModeChange();
    }

    if (valid) {
      var fetchXml = TuneXrm.XmlUtils.FormatXml(getAdjustedFetchXml());
      attrs.fetchXml.setValue(fetchXml);
      validateFetchXml();
    }
  }

  var onRelationshipNameChange = function (ctx) {
    validateRelationshipName();
    var relationship = getEntityRelationship(attrs.entityName.getValue(), attrs.relationshipName.getValue());
    var itemSetEntityName = relationship ? relationship.ItemSetEntity : "";
    attrs.itemSetEntityName.setValue(itemSetEntityName);
    onLabelAttributeNameChange();
  };

  var onDummySavingAttributeNameChange = function (ctx) {
    validateSavingAttributeName();
  };

  var onEntityNameChange = function (ctx) {
    validateEntityName();
    onRelationshipNameChange();
    onDummySavingAttributeNameChange(ctx);
  };

  var onEntityNameKeyPress = function(ctx) {
    fu.ShowAutocomplete(ctrls.entityName, entityList, function (userInput, recordIndex) {
      var record = entityList[recordIndex];
      var logicalName = record.LogicalName;
      var displayName = record.DisplayName;
      var icon = record.Icon;
      icon = icon.indexOf("/WebResource/") === 0 ? getWebResourceUrl(icon) : icon;
      icon = !!icon ? icon : "/_imgs/ico_16_customEntity.gif";
      var addItem =
        userInput.length === 0 || logicalName.indexOf(userInput) >= 0 || displayName.indexOf(userInput) >= 0;
      if (addItem) {
        return { id: recordIndex, fields: [logicalName, displayName], icon: icon };
      }

      return null;
    });
  };

  var onRelationshipNameKeyPress = function (ctx) {
    var entityLogicalName = attrs.entityName.getValue();
    var relationships = getEntityRelationships(entityLogicalName);
    fu.ShowAutocomplete(ctrls.relationshipName, relationships, function (userInput, recordIndex) {
      var record = relationships[recordIndex];
      var relation = record.Relationship;
      var itemSetEntity = record.ItemSetEntity;
      var icon = "/_imgs/ico_16_relationshipsN2N.gif";
      var addItem =
        userInput.length === 0 || relation.toLowerCase().indexOf(userInput) >= 0 || itemSetEntity.toLowerCase().indexOf(userInput) >= 0;
      if (addItem) {
        return { id: recordIndex, fields: [relation, "=>" + itemSetEntity], icon: icon };
      }

      return null;
    });
  };

  var onSavingAttributeNameKeyPress = function (ctx) {
    var entityLogicalName = attrs.entityName.getValue();
    var attributes = getEntitySavingAttributes(entityLogicalName);
    fu.ShowAutocomplete(ctrls.savingAttributeName, attributes, function (userInput, recordIndex) {
      var record = attributes[recordIndex];
      var logicalName = record.LogicalName;
      var displayName = record.DisplayName;
      var icon = "/_imgs/ico_18_attributes.gif";
      var addItem =
        userInput.length === 0 || logicalName.indexOf(userInput) >= 0 || displayName.indexOf(userInput) >= 0;
      if (addItem) {
        var maxLength = "MaxLength: " + record.MaxLength;
        return { id: recordIndex, fields: [logicalName, displayName, maxLength], icon: icon };
      }

      return null;
    });
  };

  var onLabelAttributeNameKeyPress = function (ctx) {
    var entityLogicalName = attrs.itemSetEntityName.getValue();
    var attributes = getEntityAttributes(entityLogicalName);
    fu.ShowAutocomplete(ctrls.labelAttributeName, attributes, function (userInput, recordIndex) {
      var record = attributes[recordIndex];
      var logicalName = record.LogicalName;
      var displayName = record.DisplayName;
      var icon = "/_imgs/ico_18_attributes.gif";
      var addItem =
        userInput.length === 0 || logicalName.indexOf(userInput) >= 0 || displayName.indexOf(userInput) >= 0;
      if (addItem) {
        var maxLength = "MaxLength: " + record.MaxLength;
        var isCustom = record.IsCustomAttribute ? "Custom" : "Out-of-box";
        return { id: recordIndex, fields: [logicalName, displayName, maxLength, isCustom], icon: icon };
      }

      return null;
    });
  };

  var updateFetchXmlByQueryBuilder = function() {
    var useQueryBuilder = attrs.useQueryBuilder.getValue();
    if (!useQueryBuilder) {
      return;
    }

    try {
      var fetchXml = af.GetAdvFindControl(queryBuilderIframeName).get_fetchXml();
      attrs.fetchXml.setValue(xl.FormatXml(fetchXml));
      validateFetchXml();
    } catch (e) {
      p.Utility.alertDialog("An error occured while updating Fetch Xml. Please specify Fetch Xml manually.");
    } 
  };

  var initOnKeyPressHandlers = function() {
    ctrls.entityName.addOnKeyPress(onEntityNameKeyPress);
    ctrls.relationshipName.addOnKeyPress(onRelationshipNameKeyPress);
    ctrls.savingAttributeName.addOnKeyPress(onSavingAttributeNameKeyPress);
    ctrls.labelAttributeName.addOnKeyPress(onLabelAttributeNameKeyPress);
  };

  var unlockOnLoad = function () {
    ctrls.entityName.setDisabled(false);
    ctrls.itemSetName.setDisabled(false);
    ctrls.relationshipName.setDisabled(false);
    ctrls.savingAttributeName.setDisabled(false);
    ctrls.labelAttributeName.setDisabled(false);
    ctrls.saveChangesHandler.setDisabled(false);
  };

  var initialDataLoad = function () {
    startDataLoadingNotification();
    var dfd = xrmjQuery.Deferred();
    loadEntityList().then(function () {
      stopDataLoadingNotification();
      dfd.resolve();
    }, function (error) {
      stopDataLoadingNotification();
      dfd.reject(error);
    });

    return dfd.promise();
  }

  var showAutoSaveNotification = function() {
    Xrm.Page.ui.setFormNotification("Auto-save is disabled for this form.", "INFO", "autosaveinfo");
  };

  var attachAutocompleteAttributesToOnFocus = function() {
    try {
      fu.GetInputElemByControl(ctrls.entityName).onfocus = function () { onEntityNameKeyPress(); }
      fu.GetInputElemByControl(ctrls.relationshipName).onfocus = function () { onRelationshipNameKeyPress(); }
      fu.GetInputElemByControl(ctrls.savingAttributeName).onfocus = function () { onSavingAttributeNameKeyPress(); }
      fu.GetInputElemByControl(ctrls.labelAttributeName).onfocus = function () { onLabelAttributeNameKeyPress(); }
    } catch (e) {
    } 
  };

  var validateOnLoad = function () {
    var updateMode = p.ui.getFormType() === TuneXrm.FormTypes.FORM_TYPE_UPDATE;
    if (updateMode) {
      var valid = validateEntityName() && validateRelationshipName() && validateSavingAttributeName() && validateLabelAttributeName();
      if (valid) {
        ctrls.useQueryBuilder.setDisabled(false);
        ctrls.fetchXml.setDisabled(false);
      }
    }
  };

  var createUpdateFetchXmlButton = function() {
    try {
      var button = xrmjQuery("<button/>", {
          text: "Update Fetch Xml",
          id: "updateFetchXmlButton",
          "class": "ms-crm-Button",
          click: updateFetchXmlByQueryBuilder,
        }
      );

      button.attr('onmouseover', 'window.parent.Mscrm.ButtonUtils.hoverOn(this)');
      button.attr('onmouseout', 'window.parent.Mscrm.ButtonUtils.hoverOff(this)');
      button.css('margin-top', '5px').css('margin-bottom', '5px');
      button.insertBefore(xrmjQuery("#" + queryBuilderIframeName, parent.document));

    } catch (e) {
      ctrls.useQueryBuilder.setVisible(false);
      p.ui.setFormNotification("Query Builder Edit Mode does not work in this environment. You have to specify Fetch Xml Query manually.", "WARNING", "createUpdateFetchXmlButtonWarning");
    } 
  };

  var setFocusForCreateMode = function() {
    var createMode = p.ui.getFormType() === TuneXrm.FormTypes.FORM_TYPE_CREATE;
    if (createMode) {
      ctrls.entityName.setFocus();
    }
  };

  var onLoad = function () {
    initTypeAliases();
    initElements();
    attrs.useQueryBuilder.setValue(false);

    initialDataLoad().then(function () {
      showAutoSaveNotification();
      unlockOnLoad();
      initOnKeyPressHandlers();
      attachAutocompleteAttributesToOnFocus();
      validateOnLoad();
      setFocusForCreateMode();

      createUpdateFetchXmlButton();
    }, function (error) {
      showMetadataLoadError(error);
    });
  };

  var onSave = function (context) {
    TuneXrm.FormUtils.PreventAutoSave(context);
  };

  return {
    OnFormLoad: onLoad,
    OnFormSave: onSave,
    OnEntityNameChange: onEntityNameChange,
    OnRelationshipNameChange: onRelationshipNameChange,
    OnDummySavingAttributeNameChange: onDummySavingAttributeNameChange,
    OnFetchXmlChange: onFetchXmlChange,
    OnLabelAttributeNameChange: onLabelAttributeNameChange,
    OnQueryEditModeChange: onQueryEditModeChange
  };
}();
