namespace AdvancedMultiSelect.Logic.ItemSet
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.Serialization;

  using AdvancedMultiSelect.CrmProxy;
  using Utils;

  using Microsoft.Crm.Sdk.Messages;
  using Microsoft.Xrm.Sdk;
  using Microsoft.Xrm.Sdk.Messages;
  using Microsoft.Xrm.Sdk.Query;

  public class ItemSetBuilder : ManagerBase<Entity>
  {
    public const string FetchXmlEntityIdPlaceHolder = "{00000000-0000-0000-0000-000000000000}";
    private const string EntityLogicalNameParamName = "EntityLogicalName";
    private const string RecordIdParamName = "RecordId";
    private const string ItemSetNameParamName = "ItemSetName";
    private const string ItemsParamName = "Items";
    private const string SavingAttributeLogicalNameParamName = "SavingAttributeLogicalName";
    private const string AllowUpdateParamName = "AllowUpdate";

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

      this.InitItemSetConfig();
    }

    /// <summary>
    /// Gets or sets the item sets configuration.
    /// </summary>
    /// <value>
    /// The item sets configuration.
    /// </value>
    private pavelkh_advancedmultiselectitemsetconfiguration ItemSetConfig { get; set; }

    public void BuildItemSet()
    {
      var records = this.GetItemSetForNnRelationship();
      var items = JsonHelper.SerializeJson(records);
      this.PluginContext.SetOutputParameter(ItemsParamName, items);
      this.PluginContext.SetOutputParameter(SavingAttributeLogicalNameParamName, this.ItemSetConfig.pavelkh_DummySavingField);

      var allowUpdate = this.GetUserCanUpdateItemSet(records);
      this.PluginContext.SetOutputParameter(AllowUpdateParamName, allowUpdate);
    }

    private void InitItemSetConfig()
    {
      this.PluginContext.Trace("Initiating Item Set Config...");
      var ctx = this.PluginContext.OrgCtxAsSystemUser;
      this.ItemSetConfig =
        ctx.pavelkh_advancedmultiselectitemsetconfigurationSet.Where(
          r => r.pavelkh_EntityName == this.entityLogicalName && r.pavelkh_ItemSetName == this.itemSetName)
          .Select(r => new pavelkh_advancedmultiselectitemsetconfiguration
                         {
                           pavelkh_advancedmultiselectitemsetconfigurationId = r.pavelkh_advancedmultiselectitemsetconfigurationId,
                           pavelkh_FetchXml = r.pavelkh_FetchXml,
                           pavelkh_FetchXmlForEditMode = r.pavelkh_FetchXmlForEditMode,
                           pavelkh_IntersectEntityName = r.pavelkh_IntersectEntityName,
                           pavelkh_IntersectEntityRefAttributeName = r.pavelkh_IntersectEntityRefAttributeName,
                           pavelkh_EntityName = r.pavelkh_EntityName,
                           pavelkh_EntityAttributeName = r.pavelkh_EntityAttributeName,
                           pavelkh_ItemSetEntityName = r.pavelkh_ItemSetEntityName,
                           pavelkh_ItemSetEntityAttributeName = r.pavelkh_ItemSetEntityAttributeName,
                           pavelkh_DummySavingField = r.pavelkh_DummySavingField,
                           pavelkh_LabelAttributeName = r.pavelkh_LabelAttributeName,
                           pavelkh_TooltipAttributeName = r.pavelkh_TooltipAttributeName,
                           pavelkh_HandleSecurityPrivileges = r.pavelkh_HandleSecurityPrivileges,
                           pavelkh_EntityHasOrganizationOwnership = r.pavelkh_EntityHasOrganizationOwnership,
                           pavelkh_ItemSetEntityHasOrganizationOwnership = r.pavelkh_ItemSetEntityHasOrganizationOwnership
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
    /// Determines if the current user has enough rights (both from the current record and Item Set entity sides) to save changes
    ///  </summary>
    /// <remarks>
    /// 1. TODO: Refactor content of this method!
    /// 2. The method uses ExecuteMultipleRequest to improve performance a bit 
    /// </remarks>
    /// <param name="items">List of <seealso cref="OptionItem"/> items</param>
    /// <returns>True if the current user has enough rights to save changes</returns>
    private bool GetUserCanUpdateItemSet(IList<OptionItem> items)
    {
      this.PluginContext.Trace("Figuring out if the current user can save changes...");
      var item = items.FirstOrDefault();
      if (item == null)
      {
        return false;
      }

      var itemSetConfig = this.ItemSetConfig;
      var handleSecurityPrivileges = itemSetConfig.pavelkh_HandleSecurityPrivileges ?? false;
      if (!handleSecurityPrivileges)
      {
        return true;
      }

      var addNewRecordMode = string.IsNullOrWhiteSpace(this.recordIdString);
      var currentUserRef = new EntityReference(SystemUser.EntityLogicalName, this.PluginContext.ExecContext.UserId);
      var entityHasOrganizationOwnership = itemSetConfig.pavelkh_EntityHasOrganizationOwnership ?? false;
      var itemSetEntityHasOrganizationOwnership = itemSetConfig.pavelkh_ItemSetEntityHasOrganizationOwnership ?? false;
      var retrievePrivileges = entityHasOrganizationOwnership || itemSetEntityHasOrganizationOwnership;
      var request = new ExecuteMultipleRequest
                      {
                        Requests = new OrganizationRequestCollection(),
                        Settings = new ExecuteMultipleSettings()
                                     {
                                       ContinueOnError = false,
                                       ReturnResponses = true
                                     }
                      };
      var privilegesToRetrieve = new List<string>();
      if (entityHasOrganizationOwnership)
      {
        privilegesToRetrieve.Add($"prvWrite{itemSetConfig.pavelkh_EntityName}");
        privilegesToRetrieve.Add($"prvAppend{itemSetConfig.pavelkh_EntityName}");
        privilegesToRetrieve.Add($"prvAppendTo{itemSetConfig.pavelkh_EntityName}");
      }
      else
      {
        if (!addNewRecordMode)
        {
          var recordId = new Guid(this.recordIdString);
          var recordRef = new EntityReference(this.entityLogicalName, recordId);
          request.Requests.Add(new RetrievePrincipalAccessRequest
          {
            Principal = currentUserRef,
            Target = recordRef
          });
        }
      }

      if (itemSetEntityHasOrganizationOwnership)
      {
        privilegesToRetrieve.Add($"prvAppend{itemSetConfig.pavelkh_ItemSetEntityName}");
        privilegesToRetrieve.Add($"prvAppendTo{itemSetConfig.pavelkh_ItemSetEntityName}");
      }
      else
      {
        var itemRef = new EntityReference(this.ItemSetConfig.pavelkh_ItemSetEntityName, item.Id);
        request.Requests.Add(new RetrievePrincipalAccessRequest
        {
          Principal = currentUserRef,
          Target = itemRef,
        });
      }

      if (retrievePrivileges)
      {
        var privilegeQuery = new QueryExpression("privilege")
        {
          NoLock = true,
          ColumnSet = new ColumnSet("privilegeid"),
          Criteria = new FilterExpression()
        };

        // ReSharper disable once CoVariantArrayConversion
        var condition = new ConditionExpression("name", ConditionOperator.In, privilegesToRetrieve.ToArray());
        privilegeQuery.Criteria.AddCondition(condition);
        request.Requests.Add(new RetrieveMultipleRequest { Query = privilegeQuery });
        request.Requests.Add(new RetrieveUserPrivilegesRequest {UserId = currentUserRef.Id});
      }

      var service = this.PluginContext.ServiceAsSystemUser;
      var response = (ExecuteMultipleResponse)service.Execute(request);
      if (response.IsFaulted)
      {
        throw new InvalidPluginExecutionException("An error occured while getting current user permissions for the records.");
      }

      Func<AccessRights, bool> allowUpdateFromItemSetSide = rights => (rights & AccessRights.AppendAccess) != AccessRights.None
                                                                      && (rights & AccessRights.AppendToAccess) != AccessRights.None;
      Func<AccessRights, bool> allowUpdateFromEntitySide = rights => allowUpdateFromItemSetSide(rights)
                                                                     && (rights & AccessRights.WriteAccess) != AccessRights.None;

      if (!retrievePrivileges)
      {
        if (addNewRecordMode)
        {
          var retrievePrincipalAccessResponseItemSetSide =
            // ReSharper disable once PossibleNullReferenceException
            (RetrievePrincipalAccessResponse)(response.Responses.FirstOrDefault(r => r.RequestIndex == 0).Response);
          return allowUpdateFromItemSetSide(retrievePrincipalAccessResponseItemSetSide.AccessRights);
        }
        else
        {
          var retrievePrincipalAccessResponseEntitySide =
            // ReSharper disable PossibleNullReferenceException
            (RetrievePrincipalAccessResponse)(response.Responses.FirstOrDefault(r => r.RequestIndex == 0).Response);
          var retrievePrincipalAccessResponseItemSetSide =
            (RetrievePrincipalAccessResponse)(response.Responses.FirstOrDefault(r => r.RequestIndex == 1).Response);
          // ReSharper restore PossibleNullReferenceException

          return allowUpdateFromEntitySide(retrievePrincipalAccessResponseEntitySide.AccessRights)
            && allowUpdateFromItemSetSide(retrievePrincipalAccessResponseItemSetSide.AccessRights);
        }
      }

      var bothEntitiesHasOrganizationOwnership = entityHasOrganizationOwnership
                                                 && itemSetEntityHasOrganizationOwnership;
      RetrieveMultipleResponse retrieveMultipleResponse;
      RetrieveUserPrivilegesResponse retrieveUserPrivilegesResponse;
      if (bothEntitiesHasOrganizationOwnership)
      {
        // ReSharper disable once PossibleNullReferenceException
        retrieveMultipleResponse = (RetrieveMultipleResponse)(response.Responses.FirstOrDefault(r => r.RequestIndex == 0).Response);
        retrieveUserPrivilegesResponse =
          // ReSharper disable once PossibleNullReferenceException
          (RetrieveUserPrivilegesResponse)(response.Responses.FirstOrDefault(r => r.RequestIndex == 1).Response);
      }
      else
      {
        bool readOnlyPermission;
        if (entityHasOrganizationOwnership)
        {
          // ReSharper disable PossibleNullReferenceException
          var retrievePrincipalAccessResponseItemSetSide =
            (RetrievePrincipalAccessResponse)(response.Responses.FirstOrDefault(r => r.RequestIndex == 0).Response);
          // ReSharper restore PossibleNullReferenceException
          readOnlyPermission = !allowUpdateFromItemSetSide(retrievePrincipalAccessResponseItemSetSide.AccessRights);
        }
        else
        {
          if (addNewRecordMode)
          {
            readOnlyPermission = false;
          }
          else
          {
            var retrievePrincipalAccessResponseEntitySide =
              // ReSharper disable PossibleNullReferenceException
              (RetrievePrincipalAccessResponse)(response.Responses.FirstOrDefault(r => r.RequestIndex == 0).Response);
            // ReSharper restore PossibleNullReferenceException
            readOnlyPermission = !allowUpdateFromEntitySide(retrievePrincipalAccessResponseEntitySide.AccessRights);
          }
        }

        if (readOnlyPermission)
        {
          return false;
        }

        var startIndex = addNewRecordMode ? 0 : 1;
        // ReSharper disable once PossibleNullReferenceException
        retrieveMultipleResponse = (RetrieveMultipleResponse)(response.Responses.FirstOrDefault(r => r.RequestIndex == startIndex).Response);
        retrieveUserPrivilegesResponse =
          // ReSharper disable once PossibleNullReferenceException
          (RetrieveUserPrivilegesResponse)(response.Responses.FirstOrDefault(r => r.RequestIndex == startIndex + 1).Response);
      }

      var privilegeIds = retrieveMultipleResponse.EntityCollection.Entities.Select(r => r.Id).ToList();
      foreach (var privilegeId in privilegeIds)
      {
        var userHasPrivilege =
          retrieveUserPrivilegesResponse.RolePrivileges.Any(r => r.PrivilegeId.Equals(privilegeId));
        if (!userHasPrivilege)
        {
          return false;
        }
      }

      return true;
    }

    /// <summary>
    /// Gets the item set for N:N relationship.
    /// </summary>
    /// <returns></returns>
    private IList<OptionItem> GetItemSetForNnRelationship()
    {
      var intersectEntityRefAttributeName = this.ItemSetConfig.pavelkh_IntersectEntityRefAttributeName;
      var recordExists = !string.IsNullOrWhiteSpace(this.recordIdString);
      var fetchXml = recordExists ? 
                          this.ItemSetConfig.pavelkh_FetchXmlForEditMode.Replace(FetchXmlEntityIdPlaceHolder, this.recordIdString) : 
                          this.ItemSetConfig.pavelkh_FetchXml;

      var service = this.PluginContext.Service;
      var fetchExpression = new FetchExpression(fetchXml);
      var entityCollection = service.RetrieveMultiple(fetchExpression);
      var entities = entityCollection.Entities;
      var labelFieldName = this.ItemSetConfig.pavelkh_LabelAttributeName;
      var tooltipFieldName = string.IsNullOrWhiteSpace(this.ItemSetConfig.pavelkh_TooltipAttributeName) ? null : this.ItemSetConfig.pavelkh_TooltipAttributeName;

      return entities.Select(
        entity => 
          new OptionItem
            {
              Id = entity.Id,
              Label = entity.GetAttributeValue<string>(labelFieldName),
              Tooltip = tooltipFieldName == null ? null : entity.GetAttributeValue<string>(tooltipFieldName),
              Selected = 
                recordExists && entity.GetAttributeValue<AliasedValue>(intersectEntityRefAttributeName) != null
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

      [DataMember]
      public string Tooltip { get; set; }
    }
  }
}