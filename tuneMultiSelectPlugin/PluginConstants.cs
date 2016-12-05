namespace TuneMultiSelect
{
  /// <summary>
  /// Pipeline Stage
  /// </summary>
  public enum Stage
  {
    PreValidate = 10, // PreOutsideTransaction
    PreOperation = 20, // PreInsideTransaction
    PostOperation = 40 // PostOutsideTransaction
  }

  /// <summary>
  /// Execution Mode
  /// </summary>
  public enum Mode
  {
    Synchronous = 0,
    Asynchronous = 1
  }

  /// <summary>
  /// Execution Message Name
  /// <remarks>
  /// Add additional messages here as needed
  /// </remarks>
  /// </summary>
  public enum MessageName
  {
    AddItem,
    AddListMembers,
    AddMember,
    AddMembers,
    AddPrivileges,
    AddProductToKit,
    AddRecurrence,
    AddToQueue,
    Assign,
    AssignUserRoles,
    Associate,
    BackgroundSend,
    Book,
    Cancel,
    CheckIncoming,
    CheckPromote,
    Clone,
    Close,
    CopyDynamicListToStatic,
    CopySystemForm,
    Create,
    CreateException,
    CreateInstance,
    Delete,
    DeleteOpenInstances,
    DeliverIncoming,
    DeliverPromote,
    DetachFromQueue,
    Disassociate,
    Execute,
    ExecuteById,
    Export,
    ExportAll,
    ExportCompressed,
    ExportCompressedAll,
    GrantAccess,
    Handle,
    Import,
    ImportAll,
    ImportCompressedAll,
    ImportCompressedWithProgress,
    ImportWithProgress,
    LockInvoicePricing,
    LockSalesOrderPricing,
    Lose,
    Merge,
    ModifyAccess,
    Publish,
    PublishAll,
    QualifyLead,
    Recalculate,
    RemoveItem,
    RemoveMember,
    RemoveMembers,
    RemovePrivilege,
    RemoveProductFromKit,
    RemoveRelated,
    RemoveUserRoles,
    ReplacePrivileges,
    Reschedule,
    Retrieve,
    RetrieveExchangeRate,
    RetrieveFilteredForms,
    RetrieveMultiple,
    RetrievePrincipalAccess,
    RetrieveSharedPrincipalsAndAccess,
    RetrieveUnpublished,
    RetrieveUnpublishedMultiple,
    RevokeAccess,
    Route,
    Send,
    SendFromTemplate,
    SetRelated,
    SetState,
    SetStateDynamicEntity,
    TriggerServiceEndpointCheck,
    UnlockInvoicePricing,
    UnlockSalesOrderPricing,
    Update,
    ValidateRecurrenceRule,
    Win,  

    // Custom Messages for Actions:
    tunexrm_GetItemSet,
    tunexrm_ProcessChangesForMarketingListItemSet,
    tunexrm_ItemSetConfigurationGetEntityDetails,
    tunexrm_ItemSetConfigurationGetEntities,
    tunexrm_ItemSetConfigurationGetItemSetEntityLabelAttributes,
  }
}

