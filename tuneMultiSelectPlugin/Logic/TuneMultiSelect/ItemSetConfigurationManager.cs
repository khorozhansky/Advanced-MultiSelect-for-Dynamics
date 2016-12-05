namespace TuneMultiSelect.Logic.TuneMultiSelect
{
  using Entities;

  using Metadata;

  using Microsoft.Xrm.Sdk;

  public class ItemSetConfigurationManager : ManagerBase<tunexrm_tunemultiselectitemsetconfiguration>
  {
    public ItemSetConfigurationManager(PluginBase<tunexrm_tunemultiselectitemsetconfiguration>.PluginContext pluginContext) : base(pluginContext)
    {
    }

    public void CreatePreOperationSync()
    {
      this.FillInItemSetDetails();
    }

    public void CreatePostOperationSync()
    {
    }

    public void UpdatePreOperationSync()
    {
      this.FillInItemSetDetails();
    }

    public void UpdatePostOperationSync()
    {
    }

    public void DeletePreOperationSync()
    {
      this.DeleteItemSet();
    }

    private void FillInItemSetDetails()
    {
      var pluginContext = this.PluginContext;
      pluginContext.Trace("Filling in Item Set Details...");
      var targetExt = pluginContext.TargetExt;
      var target = targetExt.Target;
      var entityNameExt = targetExt.GetValue(() => target.tunexrm_EntityName);
      var relationshipNameExt = targetExt.GetValue(() => target.tunexrm_RelationshipName);
      var entityName = entityNameExt.Value;
      var relationshipName = relationshipNameExt.Value;
      var relationship = MetadataUtils.GetManyToManyRelationshipMetadata(pluginContext, relationshipName);
      if (relationship.Entity1LogicalName == entityName)
      {
        target.tunexrm_ItemSetEntityName = relationship.Entity2LogicalName;
        target.tunexrm_EntityAttributeName = relationship.Entity1IntersectAttribute;
        target.tunexrm_ItemSetEntityAttributeName = relationship.Entity2IntersectAttribute;
      }
      else
      {
        if (relationship.Entity2LogicalName != entityName)
        {
          throw new InvalidPluginExecutionException(
            $"An error occured while saving the Item Set configuration. { entityName } Entity is not accosiated with the { relationshipName } relationship.");
        }

        target.tunexrm_ItemSetEntityName = relationship.Entity1LogicalName;
        target.tunexrm_EntityAttributeName = relationship.Entity2IntersectAttribute;
        target.tunexrm_ItemSetEntityAttributeName = relationship.Entity1IntersectAttribute;
      }

      target.tunexrm_IsCustomRelationship = relationship.IsCustomRelationship;
      target.tunexrm_IntersectEntityName = relationship.IntersectEntityName;
    }

    private void DeleteItemSet()
    {
    }

    private bool GetIsDirectAction()
    {
      var pluginContext = this.PluginContext;
      var pluginExecutionContext = pluginContext.ExecContext;
      return pluginExecutionContext.Depth == 1;
    }
  }
}
