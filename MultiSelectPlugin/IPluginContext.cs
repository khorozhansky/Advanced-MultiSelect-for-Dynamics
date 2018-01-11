namespace AdvancedMultiSelect
{
  using System;
  using AdvancedMultiSelect.CrmProxy;
  using Microsoft.Crm.Sdk.Messages;
  using Microsoft.Xrm.Sdk;
  using Microsoft.Xrm.Sdk.Query;

  public interface IPluginContext
  {
    IPluginExecutionContext ExecContext { get; }

    ITracingService TracingService { get; }
    
    IOrganizationService Service { get; }
    
    IOrganizationService ServiceAsSystemUser { get; }

    Guid PrimaryEntityId { get; }

    string PrimaryEntityName { get; }

    EntityReference Principal { get; }

    AccessRights? AccessRights { get; set; }

    OptionSetValue State { get; }

    OptionSetValue Status { get; }

    Relationship Relationship { get; }

    EntityReferenceCollection RelatedEntities { get; }

    Guid? SubordinateId { get; }

    EntityReference EntityMoniker { get; }

    QueryExpression QueryExpr { get; }

    EntityReference Assignee { get; }

    CrmContext OrgCtx { get; }

    CrmContext OrgCtxAsSystemUser { get; }

    Entity InputTargetAsEntity { get; }

    EntityReference InputTargetEntityReference { get; }

    string ParameterXml { get; }

    object GetInputParameter(string key);

    object GetParameter(ParameterCollection parameters, string key);

    void SetInputParameter(string key, object value);

    void SetOutputParameter(string key, object value);

    void SetParameter(ParameterCollection parameters, string key, object value);

    Entity GetPreImageEntity(string imageName = null);

    Entity GetPostImageEntity(string imageName = null);

    object GetSharedVariable(string name, bool useParentContext = false);

    void SetSharedVariable(string name, object value);

    bool IsMessage(MessageName messageName);

    void Trace(string message);

    void Trace(string message, params object[] args);
  }
}
