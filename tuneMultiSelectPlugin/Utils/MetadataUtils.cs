namespace TuneMultiSelect.Utils
{
  using System;

  using Microsoft.Xrm.Sdk;
  using Microsoft.Xrm.Sdk.Messages;
  using Microsoft.Xrm.Sdk.Metadata;

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
  }
}
