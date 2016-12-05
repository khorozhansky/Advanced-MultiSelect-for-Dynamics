namespace TuneMultiSelect.Utils
{
  using System.Linq;
  using Microsoft.Xrm.Sdk;
  using Microsoft.Xrm.Sdk.Messages;
  using Microsoft.Xrm.Sdk.Metadata;

  public static class MetadataHelper
  {
    /// <summary>
    /// Gets the options set by attribute.
    /// </summary>
    /// <param name="service">The service.</param>
    /// <param name="entityName">Name of the entity.</param>
    /// <param name="attributeName">Name of the attribute.</param>
    /// <returns>OptionMetadataCollection.</returns>
    public static OptionMetadataCollection GetOptionsSetByAttribute(IOrganizationService service, string entityName, string attributeName)
    {
      var retrieveAttributeRequest = new RetrieveAttributeRequest
      {
        EntityLogicalName = entityName,
        LogicalName = attributeName,
        RetrieveAsIfPublished = false
      };

      var retrieveAttributeResponse = (RetrieveAttributeResponse)service.Execute(retrieveAttributeRequest);
      var optionMetadataCollection = ((PicklistAttributeMetadata)retrieveAttributeResponse.AttributeMetadata).OptionSet.Options;
      return optionMetadataCollection;
    }

    /// <summary>
    /// Gets the option set value label.
    /// </summary>
    /// <param name="service">The service.</param>
    /// <param name="entityName">Name of the entity.</param>
    /// <param name="attributeName">Name of the attribute.</param>
    /// <param name="optionSetValue">The option set value.</param>
    /// <returns>Option Set Label</returns>
    public static string GetOptionSetValueLabel(IOrganizationService service, string entityName, string attributeName, int optionSetValue)
    {
      var optionMetadataCollection = GetOptionsSetByAttribute(service, entityName, attributeName);
      var option = optionMetadataCollection.FirstOrDefault(r => r.Value == optionSetValue);
      return option?.Label.UserLocalizedLabel.Label;
    }


    /// <summary>
    /// The get relationship.
    /// </summary>
    /// <param name="pluginContext">The plugin context.</param>
    /// <param name="name">The name.</param>
    /// <returns>The <see cref="RelationshipMetadataBase"/></returns>
    public static RelationshipMetadataBase GetRelationship(IPluginContext pluginContext, string name)
    {
      var service = pluginContext.ServiceAsSystemUser;
      var request = new RetrieveRelationshipRequest
      {
        Name = name
      };

      var response = (RetrieveRelationshipResponse)service.Execute(request);
      return response.RelationshipMetadata;
    }
  }
}
