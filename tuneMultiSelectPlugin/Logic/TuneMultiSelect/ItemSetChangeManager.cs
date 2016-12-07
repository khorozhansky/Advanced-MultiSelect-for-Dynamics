namespace TuneMultiSelect.Logic.TuneMultiSelect
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  using Entities;

  using Microsoft.Xrm.Sdk;
  using Microsoft.Xrm.Sdk.Messages;

  public class ItemSetChangeManager : ManagerBase<Entity>
  {
    public const string CustomSaveHandlerItemSetParamName = "ItemSetName";
    public const string CustomSaveHandlerEntityRefParamName = "EntityRef";
    public const string CustomSaveHandlerSelectedIdListParamName = "SelectedIdList";
    public const string CustomSaveHandlerUnselectedIdListParamName = "UnselectedIdList";

    /// <summary>
    /// The shared variable key prefix.
    /// </summary>
    private const string SharedVariableKeyPrefix = "tune_itemset_";

    /// <summary>
    /// The separator used to save data into the "fake" fields 
    /// Splits the sets of selected and unselected identifiers
    /// </summary>
    private static readonly char[] PairSeparator = { '|' };

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
      var stringList = idList.Split(ItemSeparator, StringSplitOptions.RemoveEmptyEntries);
      return stringList.Select(r => new Guid(r)).ToList();
    } 

    /// <summary>
    /// Gets or sets the item sets configurations associated with the currently processed entity.
    /// </summary>
    /// <value>The item sets.</value>
    public IList<tunexrm_tunemultiselectitemsetconfiguration> ItemSetsConfigs { get; set; }

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
    private void SaveFakeFieldValue(tunexrm_tunemultiselectitemsetconfiguration itemSetConfig)
    {
      var target = this.PluginContext.InputTargetAsEntity;
      var fakeFieldName = itemSetConfig.tunexrm_DummySavingField;
      if (string.IsNullOrWhiteSpace(fakeFieldName))
      {
        throw new InvalidPluginExecutionException($"There is an error in configuration for '{ itemSetConfig.tunexrm_ItemSetName }' entity set. The dummy field is not specifed");
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
    private void ProcessItemSetChanges(tunexrm_tunemultiselectitemsetconfiguration itemSetConfig)
    {
      var pluginContext = this.PluginContext;
      var fakeFieldName = itemSetConfig.tunexrm_DummySavingField;
      var sharedVariableKey = SharedVariableKeyPrefix + fakeFieldName;
      var value = pluginContext.GetSharedVariable(sharedVariableKey, true);

      var ids = value?.ToString();
      if (string.IsNullOrWhiteSpace(ids))
      {
        return;
      }

      var idsPair = ids.Split(PairSeparator);
      if (idsPair.Length != 2)
      {
        return;
      }

      var selectedIds = idsPair[0];
      var unselectedIds = idsPair[1];
      var customHandlerSpecified = !string.IsNullOrWhiteSpace(itemSetConfig.tunexrm_SaveChangesHandler);
      var isCustomRelationship = itemSetConfig.tunexrm_IsCustomRelationship ?? false;
      if (isCustomRelationship || !customHandlerSpecified)
      {
        this.ProcessItemSetChangesForCustomRelationship(itemSetConfig, selectedIds, unselectedIds);
      }
      else
      {
        this.ProcessItemSetChangesByCustomHandler(itemSetConfig, selectedIds, unselectedIds);
      }
    }

    /// <summary>
    /// Processes the item set changes for custom relationship.
    /// </summary>
    /// <param name="itemSetConfig">The item set.</param>
    /// <param name="selectedIds">The selected ids.</param>
    /// <param name="unselectedIds">The unselected ids.</param>
    private void ProcessItemSetChangesForCustomRelationship(tunexrm_tunemultiselectitemsetconfiguration itemSetConfig, string selectedIds, string unselectedIds)
    {
      var relationshipName = itemSetConfig.tunexrm_RelationshipName;
      var selectedIdList = ParseIdList(selectedIds);
      var unselectedIdList = ParseIdList(unselectedIds);

      var orgContex = this.PluginContext.OrgCtx;
      var inputTarget = this.PluginContext.InputTargetAsEntity;
      var entity1LogicalName = inputTarget.LogicalName;
      string entity2LogicalName;

      string entity1IntersectAttribute;
      string entity2IntersectAttribute;
      if (itemSetConfig.tunexrm_EntityName == entity1LogicalName)
      {
        entity1IntersectAttribute = itemSetConfig.tunexrm_EntityAttributeName;
        entity2LogicalName = itemSetConfig.tunexrm_ItemSetEntityName;
        entity2IntersectAttribute = itemSetConfig.tunexrm_ItemSetEntityAttributeName;
      }
      else
      {
        entity1IntersectAttribute = itemSetConfig.tunexrm_ItemSetEntityAttributeName;
        entity2LogicalName = itemSetConfig.tunexrm_EntityName;
        entity2IntersectAttribute = itemSetConfig.tunexrm_EntityAttributeName;
      }

      var intersectEntityName = itemSetConfig.tunexrm_IntersectEntityName;
      var existingSelected = orgContex
        .CreateQuery(intersectEntityName)
        .Where(r => r[entity1IntersectAttribute] != null && ((Guid)r[entity1IntersectAttribute]) == inputTarget.Id)
        .Select(r => (Guid)r[entity2IntersectAttribute])
        .ToList();

      var itemsToAssociate = selectedIdList
        .Except(existingSelected)
        .Select(r => new EntityReference(entity2LogicalName, r))
        .ToList();

      this.Associate(relationshipName, itemsToAssociate);

      var itemsToDisassociate = unselectedIdList
        .Intersect(existingSelected)
        .Select(r => new EntityReference(entity2LogicalName, r))
        .ToList();

      this.Disassociate(relationshipName, itemsToDisassociate);
    }

    /// <summary>
    /// Processes the item set changes for out-of-box relationship by custom handler.
    /// </summary>
    /// <param name="itemSetConfig">The item set.</param>
    /// <param name="selectedIds">The selected ids.</param>
    /// <param name="unselectedIds">The unselected ids.</param>
    private void ProcessItemSetChangesByCustomHandler(tunexrm_tunemultiselectitemsetconfiguration itemSetConfig, string selectedIds, string unselectedIds)
    {
      var handlerActionName = itemSetConfig.tunexrm_SaveChangesHandler;

      var request = new OrganizationRequest(handlerActionName)
      {
        [CustomSaveHandlerItemSetParamName] = itemSetConfig.tunexrm_ItemSetName,
        [CustomSaveHandlerEntityRefParamName] = this.PluginContext.PrimaryEntityRef,
        [CustomSaveHandlerSelectedIdListParamName] = selectedIds,
        [CustomSaveHandlerUnselectedIdListParamName] = unselectedIds
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

      var service = this.PluginContext.Service;
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

      var service = this.PluginContext.Service;
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

      const string WebResourceEntityName = "webresource";
      if (pluginContext.PrimaryEntityName.Equals(WebResourceEntityName))
      {
        return;
      }

      var entityName = pluginContext.InputTargetAsEntity.LogicalName;
      var ctx = pluginContext.OrgCtxAsSystemUser;
      this.ItemSetsConfigs =
        ctx.tunexrm_tunemultiselectitemsetconfigurationSet .Where(
          r => r.tunexrm_EntityName == entityName)
          .Select(r => new tunexrm_tunemultiselectitemsetconfiguration
          {
            tunexrm_tunemultiselectitemsetconfigurationId = r.tunexrm_tunemultiselectitemsetconfigurationId,
            tunexrm_ItemSetName = r.tunexrm_ItemSetName,
            tunexrm_EntityName = r.tunexrm_EntityName,
            tunexrm_EntityAttributeName = r.tunexrm_EntityAttributeName,
            tunexrm_ItemSetEntityName = r.tunexrm_ItemSetEntityName,
            tunexrm_ItemSetEntityAttributeName = r.tunexrm_ItemSetEntityAttributeName,
            tunexrm_IntersectEntityName = r.tunexrm_IntersectEntityName,
            tunexrm_RelationshipName= r.tunexrm_RelationshipName,
            tunexrm_IsCustomRelationship = r.tunexrm_IsCustomRelationship,
            tunexrm_LabelAttributeName = r.tunexrm_LabelAttributeName,
            tunexrm_DummySavingField = r.tunexrm_DummySavingField,
            tunexrm_SaveChangesHandler = r.tunexrm_SaveChangesHandler
          }).ToList();

      this.ProcessSave = this.ItemSetsConfigs.Any();
    }
  }
}
