namespace AdvancedMultiSelect.Logic.ItemSet
{
  using System;
  using System.Diagnostics.CodeAnalysis;
  using System.Linq;

  using Microsoft.Crm.Sdk.Messages;
  using Microsoft.Xrm.Sdk;
  using Microsoft.Xrm.Sdk.Query;

  using AdvancedMultiSelect.CrmProxy;

  /// <summary>
  /// A class to handle pavelkh_ProcessChangesForMarketingListItemSet action
  /// <remarks>
  /// This action processes saving changes related to Item Sets Configuration where Marketing Entity relationships are involved 
  /// (they require special requests unlike common N:N relationships) 
  /// </remarks>
  /// </summary>
  [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
  public class CustomItemSetChangeManager : CustomItemSetChangeManagerBase
  {
    private const string MarketingListEntityName = "list";

    public CustomItemSetChangeManager(PluginBase<Entity>.PluginContext pluginContext)
      : base(pluginContext)
    {
    }

    public override void ProcessItemSetChanges()
    {
      var orgContex = this.PluginContext.OrgCtx;
      var savedEntityId = this.EntityRef.Id;
      var savedEntityLogicalName = this.EntityRef.LogicalName;
      var itemSetsConfig =
        orgContex.pavelkh_advancedmultiselectitemsetconfigurationSet.Where(
          r => r.pavelkh_ItemSetName == this.ItemSetName)
          .Select(r => new pavelkh_advancedmultiselectitemsetconfiguration
          {
            pavelkh_EntityName = r.pavelkh_EntityName,
            pavelkh_ItemSetEntityName = r.pavelkh_ItemSetEntityName,
            pavelkh_FetchXmlForIntersect = r.pavelkh_FetchXmlForIntersect
          }).FirstOrDefault();

      if (itemSetsConfig == null)
      {
        throw new NullReferenceException(nameof(itemSetsConfig));
      }

      var fetchXmlQuery = itemSetsConfig.pavelkh_FetchXmlForIntersect.Replace(
        ItemSetBuilder.FetchXmlEntityIdPlaceHolder,
        savedEntityId.ToString("D"));
      var fetchExpression = new FetchExpression(fetchXmlQuery);
      var service = this.PluginContext.Service;
      var entityCollection = service.RetrieveMultiple(fetchExpression);
      var existingSelected = entityCollection.Entities.Select(r => r.Id).ToList();
      var itemsToAssociate = this.SelectedIdList
        .Except(existingSelected)
        .ToArray();
      var itemsToDisassociate = existingSelected
        .Except(this.SelectedIdList)
        .ToArray();
      var associateRequest = new AddListMembersListRequest();
      var disassociateRequest = new RemoveMemberListRequest();
      if (savedEntityLogicalName == MarketingListEntityName)
      {
        if (itemsToAssociate.Any())
        {
          associateRequest.ListId = savedEntityId;
          associateRequest.MemberIds = itemsToAssociate;
          service.Execute(associateRequest);
        }

        disassociateRequest.ListId = savedEntityId;
        foreach (var entityId in itemsToDisassociate)
        {
          disassociateRequest.EntityId = entityId;
          service.Execute(disassociateRequest);
        }
      }
      else
      {
        associateRequest.MemberIds = new[] { savedEntityId };
        foreach (var listId in itemsToAssociate)
        {
          associateRequest.ListId = listId;
          service.Execute(associateRequest);
        }

        disassociateRequest.EntityId = savedEntityId;
        foreach (var listId in itemsToDisassociate)
        {
          disassociateRequest.ListId = listId;
          service.Execute(disassociateRequest);
        }
      }
    }
  }
}