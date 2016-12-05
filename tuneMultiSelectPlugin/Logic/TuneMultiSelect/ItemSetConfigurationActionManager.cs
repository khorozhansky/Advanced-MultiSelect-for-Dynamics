namespace TuneMultiSelect.Logic.TuneMultiSelect
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.Serialization;

  using Utils;

  using Microsoft.Xrm.Sdk;
  using Microsoft.Xrm.Sdk.Messages;
  using Microsoft.Xrm.Sdk.Metadata;
  using Microsoft.Xrm.Sdk.Metadata.Query;
  using Microsoft.Xrm.Sdk.Query;

  public class ItemSetConfigurationActionManager : ManagerBase<Entity>
  {
    public ItemSetConfigurationActionManager(PluginBase<Entity>.PluginContext pluginContext)
      : base(pluginContext)
    {
    }

    public void ProcessGetEntitiesAction()
    {
      var entityMetadataCollection = this.GetEntitiesMetadata().OrderBy(r => r.LogicalName).ToList();
      var entities = entityMetadataCollection.Select(GetEntityDto).Where(r => r != null).ToList();
      var result = JsonHelper.SerializeJson(entities);
      const string EntityListParamName = "EntityList";
      this.PluginContext.SetOutputParameter(EntityListParamName, result);
    }

    private static EntityDto GetEntityDto(EntityMetadata entityMetadata)
    {
      string icon;
      if (entityMetadata.IsCustomEntity == true)
      {
        icon = entityMetadata.IconSmallName != null ? $"/WebResources/{entityMetadata.IconSmallName}" : string.Empty;
      }
      else
      {
        icon = $"/_imgs/ico_16_{entityMetadata.ObjectTypeCode}.gif";
      }

      var logicalName = entityMetadata.LogicalName;
      var displayName = entityMetadata.DisplayName.UserLocalizedLabel.Label;
      var entity = new EntityDto
      {
        LogicalName = logicalName,
        DisplayName = displayName,
        Icon = icon,
        Attributes = new List<AttributeDto>(),
        Relationships = new List<RelationshipDto>()
      };

      if (entityMetadata.ManyToManyRelationships.Length == 0)
      {
        return null;
      }

      foreach (var relationship in entityMetadata.ManyToManyRelationships)
      {
        entity.Relationships.Add(GetRelationshipDto(logicalName, relationship));
      }

      foreach (var attr in entityMetadata.Attributes.Select(GetAttributeDto).Where(attr => attr != null))
      {
        entity.Attributes.Add(attr);
      }

      return entity;
    }

    private static RelationshipDto GetRelationshipDto(string entityLogicalName, ManyToManyRelationshipMetadata relationshipMetadata)
    {
      var relationLogicalName = relationshipMetadata.SchemaName;
      var itemSetEntityLogicalName =
        relationshipMetadata.Entity1LogicalName.Equals(entityLogicalName, StringComparison.InvariantCultureIgnoreCase) ?
          relationshipMetadata.Entity2LogicalName :
          relationshipMetadata.Entity1LogicalName;

      return new RelationshipDto
      {
        Relationship = relationLogicalName,
        ItemSetEntity = itemSetEntityLogicalName
      };
    }

    private static AttributeDto GetAttributeDto(AttributeMetadata attributeMetadata)
    {
      var maxLength = attributeMetadata.AttributeType == AttributeTypeCode.String
                        ? ((StringAttributeMetadata)attributeMetadata).MaxLength
                        : ((MemoAttributeMetadata)attributeMetadata).MaxLength;
      var skip = attributeMetadata.DisplayName?.UserLocalizedLabel == null;
      if (skip)
      {
        return null;
      }

      var displayName = attributeMetadata.DisplayName.UserLocalizedLabel.Label;
      return new AttributeDto
      {
        LogicalName = attributeMetadata.LogicalName,
        DisplayName = displayName,
        IsCustomAttribute = attributeMetadata.IsCustomAttribute ?? false,
        MaxLength = maxLength ?? 0,
      };
    }

    private EntityMetadataCollection GetEntitiesMetadata()
    {
      var pluginContext = this.PluginContext;
      pluginContext.Trace("Retrieving Entity List...");
      var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
      entityFilter.Conditions.Add(new MetadataConditionExpression("IsCustomizable", MetadataConditionOperator.Equals, true));
      entityFilter.Conditions.Add(new MetadataConditionExpression("IsIntersect", MetadataConditionOperator.Equals, false));
      entityFilter.Conditions.Add(new MetadataConditionExpression("CanBeInManyToMany", MetadataConditionOperator.Equals, true));
      var entityProperties = new MetadataPropertiesExpression { AllProperties = false };
      entityProperties.PropertyNames.AddRange("LogicalName", "ObjectTypeCode", "DisplayName", "IsCustomEntity", "IconSmallName", "ManyToManyRelationships", "Attributes");
      var relationshipProperties = new MetadataPropertiesExpression { AllProperties = false };
      relationshipProperties.PropertyNames.AddRange("SchemaName", "Entity1LogicalName", "Entity2LogicalName");
      var relationshipQuery = new RelationshipQueryExpression
      {
        Properties = relationshipProperties,
      };

      var attributeFilter1 = new MetadataFilterExpression(LogicalOperator.And);
      attributeFilter1.Conditions.Add(
        new MetadataConditionExpression("AttributeType", MetadataConditionOperator.In, new []
                                                                                         {
                                                                                           AttributeTypeCode.String,
                                                                                           AttributeTypeCode.Memo
                                                                                         }));
      attributeFilter1.Conditions.Add(
        new MetadataConditionExpression("LogicalName", MetadataConditionOperator.NotIn, new string[] { "traversedpath" }));

      var attributeFilter2 = new MetadataFilterExpression(LogicalOperator.Or);
      attributeFilter2.Conditions.Add(
        new MetadataConditionExpression("IsValidForUpdate", MetadataConditionOperator.Equals, true));
      attributeFilter2.Conditions.Add(
        new MetadataConditionExpression("IsPrimaryName", MetadataConditionOperator.Equals, true));

      var attributeFilter = new MetadataFilterExpression(LogicalOperator.And);
      attributeFilter.Filters.AddRange(attributeFilter1, attributeFilter2);
      var attributeProperties = new MetadataPropertiesExpression() { AllProperties = false };
      attributeProperties.PropertyNames.AddRange("LogicalName", "DisplayName", "IsCustomAttribute", "MaxLength");
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
        RelationshipQuery = relationshipQuery
      };

      var request = new RetrieveMetadataChangesRequest { Query = query };
      var respones = (RetrieveMetadataChangesResponse)pluginContext.OrgCtx.Execute(request);
      return respones.EntityMetadata;
    }

    #region DTO subclasses

    [DataContract]
    public class EntityDto
    {
      [DataMember]
      public string LogicalName { get; set; }

      [DataMember]
      public string DisplayName { get; set; }

      [DataMember]
      public string Icon { get; set; }

      [DataMember]
      public IList<RelationshipDto> Relationships { get; set; }

      [DataMember]
      public IList<AttributeDto> Attributes { get; set; }
    }

    [DataContract]
    public class AttributeDto
    {
      [DataMember]
      public string LogicalName { get; set; }

      [DataMember]
      public string DisplayName { get; set; }

      [DataMember]
      public bool IsCustomAttribute { get; set; }

      [DataMember]
      public int MaxLength { get; set; }
    }

    [DataContract]
    public class RelationshipDto
    {
      [DataMember]
      public string Relationship { get; set; }

      [DataMember]
      public string ItemSetEntity { get; set; }
    }
  }

  #endregion
}