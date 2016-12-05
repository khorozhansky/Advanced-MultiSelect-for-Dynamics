namespace TuneMultiSelect.Logic.TuneMultiSelect
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;
  using System.Linq;
  using Microsoft.Crm.Sdk.Messages;
  using Microsoft.Xrm.Sdk;

  /// <summary>
  /// A class to handle tunexrm_ProcessChangesForMarketingListItemSet action
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

      IList<Guid> existingSelected;
      if (savedEntityLogicalName == MarketingListEntityName)
      {
        existingSelected = orgContex.ListMemberSet
          .Where(r => r.ListId.Id == savedEntityId)
          .Select(r => r.EntityId.Id)
          .ToList();
      }
      else
      {
        existingSelected = orgContex.ListMemberSet
          .Where(r => r.EntityId.Id == savedEntityId)
          .Select(r => r.ListId.Id)
          .ToList();
      }

      var idsToAssociate = this.SelectedIdList
        .Except(existingSelected)
        .ToArray();

      var idsToDisassociate = this.UnselectedIdList
        .Intersect(existingSelected)
        .ToList();

      var associateRequest = new AddListMembersListRequest();
      var disassociateRequest = new RemoveMemberListRequest();

      var service = this.PluginContext.Service;

      if (savedEntityLogicalName == MarketingListEntityName)
      {
        if (idsToAssociate.Any())
        {
          associateRequest.ListId = savedEntityId;
          associateRequest.MemberIds = idsToAssociate;
          service.Execute(associateRequest);
        }

        disassociateRequest.ListId = savedEntityId;
        foreach (var entityId in idsToDisassociate)
        {
          disassociateRequest.EntityId = entityId;
          service.Execute(disassociateRequest);
        }
      }
      else
      {
        associateRequest.MemberIds = new[] { savedEntityId };
        foreach (var listId in idsToAssociate)
        {
          associateRequest.ListId = listId;
          service.Execute(associateRequest);
        }

        disassociateRequest.EntityId = savedEntityId;
        foreach (var listId in idsToDisassociate)
        {
          disassociateRequest.ListId = listId;
          service.Execute(disassociateRequest);
        }
      }
    }
  }
}