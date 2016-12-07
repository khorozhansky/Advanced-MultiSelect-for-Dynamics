namespace TuneMultiSelect.Logic.TuneMultiSelect
{
  using System;
  using System.Linq;

  using Entities;

  using Utils;

  using Microsoft.Crm.Sdk.Messages;
  using Microsoft.Xrm.Sdk;
  using Microsoft.Xrm.Sdk.Messages;
  using Microsoft.Xrm.Sdk.Metadata;
  using Microsoft.Xrm.Sdk.Metadata.Query;
  using Microsoft.Xrm.Sdk.Query;

  public class ItemSetConfigurationManager : ManagerBase<tunexrm_tunemultiselectitemsetconfiguration>
  {
    public ItemSetConfigurationManager(PluginBase<tunexrm_tunemultiselectitemsetconfiguration>.PluginContext pluginContext) : base(pluginContext)
    {
    }

    public void CreatePreOperationSync()
    {
      this.ProcessSave();
    }

    public void UpdatePreOperationSync()
    {
      this.ProcessSave();
    }

    public void DeletePreOperationSync()
    {
      this.DeleteItemSet();
    }

    private void ProcessSave()
    {
      var itemSetConfig = this.AlignFieldValues();
      this.ValidateDuplicates(itemSetConfig);
      this.ValidateDummySavingField(itemSetConfig);
      this.SetRelationshipDetailsAttributes(itemSetConfig);
      this.ValidateLabelAttributeSavingField(itemSetConfig);
      this.ValidateFetchXmlConsistency(itemSetConfig);
      this.ValidateAction(itemSetConfig);
    }

    private void ValidateAction(tunexrm_tunemultiselectitemsetconfiguration itemSetConfig)
    {
      var actionName = itemSetConfig.tunexrm_SaveChangesHandler;
      if (string.IsNullOrEmpty(actionName))
      {
        return;
      }

      var validActions = ItemSetConfigurationActionManager.GetActionList(this.PluginContext.OrgCtxAsSystemUser);
      if (validActions.All(r => r.UniqueName != actionName))
      {
        throw new InvalidPluginExecutionException($"The '{actionName}' action is not a valid Save Action. Please see documentation regarding when and how to use Save Action.");
      }
    }

    private tunexrm_tunemultiselectitemsetconfiguration AlignFieldValues()
    {
      var pluginContext = this.PluginContext;
      var targetExt = pluginContext.TargetExt;
      var target = targetExt.Target;
      var itemSetConfig = new tunexrm_tunemultiselectitemsetconfiguration
      {
        tunexrm_tunemultiselectitemsetconfigurationId = target.Id,
        tunexrm_EntityName = targetExt.GetValue(() => target.tunexrm_EntityName).Value,
        tunexrm_ItemSetName = targetExt.GetValue(() => target.tunexrm_ItemSetName).Value,
        tunexrm_RelationshipName = targetExt.GetValue(() => target.tunexrm_RelationshipName).Value,
        tunexrm_DummySavingField = targetExt.GetValue(() => target.tunexrm_DummySavingField).Value,
        tunexrm_LabelAttributeName = targetExt.GetValue(() => target.tunexrm_LabelAttributeName).Value,
        tunexrm_FetchXml = targetExt.GetValue(() => target.tunexrm_FetchXml).Value,
        tunexrm_SaveChangesHandler = targetExt.GetValue(() => target.tunexrm_SaveChangesHandler).Value
      };

      var attributes = new[]
      {
        itemSetConfig.tunexrm_EntityName,
        itemSetConfig.tunexrm_ItemSetName,
        itemSetConfig.tunexrm_RelationshipName,
        itemSetConfig.tunexrm_DummySavingField,
        itemSetConfig.tunexrm_LabelAttributeName,
        itemSetConfig.tunexrm_FetchXml
      };

      if (attributes.Any(string.IsNullOrWhiteSpace))
      {
        throw new InvalidPluginExecutionException("Please specify all mandatory fields.");
      }

      target.tunexrm_EntityName = 
        itemSetConfig.tunexrm_EntityName = itemSetConfig.tunexrm_EntityName.Trim().ToLowerInvariant();

      target.tunexrm_ItemSetName =
        itemSetConfig.tunexrm_ItemSetName = itemSetConfig.tunexrm_ItemSetName.Trim();

      target.tunexrm_RelationshipName =
        itemSetConfig.tunexrm_RelationshipName = itemSetConfig.tunexrm_RelationshipName.Trim();

      target.tunexrm_DummySavingField =
        itemSetConfig.tunexrm_DummySavingField = itemSetConfig.tunexrm_DummySavingField.Trim().ToLowerInvariant();

      target.tunexrm_LabelAttributeName =
        itemSetConfig.tunexrm_LabelAttributeName = itemSetConfig.tunexrm_LabelAttributeName.Trim().ToLowerInvariant();

      target.tunexrm_FetchXml =
        itemSetConfig.tunexrm_FetchXml = itemSetConfig.tunexrm_FetchXml.Trim().ToLowerInvariant();

      if (!string.IsNullOrEmpty(target.tunexrm_SaveChangesHandler))
      {
        target.tunexrm_SaveChangesHandler =
          itemSetConfig.tunexrm_SaveChangesHandler = itemSetConfig.tunexrm_SaveChangesHandler.Trim();
      }

      return itemSetConfig;
    }

    private void ValidateDuplicates(tunexrm_tunemultiselectitemsetconfiguration itemSetConfig)
    {
      var id = itemSetConfig.Id;
      var entityName = itemSetConfig.tunexrm_EntityName;
      var itemSetName = itemSetConfig.tunexrm_ItemSetName;
      var savingField = itemSetConfig.tunexrm_DummySavingField;

      var pluginContext = this.PluginContext;
      var orgCtx = pluginContext.OrgCtxAsSystemUser;
      
      // ReSharper disable once ReplaceWithSingleCallToFirstOrDefault
      var duplicate = orgCtx.tunexrm_tunemultiselectitemsetconfigurationSet
        .Where(
          r => r.tunexrm_EntityName == entityName 
          && r.tunexrm_tunemultiselectitemsetconfigurationId != id
          && (r.tunexrm_ItemSetName == itemSetName || r.tunexrm_DummySavingField == savingField))
        .FirstOrDefault();

      if (duplicate == null)
      {
        return;
      }

      if (duplicate.tunexrm_ItemSetName.Equals(itemSetName, StringComparison.InvariantCultureIgnoreCase))
      {
        throw new InvalidPluginExecutionException(
          "There is already a configuration with the same Item Set Name for this Entity. Please specify another Item Set Name.");
      }

      if (duplicate.tunexrm_DummySavingField.Equals(savingField, StringComparison.InvariantCultureIgnoreCase))
      {
        throw new InvalidPluginExecutionException(
          "There is already a configuration with the same Dummy Saving Attribute for this Entity. Please specify another Dummy Saving Attribute.");
      }
    }

    private void ValidateFetchXmlConsistency(tunexrm_tunemultiselectitemsetconfiguration itemSetConfig)
    {
      var queryExpression = this.ConvertToQueryExpression(itemSetConfig.tunexrm_FetchXml);
      var itemSetEntityName = itemSetConfig.tunexrm_ItemSetEntityName;
      var entityNameValid = queryExpression.EntityName.Equals(itemSetEntityName, StringComparison.InvariantCultureIgnoreCase);
      if (!entityNameValid)
      {
        throw new InvalidPluginExecutionException("Fetch Xml is incorrect. It is not consistent with the 'Item Set Entity Name'");
      }

      var labelAttribute = itemSetConfig.tunexrm_LabelAttributeName;
      var labelAttributeValid = queryExpression.ColumnSet.AllColumns
                                || queryExpression.ColumnSet.Columns.Any(r => r.Equals(labelAttribute));

      if (!labelAttributeValid)
      {
        throw new InvalidPluginExecutionException($"Fetch Xml is incorrect. It must contain '{labelAttribute}' attribute (specified in the 'Item Set Label Attribute' field).");
      }
    }

    private void ValidateDummySavingField(tunexrm_tunemultiselectitemsetconfiguration itemSetConfig)
    {
      var pluginContext = this.PluginContext;
      var entityName = itemSetConfig.tunexrm_EntityName;
      var savingFieldName = itemSetConfig.tunexrm_DummySavingField;
      var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
      entityFilter.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, entityName));
      var entityProperties = new MetadataPropertiesExpression { AllProperties = false };
      entityProperties.PropertyNames.AddRange("LogicalName", "Attributes");
      var attributeFilter = new MetadataFilterExpression(LogicalOperator.And);
      attributeFilter.Conditions.Add(
        new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, savingFieldName)); 
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
        throw new InvalidPluginExecutionException($"'{savingFieldName}' Dummy Saving Attribute is not an attribute of the {entityName} Entity");
      }

      var attribute = entityMetadata.Attributes.FirstOrDefault();
      var validTypes = new[] { AttributeTypeCode.String, AttributeTypeCode.Memo };
      // ReSharper disable once PossibleNullReferenceException
      var validType = validTypes.Any(r => r == attribute.AttributeType);
      if (!validType)
      {
        throw new InvalidPluginExecutionException("Dummy Saving Attribute should has either String or Memo Type.");
      }
    }

    private void ValidateLabelAttributeSavingField(tunexrm_tunemultiselectitemsetconfiguration itemSetConfig)
    {
      var pluginContext = this.PluginContext;
      var entityName = itemSetConfig.tunexrm_ItemSetEntityName;
      var labelAttribute = itemSetConfig.tunexrm_LabelAttributeName;
      var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
      entityFilter.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, entityName));
      var entityProperties = new MetadataPropertiesExpression { AllProperties = false };
      entityProperties.PropertyNames.AddRange("LogicalName", "Attributes");
      var attributeFilter = new MetadataFilterExpression(LogicalOperator.And);
      attributeFilter.Conditions.Add(
        new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, labelAttribute));
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
        throw new InvalidPluginExecutionException($"'{labelAttribute}' Item Set Label Attribute is not an attribute of the {entityName} Entity");
      }

      var attribute = entityMetadata.Attributes.FirstOrDefault();
      var validTypes = new[] { AttributeTypeCode.String, AttributeTypeCode.Memo };
      // ReSharper disable once PossibleNullReferenceException
      var validType = validTypes.Any(r => r == attribute.AttributeType);
      if (!validType)
      {
        throw new InvalidPluginExecutionException("Item Set Label Attribute should has String Type.");
      }
    }

    private QueryExpression ConvertToQueryExpression(string fetchXml)
    {
      var request = new FetchXmlToQueryExpressionRequest { FetchXml = fetchXml };
      try
      {
        var response = (FetchXmlToQueryExpressionResponse)this.PluginContext.ServiceAsSystemUser.Execute(request);
        return response.Query;
      }
      catch (Exception)
      {
        throw new InvalidPluginExecutionException("Fetch Xml is not valid. Please check the 'Fetch Xml Query'.");
      }
    }

    private void SetRelationshipDetailsAttributes(tunexrm_tunemultiselectitemsetconfiguration itemSetConfig)
    {
      var pluginContext = this.PluginContext;
      pluginContext.Trace("Filling in Item Set Details...");
      var relationshipName = itemSetConfig.tunexrm_RelationshipName;
      var relationship = MetadataUtils.GetManyToManyRelationshipMetadata(
        pluginContext,
        relationshipName);

      if (relationship == null)
      {
        throw new InvalidPluginExecutionException($"The '{relationshipName}' relationship is not found.");
      }

      var target = pluginContext.InputTarget;
      var entityName = itemSetConfig.tunexrm_EntityName;
      if (relationship.Entity1LogicalName == entityName)
      {
        target.tunexrm_ItemSetEntityName = relationship.Entity2LogicalName;
        target.tunexrm_EntityAttributeName = relationship.Entity1IntersectAttribute;
        target.tunexrm_ItemSetEntityAttributeName = relationship.Entity2IntersectAttribute;
      }
      else
      {
        if (relationship.Entity2LogicalName != entityName)
        {
          throw new InvalidPluginExecutionException(
            $"An error occured while saving the Item Set configuration. { entityName } Entity is not accosiated with the { relationshipName } relationship.");
        }

        target.tunexrm_ItemSetEntityName = relationship.Entity1LogicalName;
        target.tunexrm_EntityAttributeName = relationship.Entity2IntersectAttribute;
        target.tunexrm_ItemSetEntityAttributeName = relationship.Entity1IntersectAttribute;
      }

      target.tunexrm_IsCustomRelationship = relationship.IsCustomRelationship;
      target.tunexrm_IntersectEntityName = relationship.IntersectEntityName;
      itemSetConfig.tunexrm_ItemSetEntityName = target.tunexrm_ItemSetEntityName;
    }

    private void DeleteItemSet()
    {
      // TODO: might be an additional validation
    }
  }
}
