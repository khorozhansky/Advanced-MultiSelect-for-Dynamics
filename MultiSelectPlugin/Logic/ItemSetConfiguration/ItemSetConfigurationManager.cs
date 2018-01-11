namespace AdvancedMultiSelect.Logic.ItemSetConfiguration
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text.RegularExpressions;

  using CrmProxy;
  using ItemSet;
  using Utils;

  using Microsoft.Crm.Sdk.Messages;
  using Microsoft.Xrm.Sdk;
  using Microsoft.Xrm.Sdk.Messages;
  using Microsoft.Xrm.Sdk.Metadata;
  using Microsoft.Xrm.Sdk.Metadata.Query;
  using Microsoft.Xrm.Sdk.Query;

  public class ItemSetConfigurationManager : ManagerBase<pavelkh_advancedmultiselectitemsetconfiguration>
  {
    public const string IntersectLinkedEntityAlias = "intersect_alias";
    
    public ItemSetConfigurationManager(PluginBase<pavelkh_advancedmultiselectitemsetconfiguration>.PluginContext pluginContext) : base(pluginContext)
    {
    }

    public void CreatePreValidationSync()
    {
      this.ProcessSave();
    }

    public void CreatePostOperationSync()
    {
      this.SyncDynamicPluginStepsOnCreate();
    }

    public void UpdatePreValidationSync()
    {
      this.ProcessSave();
    }

    public void UpdatePostOperationSync()
    {
      this.SyncDynamicPluginStepsOnUpdate();
      this.ProcessItemSetNameChange();
    }

    public void DeletePostOperationSync()
    {
      this.SyncDynamicPluginStepsOnDelete();
    }

    private static EntityReference GetSdkMessageFilter(CrmContext orgCtx, Guid messageId, string entityName)
    {
      var result =
        orgCtx.SdkMessageFilterSet
          .Where(
            r =>
              r.SdkMessageId != null && r.SdkMessageId.Id == messageId
              && r.PrimaryObjectTypeCode == entityName)
          .Select(r => r.SdkMessageFilterId)
          .FirstOrDefault();
      if (result == null)
      {
        throw new InvalidPluginExecutionException($"Sdk Message Filter is not found. MessageId: {messageId}, EntityName: {entityName}");
      }

      return new EntityReference(SdkMessage.EntityLogicalName, result.Value);
    }

    private void ProcessSave()
    {
      var itemSetConfig = this.AlignFieldValues();
      this.ValidateDuplicates(itemSetConfig);
      this.SetRelationshipDetailsAttributes(itemSetConfig);
      this.ValidateItemSetLabelAttributeAttributeName(itemSetConfig);
      this.ValidateItemSetTooltipAttributeName(itemSetConfig);
      this.ValidateFetchXmlConsistency(itemSetConfig);
      this.ValidateAction(itemSetConfig);
      this.ValidateAutoprocessItemSetStatusAttributeName(itemSetConfig);
      this.ProcessHandleSecurityPrivileges(itemSetConfig);

      this.ProcessDummySavingField(itemSetConfig);
    }

    private void ProcessDummySavingField(pavelkh_advancedmultiselectitemsetconfiguration itemSetConfig)
    {
      var pluginContext = this.PluginContext;
      var target = pluginContext.InputTarget;
      var createNew = itemSetConfig.pavelkh_CreateNewDummySavingAttribute ?? false;
      if (!createNew)
      {
        var parentCtx = pluginContext.ExecContext.ParentContext;
        var checkIfCreateRequired = parentCtx != null
                    && parentCtx.MessageName == MessageName.pavelkh_ItemSetConfigurationImport.ToString();
        if (checkIfCreateRequired)
        {
          var attributeMetadata = (StringAttributeMetadata)MetadataUtils.GetAttributeMetadata(
            pluginContext.Service,
            itemSetConfig.pavelkh_EntityName,
            itemSetConfig.pavelkh_DummySavingField,
            new[] { "LogicalName"});
          if (attributeMetadata == null)
          {
            createNew = true;
            itemSetConfig.pavelkh_NewDummySavingField = itemSetConfig.pavelkh_DummySavingField;
          }
        }
      }

      if (createNew)
      {
        var attrSchemaName = itemSetConfig.pavelkh_NewDummySavingField;
        var incorrectNewAttributeParams = string.IsNullOrWhiteSpace(attrSchemaName)
                                          || string.IsNullOrWhiteSpace(itemSetConfig.pavelkh_NewDummySavingFieldDisplayName)
                                          || itemSetConfig.pavelkh_NewDummySavingAttributeLength == null;
        if (incorrectNewAttributeParams)
        {
          throw new InvalidPluginExecutionException("You would like to create a new Dummy Saving Attribute, but didn't specify all required info for the new attribute correctly.");
        }

        var prefixes = ItemSetConfigurationActionManager.GetPrefixList(pluginContext.OrgCtxAsSystemUser);
        var prefixIsValid = prefixes.Any(r => attrSchemaName.StartsWith(r));
        if (!prefixIsValid)
        {
          throw new InvalidPluginExecutionException(
            $"You would like to create a new Dummy Saving Attribute. 'New Attribute Schema Name' should start with one of the following prefix: {string.Join(", ", prefixes)}.");
        }

        this.CreateNewDummySavingAttribute(itemSetConfig);
        target.pavelkh_DummySavingField = itemSetConfig.pavelkh_NewDummySavingField.ToLowerInvariant();
      }
      else
      {
        this.ValidateDummySavingFieldAttributeName(itemSetConfig);
      }

      target.pavelkh_CreateNewDummySavingAttribute = false;
      target.pavelkh_NewDummySavingField = null;
      target.pavelkh_NewDummySavingFieldDisplayName = null;
      target.pavelkh_NewDummySavingAttributeLength = null;
    }

    private void CreateNewDummySavingAttribute(pavelkh_advancedmultiselectitemsetconfiguration itemSetConfig)
    {
      const int LangCode = 1033;
      try
      {
        var schemaName = itemSetConfig.pavelkh_NewDummySavingField.Trim();
        var request = new CreateAttributeRequest
        {
          EntityName = itemSetConfig.pavelkh_EntityName,
          Attribute = new StringAttributeMetadata
          {
            SchemaName = schemaName,
            RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None),
            Format = StringFormat.Text,
            MaxLength = itemSetConfig.pavelkh_NewDummySavingAttributeLength,
            DisplayName = new Label(itemSetConfig.pavelkh_NewDummySavingFieldDisplayName, LangCode),
            Description = new Label("Dummy Saving Attribute for AdvancedMultiSelect.", LangCode),
            IsValidForAdvancedFind = new BooleanManagedProperty(false),
            IsAuditEnabled = new BooleanManagedProperty(false),
          }
        };

        var pluginContext = this.PluginContext;
        pluginContext.Trace($"Creating a new Dummy Saving Attribute ({schemaName})");
        var service = pluginContext.Service;
        service.Execute(request);
        MetadataUtils.PublishEntity(service, itemSetConfig.pavelkh_EntityName);
      }
      catch (Exception exc)
      {
        throw new InvalidPluginExecutionException(
          $"Error while creating a new field. Please check, perhaps a field with such name already exists.\n{exc.Message}");
      }
    }

    private void ValidateAction(pavelkh_advancedmultiselectitemsetconfiguration itemSetConfig)
    {
      var actionName = itemSetConfig.pavelkh_SaveChangesHandler;
      if (string.IsNullOrEmpty(actionName))
      {
        return;
      }

      var validActions = ItemSetConfigurationActionManager.GetActionList(this.PluginContext.OrgCtx);
      if (validActions.All(r => r.UniqueName != actionName))
      {
        throw new InvalidPluginExecutionException($"The '{actionName}' action is not a valid Save Action. Please see documentation regarding when and how to use Save Action.");
      }
    }

    private pavelkh_advancedmultiselectitemsetconfiguration AlignFieldValues()
    {
      var pluginContext = this.PluginContext;
      var targetExt = pluginContext.TargetExt;
      var target = targetExt.Target;
      var itemSetNameExt = targetExt.GetValue(() => target.pavelkh_ItemSetName);
      var itemSetConfig = new pavelkh_advancedmultiselectitemsetconfiguration
      {
        pavelkh_advancedmultiselectitemsetconfigurationId = target.Id,
        pavelkh_EntityName = targetExt.GetValue(() => target.pavelkh_EntityName).Value,
        pavelkh_ItemSetName = itemSetNameExt.Value,
        pavelkh_RelationshipName = targetExt.GetValue(() => target.pavelkh_RelationshipName).Value,
        pavelkh_DummySavingField = targetExt.GetValue(() => target.pavelkh_DummySavingField).Value,
        pavelkh_LabelAttributeName = targetExt.GetValue(() => target.pavelkh_LabelAttributeName).Value,
        pavelkh_TooltipAttributeName = targetExt.GetValue(() => target.pavelkh_TooltipAttributeName).Value,
        pavelkh_FetchXml = targetExt.GetValue(() => target.pavelkh_FetchXml).Value,
        pavelkh_SaveChangesHandler = targetExt.GetValue(() => target.pavelkh_SaveChangesHandler).Value,
        pavelkh_CreateNewDummySavingAttribute = targetExt.GetValue(() => target.pavelkh_CreateNewDummySavingAttribute).Value,
        pavelkh_NewDummySavingField = targetExt.GetValue(() => target.pavelkh_NewDummySavingField).Value,
        pavelkh_NewDummySavingFieldDisplayName = targetExt.GetValue(() => target.pavelkh_NewDummySavingFieldDisplayName).Value,
        pavelkh_NewDummySavingAttributeLength = targetExt.GetValue(() => target.pavelkh_NewDummySavingAttributeLength).Value,
        pavelkh_AutoProcessItemStatus = targetExt.GetValue(() => target.pavelkh_AutoProcessItemStatus).Value,
        pavelkh_AutoprocessItemStatusAttributeName = targetExt.GetValue(() => target.pavelkh_AutoprocessItemStatusAttributeName).Value,
        pavelkh_HandleSecurityPrivileges = targetExt.GetValue(() => target.pavelkh_HandleSecurityPrivileges).Value
      };

      var attributes = new List<string>
      {
        itemSetConfig.pavelkh_EntityName,
        itemSetConfig.pavelkh_ItemSetName,
        itemSetConfig.pavelkh_RelationshipName,
        itemSetConfig.pavelkh_LabelAttributeName,
        itemSetConfig.pavelkh_FetchXml
      };

      var createNewAttribute = itemSetConfig.pavelkh_CreateNewDummySavingAttribute ?? false;
      if (createNewAttribute)
      {
        attributes.Add(itemSetConfig.pavelkh_NewDummySavingField);
        attributes.Add(itemSetConfig.pavelkh_NewDummySavingFieldDisplayName);
        attributes.Add(itemSetConfig.pavelkh_NewDummySavingAttributeLength?.ToString());
      }
      else
      {
        attributes.Add(itemSetConfig.pavelkh_DummySavingField);
      }

      var autoProcessItemStatus = itemSetConfig.pavelkh_AutoProcessItemStatus ?? false;
      if (autoProcessItemStatus)
      {
        target.pavelkh_AutoprocessItemStatusAttributeName =
          itemSetConfig.pavelkh_AutoprocessItemStatusAttributeName =
            itemSetConfig.pavelkh_AutoprocessItemStatusAttributeName?.Trim().ToLowerInvariant();
      }
      
      if (attributes.Any(string.IsNullOrWhiteSpace))
      {
        throw new InvalidPluginExecutionException(
          $"{this.GetGenericItemSetSavingErrorMessage(itemSetConfig)} "
          + "Please specify all mandatory fields.");
      }

      target.pavelkh_EntityName = 
        itemSetConfig.pavelkh_EntityName = itemSetConfig.pavelkh_EntityName.Trim().ToLowerInvariant();
      target.pavelkh_ItemSetName =
        itemSetConfig.pavelkh_ItemSetName = this.AlignItemSetName(itemSetConfig.pavelkh_ItemSetName);
      target.pavelkh_RelationshipName =
        itemSetConfig.pavelkh_RelationshipName = itemSetConfig.pavelkh_RelationshipName.Trim();
      target.pavelkh_LabelAttributeName =
        itemSetConfig.pavelkh_LabelAttributeName = itemSetConfig.pavelkh_LabelAttributeName.Trim().ToLowerInvariant();
      if (!string.IsNullOrEmpty(target.pavelkh_TooltipAttributeName))
      {
        target.pavelkh_TooltipAttributeName =
          itemSetConfig.pavelkh_TooltipAttributeName = itemSetConfig.pavelkh_TooltipAttributeName.Trim().ToLowerInvariant();
      }

      target.pavelkh_FetchXml =
        itemSetConfig.pavelkh_FetchXml = itemSetConfig.pavelkh_FetchXml.Trim().ToLowerInvariant();
      if (!string.IsNullOrEmpty(target.pavelkh_SaveChangesHandler))
      {
        target.pavelkh_SaveChangesHandler =
          itemSetConfig.pavelkh_SaveChangesHandler = itemSetConfig.pavelkh_SaveChangesHandler.Trim();
      }

      if (createNewAttribute)
      {
        target.pavelkh_NewDummySavingField = 
          itemSetConfig.pavelkh_NewDummySavingField = itemSetConfig.pavelkh_NewDummySavingField.Trim().ToLowerInvariant();
        target.pavelkh_NewDummySavingFieldDisplayName =
          itemSetConfig.pavelkh_NewDummySavingFieldDisplayName = itemSetConfig.pavelkh_NewDummySavingFieldDisplayName.Trim();
      }
      else
      {
        target.pavelkh_DummySavingField =
          itemSetConfig.pavelkh_DummySavingField = itemSetConfig.pavelkh_DummySavingField.Trim().ToLowerInvariant();
      }

      return itemSetConfig;
    }

    private void ProcessItemSetNameChange()
    {
      var pluginContext = this.PluginContext;
      pluginContext.Trace("Processing Item Set Name Change...");
      var target = pluginContext.InputTarget;
      var targetExt = pluginContext.TargetExt;
      var itemSetNameExt = targetExt.GetValue(() => target.pavelkh_ItemSetName);
      var processItemSetNameChanged = itemSetNameExt.IsModified;
      if (!processItemSetNameChanged)
      {
        return;
      }

      var entityName = target.pavelkh_EntityName;
      var orgCtx = pluginContext.OrgCtx;
      var forms =
        orgCtx.SystemFormSet.Where(
          r =>
          r.Type != null 
          && r.Type.Value == ItemSetConfigurationActionManager.MainFormTypeCode 
          && r.ObjectTypeCode == entityName)
          .Select(r => new SystemForm
          {
            FormId = r.FormId,
            Name = r.Name,
            FormXml = r.FormXml
          }).ToList();

      const string WebResourceParamTemplate = "itemsetname=\"{0}\"";
      var oldParam = string.Format(WebResourceParamTemplate, itemSetNameExt.OldValue);
      var newParam = string.Format(WebResourceParamTemplate, itemSetNameExt.NewValue);
      var updateRequired = false;
      foreach (var form in forms)
      {
        if (!form.FormXml.Contains(oldParam))
        {
          continue;
        }

        updateRequired = true;
        pluginContext.Trace($"Adjusting {form.Name} form...");
        form.FormXml = form.FormXml.Replace(oldParam, newParam);
        orgCtx.Attach(form);
        orgCtx.UpdateObject(form);
      }

      if (!updateRequired)
      {
        return;
      }

      pluginContext.Trace("Saving form changes...");
      var itemSetConfig = new pavelkh_advancedmultiselectitemsetconfiguration()
                            {
                              pavelkh_advancedmultiselectitemsetconfigurationId = target.Id,
                              pavelkh_PublishRequired = true
                            };
      orgCtx.Attach(itemSetConfig);
      orgCtx.UpdateObject(itemSetConfig);
      orgCtx.SaveChanges();
    }

    private string AlignItemSetName(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        throw new InvalidPluginExecutionException("Item Set Name is invalid.");
      }

      value = value.Trim();
      value = Regex.Replace(value, @"[^\p{L}0-9 -]", "");
      return Regex.Replace(value, @"\s+", " ");
    }

    private void ValidateDuplicates(pavelkh_advancedmultiselectitemsetconfiguration itemSetConfig)
    {
      var id = itemSetConfig.Id;
      var entityName = itemSetConfig.pavelkh_EntityName;
      var itemSetName = itemSetConfig.pavelkh_ItemSetName;
      var savingField = itemSetConfig.pavelkh_DummySavingField;

      var pluginContext = this.PluginContext;
      var orgCtx = pluginContext.OrgCtx;
      
      // ReSharper disable once ReplaceWithSingleCallToFirstOrDefault
      var duplicate = orgCtx.pavelkh_advancedmultiselectitemsetconfigurationSet
        .Where(
          r => r.pavelkh_EntityName == entityName 
          && r.pavelkh_advancedmultiselectitemsetconfigurationId != id
          && (r.pavelkh_ItemSetName == itemSetName || r.pavelkh_DummySavingField == savingField))
        .FirstOrDefault();

      if (duplicate == null)
      {
        return;
      }

      if (duplicate.pavelkh_ItemSetName.Equals(itemSetName, StringComparison.InvariantCultureIgnoreCase))
      {
        throw new InvalidPluginExecutionException(
          $"{this.GetGenericItemSetSavingErrorMessage(itemSetConfig)} "
          + $"There is already a configuration with the same Item Set Name ('{itemSetName}') for the '{entityName}' entity. "
          + "Please specify another Item Set Name.");
      }

      if (duplicate.pavelkh_DummySavingField.Equals(savingField, StringComparison.InvariantCultureIgnoreCase))
      {
        throw new InvalidPluginExecutionException(
          $"{this.GetGenericItemSetSavingErrorMessage(itemSetConfig)} "
          + $"There is already a configuration with the same Dummy Saving Attribute ('{savingField}') for the '{entityName}' entity. "
          + "Please specify another Dummy Saving Attribute.");
      }
    }

    private void ValidateFetchXmlConsistency(pavelkh_advancedmultiselectitemsetconfiguration itemSetConfig)
    {
      var queryExpression = this.ConvertToQueryExpression(itemSetConfig.pavelkh_FetchXml);
      var itemSetEntityName = itemSetConfig.pavelkh_ItemSetEntityName;
      var entityNameValid = queryExpression.EntityName.Equals(itemSetEntityName, StringComparison.InvariantCultureIgnoreCase);
      if (!entityNameValid)
      {
        throw new InvalidPluginExecutionException(
          $"{this.GetGenericItemSetSavingErrorMessage(itemSetConfig)} "
          + "Fetch Xml is incorrect. It is not consistent with the 'Item Set Entity Name' "
          + "for the '{itemSetName}' Item Set configuration.");
      }

      var labelAttribute = itemSetConfig.pavelkh_LabelAttributeName;
      var labelAttributeValid = queryExpression.ColumnSet.AllColumns
                                || queryExpression.ColumnSet.Columns.Any(r => r.Equals(labelAttribute));

      if (!labelAttributeValid)
      {
        throw new InvalidPluginExecutionException(
          $"{this.GetGenericItemSetSavingErrorMessage(itemSetConfig)} "
          + $"Fetch Xml is incorrect. It must contain '{labelAttribute}' attribute "
          + "(specified in the 'Item Set Label Attribute' field).");
      }

      var tooltipAttribute = itemSetConfig.pavelkh_TooltipAttributeName;
      var tooltipAttributeValid = string.IsNullOrWhiteSpace(tooltipAttribute) || queryExpression.ColumnSet.AllColumns
                                || queryExpression.ColumnSet.Columns.Any(r => r.Equals(tooltipAttribute));

      if (!tooltipAttributeValid)
      {
        throw new InvalidPluginExecutionException(
          $"{this.GetGenericItemSetSavingErrorMessage(itemSetConfig)} "
          + $"Fetch Xml is incorrect. It must contain '{tooltipAttribute}' attribute "
          + "(specified in the 'Item Set Tooltip Attribute' field).");
      }
    }

    private void ValidateDummySavingFieldAttributeName(pavelkh_advancedmultiselectitemsetconfiguration itemSetConfig)
    {
      var validTypes = new[] { AttributeTypeCode.String, AttributeTypeCode.Memo };
      this.ValidateAttribute(
        itemSetConfig.pavelkh_ItemSetName,
        itemSetConfig.pavelkh_EntityName,
        itemSetConfig.pavelkh_DummySavingField,
        "Dummy Saving Attribute",
        true,
        validTypes);
    }

    private void ValidateItemSetLabelAttributeAttributeName(pavelkh_advancedmultiselectitemsetconfiguration itemSetConfig)
    {
      var validTypes = new[] { AttributeTypeCode.String, AttributeTypeCode.Memo };
      this.ValidateAttribute(
        itemSetConfig.pavelkh_ItemSetName,
        itemSetConfig.pavelkh_ItemSetEntityName,
        itemSetConfig.pavelkh_LabelAttributeName,
        "Item Set Label Attribute",
        true,
        validTypes);
    }

    private void ValidateItemSetTooltipAttributeName(pavelkh_advancedmultiselectitemsetconfiguration itemSetConfig)
    {
      var validTypes = new[] { AttributeTypeCode.String, AttributeTypeCode.Memo };
      this.ValidateAttribute(
        itemSetConfig.pavelkh_ItemSetName,
        itemSetConfig.pavelkh_ItemSetEntityName,
        itemSetConfig.pavelkh_TooltipAttributeName,
        "Tooltip Attribute",
        false,
        validTypes);
    }

    private void ValidateAutoprocessItemSetStatusAttributeName(pavelkh_advancedmultiselectitemsetconfiguration itemSetConfig)
    {
      var autoProcessItemStatus = itemSetConfig.pavelkh_AutoProcessItemStatus ?? false;
      if (!autoProcessItemStatus)
      {
        return;
      }

      var validTypes = new[] { AttributeTypeCode.State, AttributeTypeCode.Boolean };
      this.ValidateAttribute(
        itemSetConfig.pavelkh_ItemSetName, 
        itemSetConfig.pavelkh_EntityName, 
        itemSetConfig.pavelkh_AutoprocessItemStatusAttributeName,
        "Item Status Attribute",
        true, 
        validTypes);
    }

    private void ValidateAttribute(
      string itemSetName,
      string entityLogicalName, 
      string attrLogicalName, 
      string itemSetConfigAttrDisplayName,
      bool mandatory,
      IEnumerable<AttributeTypeCode> validTypes)
    {
      if (string.IsNullOrWhiteSpace(attrLogicalName))
      {
        if (mandatory)
        {
          throw new InvalidPluginExecutionException(
            $"{this.GetGenericItemSetSavingErrorMessage(itemSetName)} "
            + $"'{itemSetConfigAttrDisplayName}' is not specified.");
        }

        return;
      }

      var pluginContext = this.PluginContext;
      var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
      entityFilter.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, entityLogicalName));
      var entityProperties = new MetadataPropertiesExpression { AllProperties = false };
      entityProperties.PropertyNames.AddRange("LogicalName", "Attributes");
      var attributeFilter = new MetadataFilterExpression(LogicalOperator.And);
      attributeFilter.Conditions.Add(
        new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, attrLogicalName));
      var attributeProperties = new MetadataPropertiesExpression() { AllProperties = false };
      attributeProperties.PropertyNames.AddRange("LogicalName", "AttributeType");
      var attributeQuery = new AttributeQueryExpression()
      {
        Criteria = attributeFilter,
        Properties = attributeProperties
      };

      var query = new EntityQueryExpression
      {
        Criteria = entityFilter,
        Properties = entityProperties,
        AttributeQuery = attributeQuery,
      };

      var request = new RetrieveMetadataChangesRequest { Query = query };
      var respones = (RetrieveMetadataChangesResponse)pluginContext.OrgCtx.Execute(request);
      var entityMetadata = respones.EntityMetadata.FirstOrDefault();
      if (entityMetadata == null || !entityMetadata.Attributes.Any())
      {
        throw new InvalidPluginExecutionException(
          $"{this.GetGenericItemSetSavingErrorMessage(itemSetName)} "
          + $"'{attrLogicalName}' {itemSetConfigAttrDisplayName} is not an attribute of the {entityLogicalName} Entity.");
      }

      var attribute = entityMetadata.Attributes.FirstOrDefault();
      
      // ReSharper disable once PossibleNullReferenceException
      var validType = validTypes.Any(r => r == attribute.AttributeType);
      if (!validType)
      {
        throw new InvalidPluginExecutionException(
          $"{this.GetGenericItemSetSavingErrorMessage(itemSetName)} {itemSetConfigAttrDisplayName} has incorrect data type.");
      }
    }

    private QueryExpression ConvertToQueryExpression(string fetchXml)
    {
      var request = new FetchXmlToQueryExpressionRequest { FetchXml = fetchXml };
      try
      {
        var response = (FetchXmlToQueryExpressionResponse)this.PluginContext.Service.Execute(request);
        return response.Query;
      }
      catch (Exception)
      {
        throw new InvalidPluginExecutionException("Fetch Xml is not valid. Please check the 'Fetch Xml Query'.");
      }
    }

    private void SetRelationshipDetailsAttributes(pavelkh_advancedmultiselectitemsetconfiguration itemSetConfig)
    {
      var pluginContext = this.PluginContext;
      pluginContext.Trace("Filling in Item Set Details...");
      var relationshipName = itemSetConfig.pavelkh_RelationshipName;
      var relationship = MetadataUtils.GetManyToManyRelationshipMetadata(
        pluginContext,
        relationshipName);

      if (relationship == null)
      {
        throw new InvalidPluginExecutionException(
          $"{this.GetGenericItemSetSavingErrorMessage(itemSetConfig)} The '{relationshipName}' relationship is not found.");
      }

      var target = pluginContext.InputTarget;
      var currentEntityName = itemSetConfig.pavelkh_EntityName;
      string intersectCurrentEntityRefAttributeName;
      string intersectItemSetEntityRefAttributeName;
      if (relationship.Entity1LogicalName == currentEntityName)
      {
        target.pavelkh_ItemSetEntityName = relationship.Entity2LogicalName;
        target.pavelkh_EntityAttributeName = relationship.Entity1IntersectAttribute;
        target.pavelkh_ItemSetEntityAttributeName = relationship.Entity2IntersectAttribute;
        intersectCurrentEntityRefAttributeName = relationship.Entity1IntersectAttribute;
        intersectItemSetEntityRefAttributeName = relationship.Entity2IntersectAttribute;
      }
      else
      {
        if (relationship.Entity2LogicalName != currentEntityName)
        {
          throw new InvalidPluginExecutionException(
            $"{this.GetGenericItemSetSavingErrorMessage(itemSetConfig)} { currentEntityName } Entity is not associated with the { relationshipName } relationship.");
        }

        target.pavelkh_ItemSetEntityName =  relationship.Entity1LogicalName;
        target.pavelkh_EntityAttributeName = relationship.Entity2IntersectAttribute;
        target.pavelkh_ItemSetEntityAttributeName = relationship.Entity1IntersectAttribute;

        intersectCurrentEntityRefAttributeName = relationship.Entity2IntersectAttribute;
        intersectItemSetEntityRefAttributeName = relationship.Entity1IntersectAttribute;
      }

      target.pavelkh_IsCustomRelationship = relationship.IsCustomRelationship;
      target.pavelkh_IntersectEntityName = relationship.IntersectEntityName;
      target.pavelkh_IntersectEntityRefAttributeName = $"{IntersectLinkedEntityAlias}.{intersectCurrentEntityRefAttributeName}";

      itemSetConfig.pavelkh_ItemSetEntityName = target.pavelkh_ItemSetEntityName;

      #region Figure Out ItemSet Entity PrimaryKey Attribute Name

      var props = new MetadataPropertiesExpression { AllProperties = false };
      props.PropertyNames.Add("PrimaryIdAttribute");
      var filter = new MetadataFilterExpression();
      filter.Conditions.Add(new MetadataConditionExpression(
        "LogicalName",
        MetadataConditionOperator.Equals,
        target.pavelkh_ItemSetEntityName));
      var query = new EntityQueryExpression { Properties = props, Criteria = filter };
      var request = new RetrieveMetadataChangesRequest { Query = query };
      var service = pluginContext.ServiceAsSystemUser;
      var response = (RetrieveMetadataChangesResponse)service.Execute(request);
      var entityMetadata = response.EntityMetadata;
      // ReSharper disable PossibleNullReferenceException
      var itemSetEntityPrimaryKeyAttributeName =
        entityMetadata.FirstOrDefault().PrimaryIdAttribute;
      // ReSharper restore PossibleNullReferenceException

      #endregion

      #region Build Additional Fetch Xml Prepared FetchXml Query Templates

      var fetchXmlToQueryRequest = new FetchXmlToQueryExpressionRequest { FetchXml = itemSetConfig.pavelkh_FetchXml };
      QueryExpression queryExpression;
      try
      {
        queryExpression = ((FetchXmlToQueryExpressionResponse)service.Execute(fetchXmlToQueryRequest)).Query;
      }
      catch (Exception exc)
      {
        throw new InvalidPluginExecutionException(
          $"Fetch Xml query is not valid. Details: {exc.Message}"  );
      }

      var itemSetLinkedEntity = queryExpression.AddLink(
          relationship.IntersectEntityName, itemSetEntityPrimaryKeyAttributeName,
          intersectItemSetEntityRefAttributeName, JoinOperator.Inner);
      itemSetLinkedEntity.EntityAlias = IntersectLinkedEntityAlias;
      itemSetLinkedEntity.LinkCriteria = new FilterExpression();
      itemSetLinkedEntity.LinkCriteria.AddCondition(intersectCurrentEntityRefAttributeName, ConditionOperator.Equal, ItemSetBuilder.FetchXmlEntityIdPlaceHolder);
      var baseColumnSet = new ColumnSet(queryExpression.ColumnSet.Columns.ToArray());
      queryExpression.ColumnSet = new ColumnSet(false);
      var queryToFetchXmlRequest = new QueryExpressionToFetchXmlRequest { Query = queryExpression };
      var fetchXml = ((QueryExpressionToFetchXmlResponse)service.Execute(queryToFetchXmlRequest)).FetchXml;
      target.pavelkh_FetchXmlForIntersect = fetchXml;

      queryExpression.ColumnSet = baseColumnSet;
      itemSetLinkedEntity.JoinOperator = JoinOperator.LeftOuter;
      itemSetLinkedEntity.Columns.AddColumn(intersectCurrentEntityRefAttributeName);
      if (itemSetConfig.pavelkh_AutoProcessItemStatus == true)
      {
        var statusAttrName = itemSetConfig.pavelkh_AutoprocessItemStatusAttributeName;
        var columns = queryExpression.ColumnSet.Columns;
        var statusAttrExists = columns.Contains(statusAttrName);
        if (!statusAttrExists)
        {
          columns.Add(statusAttrName);
        }

        var mainFilter = new FilterExpression(LogicalOperator.And);
        var leftOuterJoinFilter = new FilterExpression(LogicalOperator.Or);
        leftOuterJoinFilter.AddCondition(statusAttrName, ConditionOperator.Equal, 0);
        leftOuterJoinFilter.AddCondition(IntersectLinkedEntityAlias, intersectCurrentEntityRefAttributeName, ConditionOperator.NotNull);
        mainFilter.AddFilter(leftOuterJoinFilter);
        if (queryExpression.Criteria != null)
        {
          mainFilter.AddFilter(queryExpression.Criteria);
        }

        queryExpression.Criteria = mainFilter;
      }

      queryToFetchXmlRequest = new QueryExpressionToFetchXmlRequest { Query = queryExpression };
      fetchXml = ((QueryExpressionToFetchXmlResponse)service.Execute(queryToFetchXmlRequest)).FetchXml;
      target.pavelkh_FetchXmlForEditMode = fetchXml;

      #endregion
    }

    private void ProcessHandleSecurityPrivileges(pavelkh_advancedmultiselectitemsetconfiguration itemSetConfig)
    {
      var handleSecurityPrivileges = itemSetConfig.pavelkh_HandleSecurityPrivileges ?? false;
      var pluginContext = this.PluginContext;
      var target = pluginContext.InputTarget;
      if (!handleSecurityPrivileges)
      {
        target.pavelkh_EntityHasOrganizationOwnership = null;
        target.pavelkh_ItemSetEntityHasOrganizationOwnership = null;
        return;
      }

      var props = new MetadataPropertiesExpression { AllProperties = false };
      props.PropertyNames.AddRange("LogicalName", "OwnershipType");
      var filter = new MetadataFilterExpression();
      filter.Conditions.Add(new MetadataConditionExpression(
        "LogicalName",
        MetadataConditionOperator.In, new []
                                        {
                                          itemSetConfig.pavelkh_EntityName,
                                          itemSetConfig.pavelkh_ItemSetEntityName
                                        }));
      var query = new EntityQueryExpression { Properties = props, Criteria = filter };
      var request = new RetrieveMetadataChangesRequest { Query = query };
      var service = pluginContext.ServiceAsSystemUser;
      var response = (RetrieveMetadataChangesResponse)service.Execute(request);
      var entityMetadata = response.EntityMetadata;
      target.pavelkh_EntityHasOrganizationOwnership =
        // ReSharper disable once PossibleNullReferenceException
        entityMetadata.FirstOrDefault(
          r => r.LogicalName.Equals(itemSetConfig.pavelkh_EntityName)).OwnershipType == OwnershipTypes.OrganizationOwned;
      target.pavelkh_ItemSetEntityHasOrganizationOwnership =
        // ReSharper disable once PossibleNullReferenceException
        entityMetadata.FirstOrDefault(
          r => r.LogicalName.Equals(itemSetConfig.pavelkh_ItemSetEntityName)).OwnershipType == OwnershipTypes.OrganizationOwned;
    }

    private string GetGenericItemSetSavingErrorMessage(pavelkh_advancedmultiselectitemsetconfiguration itemSetConfig)
    {
      return this.GetGenericItemSetSavingErrorMessage(itemSetConfig.pavelkh_ItemSetName);
    }

    private string GetGenericItemSetSavingErrorMessage(string itemSetName)
    {
      return $"It's not possible to save '{itemSetName}' Item Set configuration. ";
    }

    private void SyncDynamicPluginStepsOnCreate()
    {
      this.SyncDynamicPluginSteps();
    }

    private void SyncDynamicPluginStepsOnUpdate()
    {
      var pluginContext = this.PluginContext;
      var entityExt = pluginContext.TargetExt;
      var target = entityExt.Target;
      var entityNameExt = entityExt.GetValue(() => target.pavelkh_EntityName);
      var dummySavingAttributeNameExt = entityExt.GetValue(() => target.pavelkh_DummySavingField);
      var ignoreAction = !entityNameExt.IsModified && !dummySavingAttributeNameExt.IsModified;
      if (ignoreAction)
      {
        return;
      }

      this.SyncDynamicPluginSteps();
    }

    private void SyncDynamicPluginStepsOnDelete()
    {
      this.SyncDynamicPluginSteps();
    }

    private void SyncDynamicPluginSteps()
    {
      var pluginContext = this.PluginContext;
      var orgCtx = pluginContext.OrgCtx;
      var orgService = pluginContext.Service;
      pluginContext.Trace("Deleting Dynamic Steps...");
      const string PluginTypeName = "AdvancedMultiSelect.Plugins.ItemSet.SaveItemSet";
      var eventHandlerId = orgCtx.PluginTypeSet
        .Where(r => r.TypeName == PluginTypeName)
        .Select(r => r.PluginTypeId).FirstOrDefault();
      if (eventHandlerId == null)
      {
        throw new NullReferenceException(nameof(eventHandlerId));
      }

      var dynamicStepList = (from s in orgCtx.SdkMessageProcessingStepSet
                             where (
                              s.EventHandler != null && s.EventHandler.Id == eventHandlerId.Value)
                             select new SdkMessageProcessingStep
                             {
                               SdkMessageProcessingStepId = s.SdkMessageProcessingStepId
                             }).ToList();
      dynamicStepList.ForEach(r =>
        orgService.Delete(SdkMessageProcessingStep.EntityLogicalName, r.Id));

      pluginContext.Trace("Creating Dynamic Steps...");
      var configs = orgCtx.pavelkh_advancedmultiselectitemsetconfigurationSet
        .Select(r => new
        {
          EntityName = r.pavelkh_EntityName,
          DummySavingAttributeName = r.pavelkh_DummySavingField
        })
        .ToList();

      var entityConfigs = configs
        .GroupBy(l => new { l.EntityName })
        .Select(g => new
        {
          g.Key.EntityName,
          Attributes = string.Join(",", g.Select(i => i.DummySavingAttributeName))
        });

      const int DynamicPluginStepRank = 10;
      const string DynamicPluginStepPrefix = "[AdvancedMultiSelect] [Dynamically Created] ";
      const string DynamicPluginStepDescription = "! DO NOT INCLUDE THIS STEP INTO YOUR CUSTOM SOLUTIONS. USE SPECIAL 'EXPORT CONFIGURATION' AND 'IMPORT CONFIGURATION' FEATURES TO TRANSFER CONFIGURATIONS.";
      var createMessageId = new Guid("9ebdbb1b-ea3e-db11-86a7-000a3a5473e8");
      var createMessageRef = new EntityReference(SdkMessage.EntityLogicalName, createMessageId);
      var updateMessageId = new Guid("20bebb1b-ea3e-db11-86a7-000a3a5473e8");
      var updateMessageRef = new EntityReference(SdkMessage.EntityLogicalName, updateMessageId);
      var syncMode = new OptionSetValue((int)Mode.Synchronous);
      var preValidationStage = new OptionSetValue((int)Stage.PreValidate);
      var postOperationStage = new OptionSetValue((int)Stage.PostOperation);
      var serverDeploymentMode = new OptionSetValue(0);
      var eventHandlerRef = new EntityReference(PluginType.EntityLogicalName, eventHandlerId.Value);

      foreach (var entityConfig in entityConfigs)
      {
        var entityName = entityConfig.EntityName;
        var createMessageFilterRef = GetSdkMessageFilter(orgCtx, createMessageId, entityName);
        var createPreValidationStep = new SdkMessageProcessingStep
        {
          Name = $"{DynamicPluginStepPrefix}[{entityName}] Save Item Set. Create. Pre-Validation.",
          Mode = syncMode,
          Rank = DynamicPluginStepRank,
          Configuration = string.Empty,
          Description = DynamicPluginStepDescription,
          Stage = preValidationStage,
          SupportedDeployment = serverDeploymentMode,
          EventHandler = eventHandlerRef,
          SdkMessageFilterId = createMessageFilterRef,
          SdkMessageId = createMessageRef
        };

        orgCtx.AddObject(createPreValidationStep);

        var createPostOperationStep = new SdkMessageProcessingStep
        {
          Name = $"{DynamicPluginStepPrefix}[{entityName}] Save Item Set. Create. Post-Operation.",
          Mode = syncMode,
          Rank = DynamicPluginStepRank,
          Configuration = string.Empty,
          Description = DynamicPluginStepDescription,
          Stage = postOperationStage,
          SupportedDeployment = serverDeploymentMode,
          EventHandler = eventHandlerRef,
          SdkMessageFilterId = createMessageFilterRef,
          SdkMessageId = createMessageRef
        };

        orgCtx.AddObject(createPostOperationStep);

        var updateMessageFilterRef = GetSdkMessageFilter(orgCtx, updateMessageId, entityName);
        var updatePreValidationStep = new SdkMessageProcessingStep
        {
          Name = $"{DynamicPluginStepPrefix}[{entityName}] Save Item Set. Update. Pre-Validation.",
          Mode = syncMode,
          Rank = DynamicPluginStepRank,
          Configuration = string.Empty,
          Description = DynamicPluginStepDescription,
          Stage = preValidationStage,
          SupportedDeployment = serverDeploymentMode,
          EventHandler = eventHandlerRef,
          SdkMessageFilterId = updateMessageFilterRef,
          SdkMessageId = updateMessageRef,
          FilteringAttributes = entityConfig.Attributes
        };

        orgCtx.AddObject(updatePreValidationStep);

        var updatePostOperationStep = new SdkMessageProcessingStep
        {
          Name = $"{DynamicPluginStepPrefix}[{entityName}] Save Item Set. Update. Post-Operation.",
          Mode = syncMode,
          Rank = DynamicPluginStepRank,
          Configuration = string.Empty,
          Description = DynamicPluginStepDescription,
          Stage = postOperationStage,
          SupportedDeployment = serverDeploymentMode,
          EventHandler = eventHandlerRef,
          SdkMessageFilterId = updateMessageFilterRef,
          SdkMessageId = updateMessageRef,
          FilteringAttributes = entityConfig.Attributes
        };

        orgCtx.AddObject(updatePostOperationStep);
      }

      orgCtx.SaveChanges();
    }
  }
}
