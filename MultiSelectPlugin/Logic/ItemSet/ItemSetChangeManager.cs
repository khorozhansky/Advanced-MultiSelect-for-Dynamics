namespace AdvancedMultiSelect.Logic.ItemSet
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  using AdvancedMultiSelect.CrmProxy;

  using Microsoft.Xrm.Sdk;
  using Microsoft.Xrm.Sdk.Messages;
  using Microsoft.Xrm.Sdk.Query;

  public class ItemSetChangeManager : ManagerBase<Entity>
  {
    public const string CustomSaveHandlerItemSetParamName = "ItemSetName";
    public const string CustomSaveHandlerEntityRefParamName = "EntityRef";
    public const string CustomSaveHandlerSelectedIdListParamName = "SelectedIdList";
    public const string CustomSaveHandlerUnselectedIdListParamName = "UnselectedIdList";

    /// <summary>
    /// The shared variable key prefix.
    /// </summary>
    private const string SharedVariableKeyPrefix = "multiselect_itemset_";

    /// <summary>
    /// The separator used to save data into the "fake" fields 
    /// Splits identifiers
    /// </summary>
    private static readonly char[] ItemSeparator = new[] { ';' };

    /// <summary>
    /// Determines if the Create and Update messages of the entity should be processed by the logic
    /// </summary>
    public bool ProcessSave { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemSetChangeManager"/> class. 
    /// </summary>
    /// <param name="pluginContext">
    /// The local context.
    /// </param>
    public ItemSetChangeManager(PluginBase<Entity>.PluginContext pluginContext)
      : base(pluginContext)
    {
      this.InitItemSetConfig();
    }

    /// <summary>
    /// Parses the identifier list.
    /// </summary>
    /// <param name="idList">The identifier list.</param>
    /// <returns>a list of identifiers</returns>
    public static IList<Guid> ParseIdList(string idList)
    {
      if (idList.Equals("-"))
      {
        return new List<Guid>();
      }

      var stringList = idList.Split(ItemSeparator, StringSplitOptions.RemoveEmptyEntries);
      return stringList.Select(r => new Guid(r)).ToList();
    } 

    /// <summary>
    /// Gets or sets the item sets configurations associated with the currently processed entity.
    /// </summary>
    /// <value>The item sets.</value>
    public IList<pavelkh_advancedmultiselectitemsetconfiguration> ItemSetsConfigs { get; set; }

    /// <summary>
    /// The prepare relations update.
    /// </summary>
    public void PrepareRelationsUpdate()
    {
      if (!this.ProcessSave)
      {
        return;
      }

      foreach (var item in this.ItemSetsConfigs)
      {
        this.SaveFakeFieldValue(item);
      }
    }

    /// <summary>
    /// The update relations.
    /// </summary>
    public void UpdateRelations()
    {
      if (!this.ProcessSave)
      {
        return;
      }

      foreach (var item in this.ItemSetsConfigs)
      {
        this.ProcessItemSetChanges(item);
      }
    }

    /// <summary>
    /// The save fake field value.
    /// </summary>
    /// <param name="itemSetConfig">
    /// The item.
    /// </param>
    private void SaveFakeFieldValue(pavelkh_advancedmultiselectitemsetconfiguration itemSetConfig)
    {
      var target = this.PluginContext.InputTargetAsEntity;
      var fakeFieldName = itemSetConfig.pavelkh_DummySavingField;
      if (string.IsNullOrWhiteSpace(fakeFieldName))
      {
        throw new InvalidPluginExecutionException($"There is an error in configuration for '{ itemSetConfig.pavelkh_ItemSetName }' entity set. The dummy field is not specifed");
      }

      if (!target.Contains(fakeFieldName))
      {
        return;
      }

      var ids = target.GetAttributeValue<string>(fakeFieldName);
      var sharedVariableKey = SharedVariableKeyPrefix + fakeFieldName;
      this.PluginContext.SetSharedVariable(sharedVariableKey, ids);
      target[fakeFieldName] = null;
    }

    /// <summary>
    /// The process check box set changes.
    /// </summary>
    /// <param name="itemSetConfig">The check box set.</param>
    private void ProcessItemSetChanges(pavelkh_advancedmultiselectitemsetconfiguration itemSetConfig)
    {
      var pluginContext = this.PluginContext;
      var fakeFieldName = itemSetConfig.pavelkh_DummySavingField;
      var sharedVariableKey = SharedVariableKeyPrefix + fakeFieldName;
      var value = pluginContext.GetSharedVariable(sharedVariableKey, true);

      var ids = value?.ToString();
      if (string.IsNullOrWhiteSpace(ids))
      {
        return;
      }

      var customHandlerSpecified = !string.IsNullOrWhiteSpace(itemSetConfig.pavelkh_SaveChangesHandler);
      var isCustomRelationship = itemSetConfig.pavelkh_IsCustomRelationship ?? false;
      if (isCustomRelationship || !customHandlerSpecified)
      {
        this.ProcessItemSetChangesForCustomRelationship(itemSetConfig, ids);
      }
      else
      {
        this.ProcessItemSetChangesByCustomHandler(itemSetConfig, ids);
      }
    }

    /// <summary>
    /// Processes the item set changes for custom relationship.
    /// </summary>
    /// <param name="itemSetConfig">The item set.</param>
    /// <param name="selectedIds">The selected ids.</param>
    /// 
    private void ProcessItemSetChangesForCustomRelationship(pavelkh_advancedmultiselectitemsetconfiguration itemSetConfig, string selectedIds)
    {
      var relationshipName = itemSetConfig.pavelkh_RelationshipName;
      var selectedIdList = ParseIdList(selectedIds);
      var inputTarget = this.PluginContext.InputTargetAsEntity;
      var entity1LogicalName = inputTarget.LogicalName;
      var entity2LogicalName = 
        itemSetConfig.pavelkh_EntityName == entity1LogicalName ? 
        itemSetConfig.pavelkh_ItemSetEntityName : 
        itemSetConfig.pavelkh_EntityName;
      var fetchXmlQuery = itemSetConfig.pavelkh_FetchXmlForIntersect.Replace(
        ItemSetBuilder.FetchXmlEntityIdPlaceHolder,
        inputTarget.Id.ToString("D"));
      var fetchExpression = new FetchExpression(fetchXmlQuery);
      var service = this.PluginContext.Service;
      var entityCollection = service.RetrieveMultiple(fetchExpression);
      var existingSelected = entityCollection.Entities.Select(r => r.Id).ToList();
      var itemsToAssociate = selectedIdList
        .Except(existingSelected)
        .Select(r => new EntityReference(entity2LogicalName, r))
        .ToList();
      this.Associate(relationshipName, itemsToAssociate);

      var itemsToDisassociate = existingSelected
        .Except(selectedIdList)
        .Select(r => new EntityReference(entity2LogicalName, r))
        .ToList();
      this.Disassociate(relationshipName, itemsToDisassociate);
    }

    /// <summary>
    /// Processes the item set changes for out-of-box relationship by custom handler.
    /// </summary>
    /// <param name="itemSetConfig">The item set.</param>
    /// <param name="selectedIds">The selected ids.</param>
    /// 
    private void ProcessItemSetChangesByCustomHandler(pavelkh_advancedmultiselectitemsetconfiguration itemSetConfig, string selectedIds)
    {
      var handlerActionName = itemSetConfig.pavelkh_SaveChangesHandler;

      var request = new OrganizationRequest(handlerActionName)
      {
        [CustomSaveHandlerItemSetParamName] = itemSetConfig.pavelkh_ItemSetName,
        [CustomSaveHandlerEntityRefParamName] = this.PluginContext.PrimaryEntityRef,
        [CustomSaveHandlerSelectedIdListParamName] = selectedIds
      };

      var service = this.PluginContext.Service;
      service.Execute(request);
    }

    /// <summary>
    /// The associate.
    /// </summary>
    /// <param name="relationshipName">
    /// The relationship name.
    /// </param>
    /// <param name="ids">
    /// The ids.
    /// </param>
    private void Associate(string relationshipName, IList<EntityReference> ids)
    {
      if (ids == null || ids.Count == 0)
      {
        return;
      }

      var inputTarget = this.PluginContext.InputTargetAsEntity;
      var request = new AssociateRequest
      {
        Target = inputTarget.ToEntityReference(), 
        Relationship = new Relationship(relationshipName), 
        RelatedEntities = new EntityReferenceCollection(ids)
      };

      var entity1LogicalName = inputTarget.LogicalName;
      // ReSharper disable once PossibleNullReferenceException
      var entity2LogicalName = ids.FirstOrDefault().LogicalName;
      var selfRelation = entity1LogicalName.Equals(entity2LogicalName, StringComparison.InvariantCultureIgnoreCase);
      if (selfRelation)
      {
        request.Relationship.PrimaryEntityRole = EntityRole.Referenced;
      }

      var service = this.PluginContext.Service;
      service.Execute(request);
      if (!selfRelation)
      {
        return;
      }

      request.Relationship.PrimaryEntityRole = EntityRole.Referencing;
      request.RelatedEntities = new EntityReferenceCollection(ids.Where(r => r.Id != inputTarget.Id).ToList());
      service.Execute(request);
    }

    /// <summary>
    /// The disassociate.
    /// </summary>
    /// <param name="relationshipName">
    /// The relationship name.
    /// </param>
    /// <param name="ids">
    /// The ids.
    /// </param>
    private void Disassociate(string relationshipName, IList<EntityReference> ids)
    {
      if (ids == null || ids.Count == 0)
      {
        return;
      }

      var inputTarget = this.PluginContext.InputTargetAsEntity;
      var request = new DisassociateRequest
      {
        Target = inputTarget.ToEntityReference(), 
        Relationship = new Relationship(relationshipName), 
        RelatedEntities = new EntityReferenceCollection(ids)
      };

      var entity1LogicalName = inputTarget.LogicalName;
      // ReSharper disable once PossibleNullReferenceException
      var entity2LogicalName = ids.FirstOrDefault().LogicalName;
      var selfRelation = entity1LogicalName.Equals(entity2LogicalName, StringComparison.InvariantCultureIgnoreCase);
      if (selfRelation)
      {
        request.Relationship.PrimaryEntityRole = EntityRole.Referenced;
      }

      var service = this.PluginContext.Service;
      service.Execute(request);
      if (!selfRelation)
      {
        return;
      }

      request.Relationship.PrimaryEntityRole = EntityRole.Referencing;
      service.Execute(request);
    }


    private void InitItemSetConfig()
    {
      var pluginContext = this.PluginContext;
      const int DepthThreshold = 1;
      var depth = pluginContext.ExecContext.Depth;
      if (depth > DepthThreshold)
      {
        return;
      }

      var entityName = pluginContext.InputTargetAsEntity.LogicalName;
      var ctx = pluginContext.OrgCtxAsSystemUser;
      this.ItemSetsConfigs =
        ctx.pavelkh_advancedmultiselectitemsetconfigurationSet .Where(
          r => r.pavelkh_EntityName == entityName)
          .Select(r => new pavelkh_advancedmultiselectitemsetconfiguration
          {
            pavelkh_advancedmultiselectitemsetconfigurationId = r.pavelkh_advancedmultiselectitemsetconfigurationId,
            pavelkh_ItemSetName = r.pavelkh_ItemSetName,
            pavelkh_EntityName = r.pavelkh_EntityName,
            pavelkh_ItemSetEntityName = r.pavelkh_ItemSetEntityName,
            pavelkh_RelationshipName= r.pavelkh_RelationshipName,
            pavelkh_IsCustomRelationship = r.pavelkh_IsCustomRelationship,
            pavelkh_DummySavingField = r.pavelkh_DummySavingField,
            pavelkh_SaveChangesHandler = r.pavelkh_SaveChangesHandler,
            pavelkh_FetchXmlForIntersect = r.pavelkh_FetchXmlForIntersect
          }).ToList();

      this.ProcessSave = this.ItemSetsConfigs.Any();
    }
  }
}
