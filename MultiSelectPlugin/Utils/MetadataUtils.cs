namespace AdvancedMultiSelect.Utils
{
  using System;
  using System.Linq;

  using Microsoft.Crm.Sdk.Messages;
  using Microsoft.Xrm.Sdk;
  using Microsoft.Xrm.Sdk.Messages;
  using Microsoft.Xrm.Sdk.Metadata;
  using Microsoft.Xrm.Sdk.Metadata.Query;
  using Microsoft.Xrm.Sdk.Query;

  public static class MetadataUtils
  {
    /// <summary>
    /// The get relationship.
    /// </summary>
    /// <param name="pluginContext">
    /// The plugin context.
    /// </param>
    /// <param name="name">
    /// The name.
    /// </param>
    /// <returns>
    /// The <see cref="RelationshipMetadataBase"/>.
    /// </returns>
    private static RelationshipMetadataBase GetRelationshipMetadata(IPluginContext pluginContext, string name)
    {
      try
      {
        var service = pluginContext.ServiceAsSystemUser;
        var request = new RetrieveRelationshipRequest
        {
          Name = name
        };

        var response = (RetrieveRelationshipResponse)service.Execute(request);
        return response.RelationshipMetadata;
      }
      catch (Exception)
      {
        throw new InvalidPluginExecutionException($"The {name} relationship is not found.");
      }
    }

    public static ManyToManyRelationshipMetadata GetManyToManyRelationshipMetadata(IPluginContext pluginContext, string name)
    {
      var relationshipMetadata = GetRelationshipMetadata(pluginContext, name);
      if (relationshipMetadata == null)
      {
        throw new InvalidPluginExecutionException($"The {name} relationship is not found.");
      }

      var manyToManyRelationship = relationshipMetadata as ManyToManyRelationshipMetadata;
      if (manyToManyRelationship == null)
      {
        throw new InvalidPluginExecutionException(
          $"The {relationshipMetadata.SchemaName} relationship is not 'many to many'. This type of relations is not implemented yet.");
      }

      return manyToManyRelationship;
    }

    public static void PublishEntity(IOrganizationService service, string entityLogicalName, bool executeOutsideOfTrancation = false)
    {
      var request = new PublishXmlRequest();
      request.ParameterXml =
        $"<importexportxml><entities><entity>{entityLogicalName}</entity></entities></importexportxml>";
      if (executeOutsideOfTrancation)
      {
        var execMultipleRequest = new ExecuteMultipleRequest()
          {
            Requests = new OrganizationRequestCollection(),
            Settings = new ExecuteMultipleSettings() { ContinueOnError = false, ReturnResponses = true }
          };

        execMultipleRequest.Requests.Add(request);
        var response = (ExecuteMultipleResponse)service.Execute(execMultipleRequest);
        if (response.IsFaulted)
        {
          throw new InvalidPluginExecutionException("An error occured while publishing changes.");
        }
      }
      else
      {
        service.Execute(request);
      }
    }

    public static AttributeMetadata GetAttributeMetadata(
      IOrganizationService service,
      string entityName,
      string attributeName,
      string[] attrPropNames)
    {
      var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
      entityFilter.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, entityName));
      var entityProperties = new MetadataPropertiesExpression { AllProperties = false };
      entityProperties.PropertyNames.AddRange("LogicalName", "Attributes");
      var attributeFilter = new MetadataFilterExpression(LogicalOperator.And);
      attributeFilter.Conditions.Add(
        new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, attributeName));
      var attributeProperties = new MetadataPropertiesExpression() { AllProperties = false };
      attributeProperties.PropertyNames.AddRange(attrPropNames);
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
      };

      var request = new RetrieveMetadataChangesRequest { Query = query };
      var response = (RetrieveMetadataChangesResponse)service.Execute(request);
      return response.EntityMetadata.FirstOrDefault()?.Attributes.FirstOrDefault();
    }
  }
}
