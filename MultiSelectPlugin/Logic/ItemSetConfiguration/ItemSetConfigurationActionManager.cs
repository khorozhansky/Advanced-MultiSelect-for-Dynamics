namespace AdvancedMultiSelect.Logic.ItemSetConfiguration
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.Serialization;
  using System.Xml.Linq;

  using CrmProxy;
  using Utils;

  using Microsoft.Xrm.Sdk;
  using Microsoft.Xrm.Sdk.Messages;
  using Microsoft.Xrm.Sdk.Metadata;
  using Microsoft.Xrm.Sdk.Metadata.Query;
  using Microsoft.Xrm.Sdk.Query;

  public class ItemSetConfigurationActionManager : ManagerBase<Entity>
  {
    public const int MainFormTypeCode = 2;
    private const int FormActivationStateActiveCode = 1;

    private int? currentUserLangCode;

    public ItemSetConfigurationActionManager(PluginBase<Entity>.PluginContext pluginContext)
      : base(pluginContext)
    {
    }

    public static IList<ActionDto> GetActionList(CrmContext orgCtx)
    {
      const int WorkflowTypeCode = 1;
      const int WorkflowCategoryCode = 3;
      const string PrimaryEntity = "none";
      const int ActiveStatusCode = 2;
      var actions = (from w in orgCtx.WorkflowSet
                     join ms in orgCtx.SdkMessageSet on w.SdkMessageId.Id equals ms.SdkMessageId.Value
                     where (
                      w.StateCode == WorkflowState.Activated
                      && w.StatusCode != null && w.StatusCode.Value == ActiveStatusCode
                      && w.IsCustomizable != null && w.IsCustomizable.Value
                      && w.SdkMessageId != null
                      && w.Type != null && w.Type.Value == WorkflowTypeCode
                      && w.Category != null && w.Category.Value == WorkflowCategoryCode
                      && w.PrimaryEntity == PrimaryEntity
                      && w.UniqueName != "ItemSetConfigurationGetEntities"
                      && w.UniqueName != "GetItemSet"
                      && w.UniqueName != "ItemSetConfigurationAddControlOnForm"
                      && w.UniqueName != "ItemSetConfigurationGetAddControlWizardData"
                      && w.UniqueName != "ItemSetConfigurationExport"
                      && w.UniqueName != "ItemSetConfigurationImport"
                      && w.UniqueName != "DemoInstallDemoData"
                     )
                     select new ActionDto { UniqueName = ms.Name, Name = w.Name })
                  .ToList();

      return actions.OrderBy(r => r.UniqueName).ToList();
    }

    public static IList<string> GetPrefixList(CrmContext orgCtx)
    {
      var defaultPublisherRef =
        orgCtx.SolutionSet
          .Where(r => r.UniqueName == "Default")
          .Select(r => r.PublisherId)
          .FirstOrDefault();

      var publishers = 
        orgCtx.PublisherSet.Select(
          r => new Publisher
                 {
                   PublisherId = r.PublisherId,
                   CustomizationPrefix = r.CustomizationPrefix
                 }).ToList();

      var defaultPrefix = defaultPublisherRef == null
                            ? null
                            : publishers.FirstOrDefault(r => r.PublisherId == defaultPublisherRef.Id)?.CustomizationPrefix;
      defaultPrefix = defaultPrefix == null ? null : defaultPrefix + "_";
      var prefixes = publishers.Select(r => r.CustomizationPrefix + "_").ToList();
      if (defaultPrefix == null)
      {
        return prefixes;
      }

      var result = new List<string>();
      result.Add(defaultPrefix);
      result.AddRange(prefixes.Where(r => !r.Equals(defaultPrefix, StringComparison.InvariantCultureIgnoreCase)));
      return result;
    }

    public void ProcessGetEntitiesAction()
    {
      var entityMetadataCollection = this.GetEntitiesMetadata().OrderBy(r => r.LogicalName).ToList();
      var entities = entityMetadataCollection.Select(GetEntityDto).Where(r => r != null).ToList();
      var entitiesResult = JsonHelper.SerializeJson(entities);
      const string EntityListParamName = "EntityList";
      var pluginContext = this.PluginContext;
      var orgCtx = pluginContext.OrgCtxAsSystemUser;
      pluginContext.SetOutputParameter(EntityListParamName, entitiesResult);
      var actions = GetActionList(orgCtx);
      var actionsResult = JsonHelper.SerializeJson(actions);
      const string ActionListParamName = "ActionList";
      pluginContext.SetOutputParameter(ActionListParamName, actionsResult);
      var prefixes = GetPrefixList(orgCtx);
      var prefixesResult = JsonHelper.SerializeJson(prefixes);
      const string ValidPublishPrefixListParamName = "ValidPublishPrefixList";
      pluginContext.SetOutputParameter(ValidPublishPrefixListParamName, prefixesResult);
    }

    public void ProcessGetAddControlWizardData()
    {
      var pluginContext = this.PluginContext;
      const string EntityLogicalNameParamName = "EntityLogicalName";
      var entityLogicalName = pluginContext.GetInputParameter<string>(EntityLogicalNameParamName);
      if (string.IsNullOrWhiteSpace(entityLogicalName))
      {
        throw new InvalidPluginExecutionException($"{EntityLogicalNameParamName} parameter is not specified.");
      }

      var forms = this.GetFormsDto(entityLogicalName);
      var formsResult = JsonHelper.SerializeJson(forms);
      const string FormsParamName = "FormList";
      pluginContext.SetOutputParameter(FormsParamName, formsResult);

      var templates = this.GetTemplatesDto();
      var templatesResult = JsonHelper.SerializeJson(templates);
      const string TemplatesParamName = "TemplateList";
      pluginContext.SetOutputParameter(TemplatesParamName, templatesResult);
    }

    public void Export()
    {
      var pluginContext = this.PluginContext;
      pluginContext.Trace("Export Configurations...");
      var selectedConfigIdsString = this.PluginContext.GetInputParameter<string>("SelectedIds");
      var selectedConfigIds = selectedConfigIdsString.Split(',');
      var query = new QueryExpression(pavelkh_advancedmultiselectitemsetconfiguration.EntityLogicalName)
                    {
                      ColumnSet = new ColumnSet(true),
                      Criteria =new FilterExpression()
                    };

      query.Criteria.AddCondition(
        new ConditionExpression(
          pavelkh_advancedmultiselectitemsetconfiguration.Fields.pavelkh_advancedmultiselectitemsetconfigurationId, 
          ConditionOperator.In,
          // ReSharper disable once CoVariantArrayConversion
          selectedConfigIds));

      var orgService = this.PluginContext.Service;
      var entities = 
        orgService.RetrieveMultiple(query).Entities
        .Select(r => (ItemSetConfig)r.ToEntity<pavelkh_advancedmultiselectitemsetconfiguration>())
        .ToList();

      foreach (var entity in entities)
      {
        this.SetDummyAttributeProperties(entity);
      }

      pluginContext.Trace("Serializing configuration data...");
      var result = JsonHelper.SerializeJson(entities);
      pluginContext.Trace("Preparing results...");
      this.PluginContext.SetOutputParameter("Configuration", result);
    }
    
    public void Import()
    {
      var pluginContext = this.PluginContext;
      pluginContext.Trace("Import Configurations...");
      var configuratonParameter = this.PluginContext.GetInputParameter<string>("Configuration");
      var itemSetConfigurations = JsonHelper.DeserializeJson<IList<ItemSetConfig>>(configuratonParameter);
      if (!itemSetConfigurations.Any())
      {
        return;
      }

      var errors = new List<string>();

      var orgService = pluginContext.Service;
      // Create configuration records record by record (not in bulk mode) in order to avoid potential issues related to custom plugin step creation  (related to creating new dummy attributes on the fly)
      foreach (var itemSetConfiguration in itemSetConfigurations)
      {
        try
        {
          orgService.Create((pavelkh_advancedmultiselectitemsetconfiguration)itemSetConfiguration);
        }
        catch (Exception exc)
        {
          errors.Add($"'{itemSetConfiguration.ItemSetName}'. Error:\r\n{exc.InnerException?.Message ?? exc.Message}");
        }
      }

      if (!errors.Any())
      {
        return;
      }

      var importedCount = itemSetConfigurations.Count - errors.Count;
      var errorMessage =
        $"{importedCount} of {itemSetConfigurations.Count} configuration(s) have been imported successfully.\r\n"
        + $"{errors.Count} configurations could not be imported due error(s).\r\nReview error details below for each an Item Set.\r\n------------------\r\n"
        + $"{errors.Aggregate((i,j) => i + "\r\n------------------\r\n" + j)}";
      this.PluginContext.SetOutputParameter("Errors", errorMessage);
    }

    public void PublishAfterRenaming()
    {
      var pluginContext = this.PluginContext;
      pluginContext.Trace("Publishing after Item Set Configuration renaming...");
      var itemSetConfigurationId = this.PluginContext.PrimaryEntityId;
      var ctx = pluginContext.OrgCtx;
      var entityName = ctx.pavelkh_advancedmultiselectitemsetconfigurationSet.Where(
        r => r.pavelkh_advancedmultiselectitemsetconfigurationId == itemSetConfigurationId)
        .Select(r => r.pavelkh_EntityName)
        .FirstOrDefault();
      var service = pluginContext.Service;
      MetadataUtils.PublishEntity(service, entityName);
      var configUpdate = new pavelkh_advancedmultiselectitemsetconfiguration()
                           {
                             pavelkh_advancedmultiselectitemsetconfigurationId = itemSetConfigurationId,
                             pavelkh_PublishRequired = false
                           };
      service.Update(configUpdate);
    }

    private void SetDummyAttributeProperties(ItemSetConfig itemSetConfig)
    {
      var pluginContext = this.PluginContext;
      var attributeMetadata = (StringAttributeMetadata)MetadataUtils.GetAttributeMetadata(
        pluginContext.Service,
        itemSetConfig.EntityName,
        itemSetConfig.DummySavingAttributeName,
        new[] { "SchemaName", "DisplayName", "MaxLength" });
      if (attributeMetadata == null)
      {
        throw new InvalidPluginExecutionException($"Attribute {itemSetConfig.DummySavingAttributeName} of {itemSetConfig.EntityName} is not found in the system.");
      }

      itemSetConfig.DummySavingAttributeDisplayName = attributeMetadata.DisplayName?.LocalizedLabels.FirstOrDefault()?.Label;
      itemSetConfig.DummySavingAttributeSchemaName = attributeMetadata.SchemaName;
      itemSetConfig.DummySavingAttributeLength = attributeMetadata.MaxLength;
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
        PotentialStatusAttributes = new List<AttributeDto>(),
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
        if (attr.MaxLength == null)
        {
          entity.PotentialStatusAttributes.Add(attr);
        }
        else
        {
          entity.Attributes.Add(attr);
        }
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
      int? maxLength = null;
      switch (attributeMetadata.AttributeType)
      {
          case AttributeTypeCode.String:
            maxLength = ((StringAttributeMetadata)attributeMetadata).MaxLength;
            break;

        case AttributeTypeCode.Memo:
          maxLength = ((MemoAttributeMetadata)attributeMetadata).MaxLength;
          break;
      }

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
        MaxLength = maxLength
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

      var selectTextAttributesFilter1 = new MetadataFilterExpression(LogicalOperator.And);
      selectTextAttributesFilter1.Conditions.Add(
        new MetadataConditionExpression("AttributeType", MetadataConditionOperator.In, new []
                                                                                         {
                                                                                           AttributeTypeCode.String,
                                                                                           AttributeTypeCode.Memo,
                                                                                         }));
      selectTextAttributesFilter1.Conditions.Add(
        new MetadataConditionExpression("LogicalName", MetadataConditionOperator.NotIn, new[] { "traversedpath" }));

      var selectTextAttributesFilter2 = new MetadataFilterExpression(LogicalOperator.Or);
      selectTextAttributesFilter2.Conditions.Add(
        new MetadataConditionExpression("IsValidForUpdate", MetadataConditionOperator.Equals, true));
      selectTextAttributesFilter2.Conditions.Add(
        new MetadataConditionExpression("IsPrimaryName", MetadataConditionOperator.Equals, true));

      var selectTextAttributesFilter = new MetadataFilterExpression(LogicalOperator.And);
      selectTextAttributesFilter.Filters.AddRange(selectTextAttributesFilter1, selectTextAttributesFilter2);

      var selectPotentialStatusAttributesFilter = new MetadataFilterExpression(LogicalOperator.And);
      selectPotentialStatusAttributesFilter.Conditions.Add(new MetadataConditionExpression("AttributeType", MetadataConditionOperator.In, new[]
                                                                                         {
                                                                                           AttributeTypeCode.Boolean,
                                                                                           AttributeTypeCode.State
                                                                                         }));
      selectPotentialStatusAttributesFilter.Conditions.Add(
        new MetadataConditionExpression("IsValidForUpdate", MetadataConditionOperator.Equals, true));

      var attributeFilter = new MetadataFilterExpression(LogicalOperator.Or);
      attributeFilter.Filters.AddRange(new List<MetadataFilterExpression>()
                                         {
                                           selectTextAttributesFilter,
                                           selectPotentialStatusAttributesFilter
                                         });

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

    private IList<FormDto> GetFormsDto(string entityLogicalName)
    {
      entityLogicalName = entityLogicalName.Trim().ToLowerInvariant();
      var pluginContext = this.PluginContext;
      var orgCtx = pluginContext.OrgCtx;
      var result = new List<FormDto>();

      var forms =
        orgCtx.SystemFormSet.Where(
          r =>
          r.FormActivationState != null && r.FormActivationState.Value == FormActivationStateActiveCode
          && r.Type != null && r.Type.Value == MainFormTypeCode && r.ObjectTypeCode == entityLogicalName)
          .Select(r => new SystemForm
          {
            FormId = r.FormId,
            Name = r.Name,
            Description = r.Description,
            FormXml = r.FormXml
          }).ToList().OrderBy(r => r.Name);

      foreach (var form in forms)
      {
        var formDto = new FormDto
          {
            Id = form.FormId?.ToString("D"),
            Name = form.Name,
            FullName = $"{form.Name} {form.FormId}",
            Description = form.Description
          };

        var formXml = XDocument.Parse(form.FormXml);
        var tabs = this.GetFormTabsDto(formXml);
        formDto.Tabs = new List<FormTabDto>(tabs);
        result.Add(formDto);
      }

      return result;
    }

    private IList<FormTabDto> GetFormTabsDto(XDocument formXml)
    {
      var result = new List<FormTabDto>();
      // ReSharper disable PossibleNullReferenceException
      var xTabs = formXml.Root.Element("tabs").Elements("tab");
      foreach (var xTab in xTabs)
      {
        var tabNameAttribute = xTab.Attribute("name");
        var name = tabNameAttribute?.Value ?? xTab.Attribute("id").Value;
        var tabDto = new FormTabDto
          {
            Name = name,
            DisplayName = this.GetLocalizedDescription(xTab)
          };
        // ReSharper restore PossibleNullReferenceException

        var sections = this.GetFormTabSections(xTab);
        tabDto.Sections = new List<FormSectionDto>(sections);
        
        result.Add(tabDto);
      }

      return result;
    }

    private IList<FormSectionDto> GetFormTabSections(XElement xTab)
    {
      var result = new List<FormSectionDto>();
      // ReSharper disable PossibleNullReferenceException
      var xSections = xTab.Element("columns").Elements("column").Elements("sections").Elements("section");
      foreach (var xSection in xSections)
      {
        var sectionNameAttribute = xSection.Attribute("name");
        var name = sectionNameAttribute?.Value ?? xSection.Attribute("id").Value;
        var sectionDto = new FormSectionDto
          {
            Name = name,
            DisplayName = this.GetLocalizedDescription(xSection)
          };
        // ReSharper restore PossibleNullReferenceException

        result.Add(sectionDto);
      }

      return result;
    }

    private IList<TemplateDto> GetTemplatesDto()
    {
      var pluginContext = this.PluginContext;
      var orgCtx = pluginContext.OrgCtx;
      const int HtmlType = 1;
      const string CustomTemplateDisplayNameFilter = "MultiSelectTemplate";

      var templates =
        orgCtx.WebResourceSet.Where(
          r =>
          // ReSharper disable once RedundantBoolCompare
          r.IsCustomizable != null && r.IsCustomizable.Value == true 
          && r.IsHidden != null && r.IsHidden.Value == false 
          && r.WebResourceType != null && r.WebResourceType.Value == HtmlType
          && (r.DisplayName.Contains(CustomTemplateDisplayNameFilter)))
          .Select(r => new WebResource
          {
            WebResourceId = r.WebResourceId.Value,
            Name = r.Name,
            DisplayName = r.DisplayName
          }).ToList().OrderBy(r => r.Name);

      return templates.Select(
        template => 
          new TemplateDto
            {
              Id = template.WebResourceId?.ToString("D"),
              Name = template.Name,
              DisplayName = template.DisplayName
            }).ToList();
    }

    private string GetLocalizedDescription(XElement xElement)
    {
      var langCode = this.GetCurrentUserLanguageCode().ToString();
      return xElement.Elements("labels")
          .Elements("label")
          .FirstOrDefault(
            r =>
              r.Attribute("languagecode") != null
              // ReSharper disable PossibleNullReferenceException
              && r.Attribute("languagecode").Value == langCode)?.Attribute("description")?.Value;
    }

    private int GetCurrentUserLanguageCode()
    {
      var pluginContext = this.PluginContext;
      if (this.currentUserLangCode != null)
      {
        return this.currentUserLangCode.Value;
      }

      return (int)(this.currentUserLangCode = UserSettingUtils.GetCurrentUserLanguageCode(pluginContext));
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

      [DataMember]
      public IList<AttributeDto> PotentialStatusAttributes { get; set; }
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
      public int? MaxLength { get; set; }
    }

    [DataContract]
    public class RelationshipDto
    {
      [DataMember]
      public string Relationship { get; set; }

      [DataMember]
      public string ItemSetEntity { get; set; }
    }

    [DataContract]
    public class ActionDto
    {
      [DataMember]
      public string UniqueName { get; set; }

      [DataMember]
      public string Name { get; set; }
    }

    [DataContract]
    public class FormDto
    {
      [DataMember]
      public string Name { get; set; }

      [DataMember]
      public string Id { get; set; }

      [DataMember]
      public string Description { get; set; }

      [DataMember]
      public IList<FormTabDto> Tabs { get; set; }

      [DataMember]
      public string FullName { get; set; }
    }

    [DataContract]
    public class FormTabDto
    {
      [DataMember]
      public string Name { get; set; }

      [DataMember]
      public string DisplayName { get; set; }

      [DataMember]
      public IList<FormSectionDto> Sections { get; set; } 
    }

    [DataContract]
    public class FormSectionDto
    {
      [DataMember]
      public string Name { get; set; }

      [DataMember]
      public string DisplayName { get; set; }
    }

    [DataContract]
    public class TemplateDto
    {
      [DataMember]
      public string Name { get; set; }

      [DataMember]
      public string Id { get; set; }

      [DataMember]
      public string DisplayName { get; set; }
    }
  }

  [DataContract]
  public class ItemSetConfig
  {
    [DataMember]
    public string EntityName { get; set; }

    [DataMember]
    public string ItemSetName { get; set; }

    [DataMember]
    public string RelationshipName { get; set; }

    [DataMember]
    public string DummySavingAttributeName { get; set; }

    [DataMember]
    public string DummySavingAttributeSchemaName { get; set; }

    [DataMember]
    public string DummySavingAttributeDisplayName { get; set; }

    [DataMember]
    public int? DummySavingAttributeLength { get; set; }

    [DataMember]
    public string SaveChangeHandler { get; set; }

    [DataMember]
    public string LabelAttributeName { get; set; }

    [DataMember]
    public string TooltipAttributeName { get; set; }

    [DataMember]
    public bool? AutoprocessItemStatus { get; set; }

    [DataMember]
    public string AutoprocessItemStatusAttributeName { get; set; }

    [DataMember]
    public string Description { get; set; }

    [DataMember]
    public string FetchXmlQuery { get; set; }

    [DataMember]
    public bool? HandleSecurityPrivileges { get; set; }

    public static implicit operator ItemSetConfig(pavelkh_advancedmultiselectitemsetconfiguration configEntity)
    {
      return new ItemSetConfig()
               {
                 EntityName = configEntity.pavelkh_EntityName,
                 ItemSetName = configEntity.pavelkh_ItemSetName,
                 RelationshipName = configEntity.pavelkh_RelationshipName,
                 DummySavingAttributeName = configEntity.pavelkh_DummySavingField,
                 SaveChangeHandler = configEntity.pavelkh_SaveChangesHandler,
                 LabelAttributeName = configEntity.pavelkh_LabelAttributeName,
                 TooltipAttributeName = configEntity.pavelkh_TooltipAttributeName,
                 AutoprocessItemStatus = configEntity.pavelkh_AutoProcessItemStatus,
                 AutoprocessItemStatusAttributeName = configEntity.pavelkh_AutoprocessItemStatusAttributeName,
                 Description = configEntity.pavelkh_Description,
                 FetchXmlQuery = configEntity.pavelkh_FetchXml,
                 HandleSecurityPrivileges = configEntity.pavelkh_HandleSecurityPrivileges
      };
    }

    public static explicit operator pavelkh_advancedmultiselectitemsetconfiguration(ItemSetConfig itemSetConfig)
    {
      return new pavelkh_advancedmultiselectitemsetconfiguration()
               {
                 pavelkh_EntityName = itemSetConfig.EntityName,
                 pavelkh_ItemSetName = itemSetConfig.ItemSetName,
                 pavelkh_RelationshipName = itemSetConfig.RelationshipName,
                 pavelkh_DummySavingField = itemSetConfig.DummySavingAttributeName,
                 pavelkh_NewDummySavingField = itemSetConfig.DummySavingAttributeName,
                 pavelkh_NewDummySavingFieldDisplayName = itemSetConfig.DummySavingAttributeDisplayName,
                 pavelkh_NewDummySavingAttributeLength = itemSetConfig.DummySavingAttributeLength,
                 pavelkh_SaveChangesHandler = itemSetConfig.SaveChangeHandler,
                 pavelkh_LabelAttributeName = itemSetConfig.LabelAttributeName,
                 pavelkh_TooltipAttributeName = itemSetConfig.TooltipAttributeName,
                 pavelkh_AutoProcessItemStatus = itemSetConfig.AutoprocessItemStatus,
                 pavelkh_AutoprocessItemStatusAttributeName = itemSetConfig.AutoprocessItemStatusAttributeName,
                 pavelkh_Description = itemSetConfig.Description,
                 pavelkh_FetchXml = itemSetConfig.FetchXmlQuery,
                 pavelkh_HandleSecurityPrivileges = itemSetConfig.HandleSecurityPrivileges
               };
    }
  }


  #endregion
}