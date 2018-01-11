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
  var queryBuilderIframeName = "IFRAME_QueryBuilder";
  var saveBeforeActionWarningNotificationId = "SaveBeforeActionWarning";
  var dataLoadingIsInProgress = false, publishingEntityIsInProgress = false;

  var p, fu, af, xl;

  var entityList, actionList, validPrefixList;

  var ctrls = {
    entityName: null,
    itemSetName: null,
    relationshipName: null,
    savingAttributeName: null,
    labelAttributeName: null,
    tooltipAttributeName: null,
    saveAction: null,
    fetchXml: null,
    useQueryBuilder: null,
    queryBuilderIframe: null,
    description: null,
    createNewSavingAttribute: null,
    newSavingAttributeName: null,
    newSavingAttributeDisplayName: null,
    newSavingAttributeLength: null,
    autoprocessItemStatus: null,
    autoprocessItemStatusAttributeName: null,
    handleSecurityPrivileges: null
};

  var attrs = {
    entityName: null,
    itemSetName: null,
    relationshipName: null,
    savingAttributeName: null,
    itemSetEntityName: null,
    labelAttributeName: null,
    tooltipAttributeName: null,
    saveAction: null,
    fetchXml: null,
    useQueryBuilder: null,
    createNewSavingAttribute: null,
    newSavingAttributeName: null,
    newSavingAttributeDisplayName: null,
    newSavingAttributeLength: null,
    autoprocessItemStatus: null,
    autoprocessItemStatusAttributeName: null,
    publishRequired: null
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
    ctrls.relationshipName = p.getControl("pavelkh_relationshipname");
    ctrls.savingAttributeName = p.getControl("pavelkh_dummysavingfield");
    ctrls.labelAttributeName = p.getControl("pavelkh_labelattributename");
    ctrls.tooltipAttributeName = p.getControl("pavelkh_tooltipattributename");
    ctrls.saveAction = p.getControl("pavelkh_savechangeshandler");
    ctrls.fetchXml = p.getControl("pavelkh_fetchxml");
    ctrls.useQueryBuilder = p.getControl("pavelkh_usequerybuilder");
    ctrls.queryBuilderIframe = p.getControl(queryBuilderIframeName);
    ctrls.description = p.getControl("pavelkh_description");
    ctrls.createNewSavingAttribute = p.getControl("pavelkh_createnewdummysavingattribute");
    ctrls.newSavingAttributeName = p.getControl("pavelkh_newdummysavingfield");
    ctrls.newSavingAttributeDisplayName = p.getControl("pavelkh_newdummysavingfielddisplayname");
    ctrls.newSavingAttributeLength = p.getControl("pavelkh_newdummysavingattributelength");
    ctrls.autoprocessItemStatus = p.getControl("pavelkh_autoprocessitemstatus");
    ctrls.autoprocessItemStatusAttributeName = p.getControl("pavelkh_autoprocessitemstatusattributename");
    ctrls.handleSecurityPrivileges = p.getControl("pavelkh_handlesecurityprivileges");
    
    attrs.entityName = ctrls.entityName.getAttribute();
    attrs.itemSetName = ctrls.itemSetName.getAttribute();
    attrs.relationshipName = ctrls.relationshipName.getAttribute();
    attrs.savingAttributeName = ctrls.savingAttributeName.getAttribute();
    attrs.itemSetEntityName = p.getAttribute("pavelkh_itemsetentityname");
    attrs.labelAttributeName = ctrls.labelAttributeName.getAttribute();
    attrs.tooltipAttributeName = ctrls.tooltipAttributeName.getAttribute();
    attrs.saveAction = ctrls.saveAction.getAttribute();
    attrs.fetchXml = ctrls.fetchXml.getAttribute();
    attrs.useQueryBuilder = ctrls.useQueryBuilder.getAttribute();
    attrs.createNewSavingAttribute = ctrls.createNewSavingAttribute.getAttribute();
    attrs.newSavingAttributeName = ctrls.newSavingAttributeName.getAttribute();
    attrs.newSavingAttributeDisplayName = ctrls.newSavingAttributeDisplayName.getAttribute();
    attrs.newSavingAttributeLength = ctrls.newSavingAttributeLength.getAttribute();
    attrs.autoprocessItemStatus = ctrls.autoprocessItemStatus.getAttribute();
    attrs.autoprocessItemStatusAttributeName = ctrls.autoprocessItemStatusAttributeName.getAttribute();
    attrs.publishRequired = p.getAttribute("pavelkh_publishrequired");
  };

  var excludeControlFieldsFromSubmit = function() {
    attrs.useQueryBuilder.setSubmitMode("never");
  };

  var getRecordId = function() {
    return Xrm.Page.data.entity.getId();
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

  var getAction = function (uniqueName, ignoreCase) {
    if (!uniqueName) {
      return null;
    }

    uniqueName = uniqueName.trim();
    ignoreCase = ignoreCase || false;
    if (ignoreCase) {
      uniqueName = uniqueName.toLowerCase();
    }

    for (var i = 0; i < actionList.length; i++) {
      var select = ignoreCase ? (actionList[i].UniqueName.toLowerCase() === uniqueName) : (actionList[i].UniqueName === uniqueName);
      if (select) {
        return actionList[i];
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

  var getEntityPotentialStatusAttributes = function (entityLogicalName, ignoreCase) {
    var entity = getEntity(entityLogicalName, ignoreCase);
    if (!entity) {
      return [];
    }

    return entity.PotentialStatusAttributes;
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

  var getEntityAutoprocessItemStatusAttribute = function (entityLogicalName, attributeLogicalName, ignoreAttributeNameCase) {
    var attributes = getEntityPotentialStatusAttributes(entityLogicalName, false);
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

  var startDataLoadingNotification = function () {
    dataLoadingIsInProgress = true;
    fu.ShowWaitingNotification("Loading metadata", getDataLoadingIsInProgress, 200, "Loading metadata, please wait");
  };

  var stopDataLoadingNotification = function () {
    dataLoadingIsInProgress = false;
  };

  var getPublishingEntityIsInProgress = function () {
    return publishingEntityIsInProgress;
  };

  var startPublishingEntityNotification = function() {
    publishingEntityIsInProgress = true;
    fu.ShowWaitingNotification("Loading metadata", getPublishingEntityIsInProgress, 200, "Publishing form changes (after Item Set Name change), please wait");
  };

  var stopPublishingEntityNotification = function () {
    publishingEntityIsInProgress = false;
  };

  var getWebResourceUrl = function(webResourceName) {
    try {
      return window.Mscrm.CrmUri.create(String.format("$webresource:{0}", webResourceName)).toString();
    } catch (e) {
      return "";
    }
  };

  var showPublishingLoadError = function (error) {
    var baseMessage = "An error occurred while publishing form changes.";
    p.ui.setFormNotification(baseMessage, "ERROR", "MetadataLoadError");
    Xrm.Utility.alertDialog(baseMessage + "\nDetails:\n" + error.message);
  };

  var showMetadataLoadError = function (error) {
    var baseMessage = "An error occurred while getting metadata.";
    p.ui.setFormNotification(baseMessage, "ERROR", "MetadataLoadError");
    Xrm.Utility.alertDialog(baseMessage + "\nDetails:\n" + error.message);
  };

  var showSaveBeforeActionWarning = function(show, text) {
    if (show) {
      p.ui.setFormNotification(text, "ERROR", saveBeforeActionWarningNotificationId);
      return;
    }

    p.ui.clearFormNotification(saveBeforeActionWarningNotificationId);
  };

  var loadEntityList = function () {
    var dfd = xrmjQuery.Deferred();
    var query = "pavelkh_ItemSetConfigurationGetEntities";
    CrmWebApiFacade.ExecuteAction(query).then(function (result) {
      entityList = JSON.parse(result["EntityList"]);
      actionList = JSON.parse(result["ActionList"]);
      validPrefixList = JSON.parse(result["ValidPublishPrefixList"]);
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
      var createNew = attrs.createNewSavingAttribute.getValue();
      if (createNew) {
        return true;
      }

      var record = getEntitySavingAttribute(attrs.entityName.getValue(), value);
      return !!record && record.LogicalName === value;
    });
  }

  var validateNewAttrSchemaName = function () {
    var notificationId = "ValidateNewAttrSchemaName";
    var ctrl = ctrls.newSavingAttributeName;
    var createNewAttr = attrs.createNewSavingAttribute.getValue();
    if (!createNewAttr) {
      ctrl.clearNotification(notificationId);
      return true;
    }

    var value = attrs.newSavingAttributeName.getValue();
    if (!value) {
      ctrl.clearNotification(notificationId);
      return true;
    }

    for (var i = 0; i < validPrefixList.length; i++) {
      if (value.startsWith(validPrefixList[i])) {
        ctrl.clearNotification(notificationId);
        return true;
      }
    }

    ctrl.setNotification("The name should start with one of the following prefix: " + validPrefixList.join(), notificationId);
    return false;
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

      xrmjQuery.parseXML(fetchXml);
      ctrl.clearNotification(notificationId);
      return true;
    } catch (e) {
      processInvalidValue();
      return false;
    }
  };

  var validateSaveAction = function () {
    return fu.ValidateAutocomplete(ctrls.saveAction, function (value) {
      var record = getAction(value);
      return !!record && record.UniqueName === value;
    }, " (Or, leave the field empty.)");
  };

  var validateLabelAttributeName = function () {
    return fu.ValidateAutocomplete(ctrls.labelAttributeName, function (value) {
      var record = getEntityLabelAttribute(attrs.itemSetEntityName.getValue(), value);
      return !!record && record.LogicalName === value;
    });
  };

  var validateTooltimpAttributeName = function () {
    return fu.ValidateAutocomplete(ctrls.tooltipAttributeName, function (value) {
      var record = getEntityLabelAttribute(attrs.itemSetEntityName.getValue(), value);
      return !!record && record.LogicalName === value;
    });
  }

  var validateAutoprocessItemStatusAttributeName = function () {
    return fu.ValidateAutocomplete(ctrls.autoprocessItemStatusAttributeName, function (value) {
      var record = getEntityAutoprocessItemStatusAttribute(attrs.itemSetEntityName.getValue(), value);
      return !!record && record.LogicalName === value;
    });
  };

  var onFetchXmlChange = function() {
    validateFetchXml();
  };

  var setNewSavingAttributeDefaultParameters = function () {
    var createNew = attrs.createNewSavingAttribute.getValue();
    if (!createNew) {
      return;
    };

    try {
      var result = validPrefixList[0];
      var itemSetName = attrs.itemSetName.getValue();
      if (itemSetName) {
        var suffix = "_dummyfield";
        result += itemSetName.replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
        var maxNameLength = 45;
        if ((result.length + suffix.length) > maxNameLength) {
          result = result.substring(0, (maxNameLength - suffix.length)) + suffix;
        }
      }

      attrs.newSavingAttributeName.setValue(result);
      if (!attrs.newSavingAttributeLength.getValue()) {
        attrs.newSavingAttributeLength.setValue(4000);
      }

      var prefix = "[SYSTEM][MultiSelect]";
      var displayName = prefix + itemSetName.replace(/[^a-zA-Z0-9]/g, '');
      var maxDisplayNameLength = 50;
      if (displayName.length > maxDisplayNameLength) {
        displayName = displayName.substring(0, maxDisplayNameLength);
      }
      
      attrs.newSavingAttributeDisplayName.setValue(displayName);
    } catch (e) {
    } 
  };

  var onCreateNewDummyAttributeChange = function() {
    var createNew = attrs.createNewSavingAttribute.getValue();
    ctrls.savingAttributeName.setVisible(!createNew);
    attrs.savingAttributeName.setRequiredLevel(createNew ? "none" : "required");

    ctrls.newSavingAttributeName.setVisible(createNew);
    ctrls.newSavingAttributeDisplayName.setVisible(createNew);
    ctrls.newSavingAttributeLength.setVisible(createNew);

    var requiredLevel = createNew ? "required" : "none";
    attrs.newSavingAttributeName.setRequiredLevel(requiredLevel);
    attrs.newSavingAttributeDisplayName.setRequiredLevel(requiredLevel);
    attrs.newSavingAttributeLength.setRequiredLevel(requiredLevel);

    setNewSavingAttributeDefaultParameters();
    
    validateSavingAttributeName();
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

    var tooltipAttributeName = attrs.tooltipAttributeName.getValue();
    var addTooltip = tooltipAttributeName && tooltipAttributeName.trim() && tooltipAttributeName.trim() !== labelAttributeName;
    var fetchXml = "" +
      "<fetch no-lock='true' distinct='false'>" +
      "  <entity name='" + entityName + "'>" +
      "    <attribute name='" + labelAttributeName + "'/>" +
      (!!addTooltip ? "<attribute name='" + tooltipAttributeName + "'/>" : "") +
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

      entityNode.children.each(function() {
        var el = xrmjQuery(this);
        if (el.nodeName === "attribute") el.remove();
      });

      var labelAttrNode = xrmjQuery("<attribute>", entityNode);
      labelAttrNode.attr("name", labelAttributeName);
      var tooltipAttributeName = attrs.tooltipAttributeName.getValue();
      if (tooltipAttributeName) {
        var tooltipAttrNode = xrmjQuery("<attribute>", entityNode);
        tooltipAttrNode.attr("name", tooltipAttributeName);
      }

      return AdvancedMultiSelect.XmlUtils.XmlToString(fxml);
    } catch (e) {
      return buildBaseFetchXml();
    } 
  };

  var onLabelAttributeNameChange = function () {
    var valid = validateLabelAttributeName();
    ctrls.fetchXml.setDisabled(!valid);
    ctrls.useQueryBuilder.setDisabled(!valid);
    var useQueryBuilder = attrs.useQueryBuilder.getValue();
    if (useQueryBuilder) {
      attrs.useQueryBuilder.setValue(false);
      onQueryEditModeChange();
    }

    if (valid) {
      var fetchXml = AdvancedMultiSelect.XmlUtils.FormatXml(getAdjustedFetchXml());
      attrs.fetchXml.setValue(fetchXml);
      validateFetchXml();
    }
  }

  var onTooltipAttributeNameChange = function() {
    var valid = validateTooltimpAttributeName();
    ctrls.fetchXml.setDisabled(!valid);
    ctrls.useQueryBuilder.setDisabled(!valid);
    var useQueryBuilder = attrs.useQueryBuilder.getValue();
    if (useQueryBuilder) {
      attrs.useQueryBuilder.setValue(false);
      onQueryEditModeChange();
    }

    if (valid) {
      var fetchXml = AdvancedMultiSelect.XmlUtils.FormatXml(getAdjustedFetchXml());
      attrs.fetchXml.setValue(fetchXml);
      validateFetchXml();
    }
  };

  var onAutoprocessItemStatusAttributeNameChange = function () {
    validateAutoprocessItemStatusAttributeName();
  }

  var onRelationshipNameChange = function () {
    validateRelationshipName();
    var relationship = getEntityRelationship(attrs.entityName.getValue(), attrs.relationshipName.getValue());
    var itemSetEntityName = relationship ? relationship.ItemSetEntity : "";
    attrs.itemSetEntityName.setValue(itemSetEntityName);
    onLabelAttributeNameChange();
  };

  var onDummySavingAttributeNameChange = function () {
    validateSavingAttributeName();
  };

  var onNewAttributeSchemaNameChange = function () {
    validateNewAttrSchemaName();
  };

  var onEntityNameChange = function (ctx) {
    validateEntityName();
    onRelationshipNameChange();
    onDummySavingAttributeNameChange(ctx);
  };

  var onItemSetNameChange = function() {
    setNewSavingAttributeDefaultParameters();
  };

  var onSaveActionChange = function () {
    validateSaveAction();
  };

  var onEntityNameKeyPress = function() {
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

  var onSaveActionKeyPress = function () {
    fu.ShowAutocomplete(ctrls.saveAction, actionList, function (userInput, recordIndex) {
      var record = actionList[recordIndex];
      var uniqueName = record.UniqueName;
      var displayName = record.Name;
      var icon = "/_imgs/ico_16_4703.png";
      var addItem =
        userInput.length === 0 || uniqueName.indexOf(userInput) >= 0 || displayName.indexOf(userInput) >= 0;
      if (addItem) {
        return { id: recordIndex, fields: [uniqueName, displayName], icon: icon };
      }

      return null;
    });
  };

  var onRelationshipNameKeyPress = function () {
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

  var onSavingAttributeNameKeyPress = function () {
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

  var onLabelAttributeNameKeyPress = function () {
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

  var onTooltipAttributeNameKeyPress = function() {
    var entityLogicalName = attrs.itemSetEntityName.getValue();
    var attributes = getEntityAttributes(entityLogicalName);
    fu.ShowAutocomplete(ctrls.tooltipAttributeName, attributes, function (userInput, recordIndex) {
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

  var onAutoprocessItemStatusKeyPress = function () {
    var entityLogicalName = attrs.itemSetEntityName.getValue();
    var attributes = getEntityPotentialStatusAttributes(entityLogicalName);
    fu.ShowAutocomplete(ctrls.autoprocessItemStatusAttributeName, attributes, function (userInput, recordIndex) {
      var record = attributes[recordIndex];
      var logicalName = record.LogicalName;
      var displayName = record.DisplayName;
      var icon = "/_imgs/ico_18_attributes.gif";
      var addItem =
        userInput.length === 0 || logicalName.indexOf(userInput) >= 0 || displayName.indexOf(userInput) >= 0;
      if (addItem) {
        var isCustom = record.IsCustomAttribute ? "Custom" : "Out-of-box";
        return { id: recordIndex, fields: [logicalName, displayName, isCustom], icon: icon };
      }

      return null;
    });
  };

  var updateFetchXmlByQueryBuilder = function () {
    var useQueryBuilder = attrs.useQueryBuilder.getValue();
    if (!useQueryBuilder) {
      return;
    }

    try {
      var fetchXml = af.GetAdvFindControl(queryBuilderIframeName).get_fetchXml();
      if (fetchXml.indexOf("no-lock") === -1) {
        fetchXml = fetchXml.replace("<fetch", "<fetch no-lock=\"true\" ");
      }

      attrs.fetchXml.setValue(xl.FormatXml(fetchXml));
      validateFetchXml();
    } catch (e) {
      Xrm.Page.Utility.alertDialog("An error occured while updating Fetch Xml. Please specify Fetch Xml manually.");
    } 
  };

  var initOnKeyPressHandlers = function() {
    ctrls.entityName.addOnKeyPress(onEntityNameKeyPress);
    ctrls.relationshipName.addOnKeyPress(onRelationshipNameKeyPress);
    ctrls.savingAttributeName.addOnKeyPress(onSavingAttributeNameKeyPress);
    ctrls.labelAttributeName.addOnKeyPress(onLabelAttributeNameKeyPress);
    ctrls.tooltipAttributeName.addOnKeyPress(onTooltipAttributeNameKeyPress);
    ctrls.saveAction.addOnKeyPress(onSaveActionKeyPress);
    ctrls.autoprocessItemStatusAttributeName.addOnKeyPress(onAutoprocessItemStatusKeyPress);
  };

  var getIsCreateMode = function () {
    return p.ui.getFormType() === AdvancedMultiSelect.FormTypes.FORM_TYPE_CREATE;
  }

  var lockUnlockOnLoad = function (lock) {
    var createMode = getIsCreateMode();
    var lockKeyAttributes = !createMode || lock;
    ctrls.entityName.setDisabled(lockKeyAttributes);
    ctrls.itemSetName.setDisabled(lock);
    ctrls.relationshipName.setDisabled(lock);
    ctrls.savingAttributeName.setDisabled(lockKeyAttributes);
    ctrls.labelAttributeName.setDisabled(lock);
    ctrls.tooltipAttributeName.setDisabled(lock);
    ctrls.saveAction.setDisabled(lock);
    ctrls.description.setDisabled(lock);

    ctrls.createNewSavingAttribute.setDisabled(lockKeyAttributes);
    ctrls.newSavingAttributeName.setDisabled(lockKeyAttributes);
    ctrls.newSavingAttributeDisplayName.setDisabled(lockKeyAttributes);
    ctrls.newSavingAttributeLength.setDisabled(lockKeyAttributes);

    ctrls.createNewSavingAttribute.setVisible(createMode);

    ctrls.autoprocessItemStatus.setDisabled(lock);
    ctrls.autoprocessItemStatusAttributeName.setDisabled(lock);
    ctrls.handleSecurityPrivileges.setDisabled(lock);
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
      fu.GetInputElemByControl(ctrls.tooltipAttributeName).onfocus = function () { onTooltipAttributeNameKeyPress(); }
      fu.GetInputElemByControl(ctrls.saveAction).onfocus = function () { onSaveActionKeyPress(); }
      fu.GetInputElemByControl(ctrls.autoprocessItemStatusAttributeName).onfocus = function () { onAutoprocessItemStatusKeyPress(); }
    } catch (e) {
    } 
  };

  var validateOnLoad = function () {
    var createMode = getIsCreateMode();
    if (!createMode) {
      var valid = validateEntityName() && validateRelationshipName() && validateSavingAttributeName() && validateLabelAttributeName();
      if (valid) {
        ctrls.useQueryBuilder.setDisabled(false);
        ctrls.fetchXml.setDisabled(false);
      }
    }
  };

  var createUpdateFetchXmlButton = function() {
    try {
      var buttonId = "updateFetchXmlButton";
      if (xrmjQuery("#" + buttonId, parent.document).length !== 0) return;
      var button = xrmjQuery("<button/>", {
          text: "Update Fetch Xml",
          id: buttonId,
          "class": "ms-crm-Button",
          click: updateFetchXmlByQueryBuilder
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
    var createMode = getIsCreateMode();
    if (createMode) {
      ctrls.entityName.setFocus();
    }
  };

  var alineCreateDummySavingAttribute = function () {
    var createMode = getIsCreateMode();
    attrs.createNewSavingAttribute.setValue(createMode);
    onCreateNewDummyAttributeChange();
  };

  var setItemStatusAttributeVisibility = function (trySetDefaulValue) {
    var show = !!attrs.autoprocessItemStatus.getValue();
    ctrls.autoprocessItemStatusAttributeName.setVisible(show);
    attrs.autoprocessItemStatusAttributeName.setRequiredLevel(show ? "required" : "none");;
    if (trySetDefaulValue) {
      var itemSetEntityName = attrs.itemSetEntityName.getValue();
      var attribute = getEntityAutoprocessItemStatusAttribute(itemSetEntityName, "statecode", true);
      if (attribute) {
        attrs.autoprocessItemStatusAttributeName.setValue(attribute.LogicalName);
      }
    }
  };

  var publishAfterRenaming = function () {
    var dfd = xrmjQuery.Deferred();
    var createMode = getIsCreateMode();
    if (createMode) {
      dfd.resolve();
    } else {
      var publishRequired = attrs.publishRequired.getValue();
      if (!publishRequired) {
        dfd.resolve();
      } else {
        startPublishingEntityNotification();
        var id = getRecordId().replace("{", "").replace("}", "");
        var query = "pavelkh_advancedmultiselectitemsetconfigurations(" + id + ")/Microsoft.Dynamics.CRM.pavelkh_PublishAfterItemSetConfigurationRenaming";
        CrmWebApiFacade.ExecuteAction(query).then(function () {
          stopPublishingEntityNotification();
          dfd.resolve();
        }, function (error) {
          stopPublishingEntityNotification();
          dfd.reject(error);
        });
      }
    }

    return dfd.promise();
  };

  var initilize = function(initialLoad) {
    if (initialLoad) {
      initTypeAliases();
      initElements();
      excludeControlFieldsFromSubmit();
    }

    initialDataLoad().then(function () {
      if (initialLoad) {
        showAutoSaveNotification();
        initOnKeyPressHandlers();
        attachAutocompleteAttributesToOnFocus();
        setFocusForCreateMode();
        createUpdateFetchXmlButton();
      }

      alineCreateDummySavingAttribute();
      publishAfterRenaming().then(function() {
        lockUnlockOnLoad(false);
        validateOnLoad();
        setItemStatusAttributeVisibility();
        attrs.useQueryBuilder.setValue(false);
        try {
          onQueryEditModeChange();
        } catch (e) { };
      }, function () {
        showPublishingLoadError();
      });
    }, function (error) {
      showMetadataLoadError(error);
    });
  };

  var onLoad = function () {
    initilize(true);
  };

  var onSave = function (context) {
    AdvancedMultiSelect.FormUtils.PreventAutoSave(context);
  };

  var onSaveComplete = function() {
    lockUnlockOnLoad(true);
    initilize(false);
    showSaveBeforeActionWarning(false);
  };

  var modifiedOnChanged = function () {
    onSaveComplete();
  };

  var navigateToAddControlWizardForm = function () {
    var e = p.data.entity;
    if (e.getIsDirty()) {
      showSaveBeforeActionWarning(true, "Please save the form before you switch to the Add Control Wizard Form.");
      return;
    }

    var params = {};
    params["formid"] = "B5FA4DFB-E897-45CF-ABED-39A9B34A6E1A";
    params["parameter_validopen"] = true;
    var id = e.getId();
    Xrm.Utility.openEntityForm("pavelkh_advancedmultiselectitemsetconfiguration", id, params);
  };

  var onAutoprocessItemStatusChange = function () {
    setItemStatusAttributeVisibility(true);
  };

  var closeForm = function () {
    Xrm.Page.ui.close();
  };

  return {
    OnFormLoad: onLoad,
    OnFormSave: onSave,
    OnEntityNameChange: onEntityNameChange,
    OnItemSetNameChange: onItemSetNameChange,
    OnRelationshipNameChange: onRelationshipNameChange,
    OnDummySavingAttributeNameChange: onDummySavingAttributeNameChange,
    OnFetchXmlChange: onFetchXmlChange,
    OnLabelAttributeNameChange: onLabelAttributeNameChange,
    OnTooltipAttributeNameChange: onTooltipAttributeNameChange,
    OnQueryEditModeChange: onQueryEditModeChange,
    OnSaveActionChange: onSaveActionChange,
    OnCreateNewDummyAttributeChange: onCreateNewDummyAttributeChange,
    OnNewAttributeSchemaNameChange: onNewAttributeSchemaNameChange,
    ModifiedOnChanged: modifiedOnChanged,
    NavigateToAddControlWizardForm: navigateToAddControlWizardForm,
    OnAutoprocessItemStatusChange: onAutoprocessItemStatusChange,
    OnAutoprocessItemStatusAttributeNameChange: onAutoprocessItemStatusAttributeNameChange,
    CloseForm: closeForm
  };
}();
