namespace TuneMultiSelect.Logic.TuneMultiSelect
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.Serialization;
  using Microsoft.Xrm.Sdk;
  using Microsoft.Xrm.Sdk.Query;

  using Utils;
  using Entities;

  public class ItemSetBuilder : ManagerBase<Entity>
  {
    private const string EntityLogicalNameParamName = "EntityLogicalName";
    private const string RecordIdParamName = "RecordId";
    private const string ItemSetNameParamName = "ItemSetName";
    private const string ItemsParamName = "Items";
    private const string SavingAttributeLogicalNameParamName = "SavingAttributeLogicalName";

    private readonly string entityLogicalName;

    private readonly string recordIdString;

    private readonly string itemSetName;

    public ItemSetBuilder(PluginBase<Entity>.PluginContext pluginContext)
      : base(pluginContext)
    {
      this.entityLogicalName = this.PluginContext.GetInputParameter<string>(EntityLogicalNameParamName);
      this.recordIdString = this.PluginContext.GetInputParameter<string>(RecordIdParamName);
      this.itemSetName = this.PluginContext.GetInputParameter<string>(ItemSetNameParamName);

      if (string.IsNullOrWhiteSpace(this.itemSetName))
      {
        throw new InvalidPluginExecutionException($"{ ItemSetNameParamName } parameter is not provided. Check if the Web Resource Properties specified correctly ('Custom Parameter(data)' parameter).");
      }

      if (string.IsNullOrWhiteSpace(this.entityLogicalName))
      {
        throw new InvalidPluginExecutionException($"{ EntityLogicalNameParamName } parameter is not provided. Check if the Web Resource Properties has 'Pass record object-type code and unique identifier as parameters' checkbox checked.");
      }

      if (string.IsNullOrWhiteSpace(this.recordIdString))
      {
        throw new InvalidPluginExecutionException($"{ RecordIdParamName } parameter is not provided. Check if the Web Resource Properties has 'Pass record object-type code and unique identifier as parameters' checkbox checked.");
      }

      this.InitItemSetConfig();
    }

    /// <summary>
    /// Gets or sets the item sets configuration.
    /// </summary>
    /// <value>
    /// The item sets configuration.
    /// </value>
    private tunexrm_tunemultiselectitemsetconfiguration ItemSetConfig { get; set; }

    public void BuildItemSet()
    {
      var items = this.GetItemSet();
      this.PluginContext.SetOutputParameter(ItemsParamName, items);
      this.PluginContext.SetOutputParameter(SavingAttributeLogicalNameParamName, this.ItemSetConfig.tunexrm_DummySavingField);
    }

    public string GetItemSet()
    {
      var records =  this.GetItemSetForNnRelationship();
      return JsonHelper.SerializeJson(records);
    }

    private void InitItemSetConfig()
    {
      var ctx = this.PluginContext.OrgCtxAsSystemUser;
      this.ItemSetConfig =
        ctx.tunexrm_tunemultiselectitemsetconfigurationSet .Where(
          r => r.tunexrm_EntityName == this.entityLogicalName && r.tunexrm_ItemSetName == this.itemSetName)
          .Select(r => new tunexrm_tunemultiselectitemsetconfiguration
                         {
                           tunexrm_tunemultiselectitemsetconfigurationId = r.tunexrm_tunemultiselectitemsetconfigurationId,
                           tunexrm_FetchXml = r.tunexrm_FetchXml,
                           tunexrm_IntersectEntityName = r.tunexrm_IntersectEntityName,
                           tunexrm_EntityName = r.tunexrm_EntityName,
                           tunexrm_EntityAttributeName = r.tunexrm_EntityAttributeName,
                           tunexrm_ItemSetEntityName = r.tunexrm_ItemSetEntityName,
                           tunexrm_ItemSetEntityAttributeName = r.tunexrm_ItemSetEntityAttributeName,
                           tunexrm_DummySavingField = r.tunexrm_DummySavingField,
                           tunexrm_LabelAttributeName = r.tunexrm_LabelAttributeName
          })
          .FirstOrDefault();

      if (this.ItemSetConfig != null)
      {
        return;
      }

      var errorMessage = $"An error occurred while getting the list of items. '{this.itemSetName}' Item Set for {this.entityLogicalName} entity is not found in the configuration.";
      throw new InvalidPluginExecutionException(errorMessage);
    }

    /// <summary>
    /// Gets the item set for N:N relationship.
    /// </summary>
    /// <returns></returns>
    private IList<OptionItem> GetItemSetForNnRelationship()
    {
      var fetchXml = this.ItemSetConfig.tunexrm_FetchXml;
      var intersectEntityName = this.ItemSetConfig.tunexrm_IntersectEntityName;
      var linkedEntityAlias = intersectEntityName + "_itemset";

      string entity1IntersectAttribute;
      string entity2IntersectAttribute;
      if (this.entityLogicalName == this.ItemSetConfig.tunexrm_EntityName)
      {
        entity1IntersectAttribute = this.ItemSetConfig.tunexrm_EntityAttributeName;
        entity2IntersectAttribute = this.ItemSetConfig.tunexrm_ItemSetEntityAttributeName;
      }
      else
      {
        entity1IntersectAttribute = this.ItemSetConfig.tunexrm_ItemSetEntityAttributeName;
        entity2IntersectAttribute = this.ItemSetConfig.tunexrm_EntityAttributeName;
      }

      var linkedEntitySelectControlField = linkedEntityAlias + "." + entity1IntersectAttribute;
      var recordId = this.recordIdString;
      const string LinkedEntityXmlInjectionFormat = 
        "  <link-entity name='{0}' alias='{1}' from='{2}' to='{2}' link-type='outer' visible='false' intersect='true'>" +
        "    <attribute name='{3}'/>" +
        "    <filter type='and'>" +
        "      <condition attribute='{3}' operator='eq' value='{4}'/>" +
        "    </filter>" +
        "  </link-entity>" +
        "</entity>";

      var linkedEntityXml =
        string.Format(
          LinkedEntityXmlInjectionFormat,
          intersectEntityName, 
          linkedEntityAlias, 
          entity2IntersectAttribute, 
          entity1IntersectAttribute,
          recordId);

      var fetchXmlToExecute = fetchXml.Replace("</entity>", linkedEntityXml);
      var service = this.PluginContext.Service;
      var fetchExpression = new FetchExpression(fetchXmlToExecute);
      var entityCollection = service.RetrieveMultiple(fetchExpression);
      var entities = entityCollection.Entities;
      var labelFieldName = this.ItemSetConfig.tunexrm_LabelAttributeName;

      return entities.Select(
        entity => 
          new OptionItem
            {
              Id = entity.Id,
              Label = entity.GetAttributeValue<string>(labelFieldName),
              Selected = entity.GetAttributeValue<AliasedValue>(linkedEntitySelectControlField) != null
            }).ToList();
    }

    [DataContract]
    public class OptionItem
    {
      [DataMember]
      public Guid Id { get; set; }

      [DataMember]
      public string Label { get; set; }

      [DataMember]
      public bool Selected { get; set; }
    }
  }
}