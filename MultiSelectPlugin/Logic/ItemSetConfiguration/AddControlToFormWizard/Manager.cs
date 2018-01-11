namespace AdvancedMultiSelect.Logic.ItemSetConfiguration.AddControlToFormWizard
{
  using System;
  using System.Linq;
  using System.Text.RegularExpressions;
  using System.Xml.Linq;
  using System.Xml.XPath;

  using Microsoft.Xrm.Sdk;

  using CrmProxy;
  using Utils;

  public class Manager : ManagerBase<Entity>
  {
    private readonly MultiSelectFormControl control;

    public Manager(PluginBase<Entity>.PluginContext pluginContext)
      : base(pluginContext)
    {
      this.control = new MultiSelectFormControl
        {
          EntityLogicalName = pluginContext.GetInputParameter<string>("EntityLogicalName"),
          ItemSetName = pluginContext.GetInputParameter<string>("ItemSetName"),
          SavingAttributeName = pluginContext.GetInputParameter<string>("DummySavingAttributeName"),
          FormId = pluginContext.GetInputParameter<string>("FormId"),
          TabName = pluginContext.GetInputParameter<string>("TabName"),
          NewSectionLocation = pluginContext.GetInputParameter<int>("NewSectionLocation"),
          NearSectionName = pluginContext.GetInputParameter<string>("NearSectionName"),
          WebResource = pluginContext.GetInputParameter<string>("WebResource"),
          NewSectionName = pluginContext.GetInputParameter<string>("NewSectionName"),
          NewSectionLabel = pluginContext.GetInputParameter<string>("NewSectionLabel"),
          ShowLabel = pluginContext.GetInputParameter<bool>("ShowLabel"),
          ShowLine = pluginContext.GetInputParameter<bool>("ShowLine"),
          NumberOfRows = pluginContext.GetInputParameter<int>("NumberOfRows")
        };

      this.ValidateInputParams();
    }

    public void ProcessAddControlOnFormAction()
    {
      var pluginContext = this.PluginContext;
      var ctrl = this.control;
      Guid formId;
      if (!Guid.TryParse(ctrl.FormId, out formId))
      {
        throw new ArgumentException(nameof(ctrl.FormId));
      }

      ctrl.EntityLogicalName = ctrl.EntityLogicalName.Trim().ToLowerInvariant();
      var orgCtx = pluginContext.OrgCtx;

      var formXmlString =
        orgCtx.SystemFormSet.Where(r => r.FormId == formId && r.ObjectTypeCode == ctrl.EntityLogicalName)
          .Select(r => r.FormXml)
          .FirstOrDefault();

      if (string.IsNullOrWhiteSpace(formXmlString))
      {
        throw new NullReferenceException(nameof(formXmlString));
      }

      ctrl.TabName = ctrl.TabName.Trim().ToLowerInvariant();
      ctrl.WebResource = ctrl.WebResource.Trim();
      ctrl.FormId = ctrl.FormId.Trim().ToLowerInvariant();
      ctrl.ItemSetName = ctrl.ItemSetName.Trim();
      ctrl.NewSectionLabel = ctrl.NewSectionLabel.Trim();
      ctrl.NewSectionName = ctrl.NewSectionName.Trim();
      ctrl.SavingAttributeName = ctrl.SavingAttributeName.Trim().ToLowerInvariant();

      var formXml = XDocument.Parse(formXmlString);

      this.ValidateSectionName(formXml, ctrl.NewSectionName);

      // ReSharper disable PossibleNullReferenceException
      var xTabs = formXml.Root.Element("tabs").Elements("tab").ToList();
      // ReSharper restore PossibleNullReferenceException
      var xTab = xTabs.FirstOrDefault(
        r =>
          {
            var xAttribute = r.Attribute("name");
            return xAttribute != null 
              && xAttribute.Value.Equals(ctrl.TabName, StringComparison.InvariantCultureIgnoreCase);
          });

      if (xTab == null)
      {
        xTab = xTabs.FirstOrDefault(
          r =>
          {
            var xAttribute = r.Attribute("id");
            return xAttribute != null 
              && xAttribute.Value.Equals(ctrl.TabName, StringComparison.InvariantCultureIgnoreCase);
          });
      }

      if (xTab == null)
      {
        throw new InvalidPluginExecutionException($"{ctrl.TabName} tab is not found.");
      }

      var xColumns = xTab.Elements("columns").Elements("column");
      var controlSection = this.BuildControlSection();

      const int AddBefore = 10;
      const int AddFirst = 30;

      var location = ctrl.NewSectionLocation;
      if (location == AddFirst)
      {
        // ReSharper disable PossibleNullReferenceException
        var xFirstColumn = xColumns.FirstOrDefault();
        var columnSections = xFirstColumn.Element("sections");
        columnSections.Add(controlSection);
        // ReSharper restore PossibleNullReferenceException
      }
      else
      {
        var sectionName = ctrl.NearSectionName;
        if (string.IsNullOrWhiteSpace(sectionName))
        {
          throw new NullReferenceException(nameof(sectionName));
        }

        sectionName = sectionName.Trim();
        var section = formXml.XPathSelectElement($@"//section[@name=""{sectionName}""]");
        if (section == null)
        {
          section = formXml.XPathSelectElement($@"//section[@id=""{sectionName}""]");
          if (section == null)
          {
            throw new InvalidPluginExecutionException($"The '{sectionName}' section is not found on the form.");
          }
        }

        if (location == AddBefore)
        {
          section.AddBeforeSelf(controlSection);
        }
        else
        {
          section.AddAfterSelf(controlSection);
        }
      }

      var form = new SystemForm
        {
          FormId = formId,
          FormXml = formXml.ToString()
        };

      pluginContext.Service.Update(form);
      MetadataUtils.PublishEntity(pluginContext.Service, this.control.EntityLogicalName);
    }

    private void ValidateSectionName(XDocument formXml, string sectionName)
    {
      var regex = new Regex(@"^[a-zA-Z0-9_]*$");
      var match = regex.Match(sectionName);
      if (!match.Success)
      {
        throw new InvalidPluginExecutionException("'The New Section Name' can only contain alphanumeric characters and underscore.");
      }

      var section =  formXml.XPathSelectElement($@"//section[@name=""{sectionName}""]");
      if (section != null)
      {
        throw new InvalidPluginExecutionException($"The form contains the '{sectionName}' section already. Please choose another section name.");
      }

      var webResourceName = $"WebResource_{sectionName}";
      var webResource = formXml.XPathSelectElement($@"//control[@id=""{webResourceName}""]");
      if (webResource != null)
      {
        throw new InvalidPluginExecutionException($"The form contains the web resource with ID='{webResourceName}' already.");
      }
    }

    private void ValidateInputParams()
    {
      var ctrl = this.control;
      var stringParams = new[]
        {
          ctrl.EntityLogicalName, ctrl.ItemSetName, ctrl.SavingAttributeName, ctrl.FormId,
          ctrl.TabName, ctrl.WebResource, ctrl.NewSectionName, ctrl.NewSectionLabel
        };

      var invalidParams = stringParams.Any(string.IsNullOrWhiteSpace);
      if (invalidParams)
      {
        throw new InvalidPluginExecutionException("Not all required parameters provided.");
      }
    }

    private XElement BuildControlSection()
    {
      var pluginContext = this.PluginContext;
      var langCode = UserSettingUtils.GetCurrentUserLanguageCode(pluginContext);
      var ctrl = this.control;
      var sectionId = Guid.NewGuid().ToString("B");
      var controlCellId = Guid.NewGuid().ToString("B");
      var bottomRowsXmlString = string.Concat(Enumerable.Repeat(@"<row/>", ctrl.NumberOfRows - 1));
      const int HtmlType = 1;
      var webResourceId =
        pluginContext.OrgCtx.WebResourceSet
          .Where(r =>
                 // ReSharper disable once RedundantBoolCompare
            r.IsCustomizable != null && r.IsCustomizable.Value == true
            && r.IsHidden != null && r.IsHidden.Value == false
            && r.WebResourceType != null && r.WebResourceType.Value == HtmlType
            && r.Name == ctrl.WebResource)
          .Select(r => r.WebResourceId)
          .FirstOrDefault();
      if (webResourceId == null)
      {
        throw new InvalidPluginExecutionException($"Web Resource {ctrl.WebResource} is not found in the system.");
      }

      var webResourceIdString = webResourceId.Value.ToString("P").ToUpperInvariant();

      var dummySavingAttributeCellId = Guid.NewGuid().ToString("B");
      var showLabel = ctrl.ShowLabel.ToString().ToLowerInvariant();
      var showLine = ctrl.ShowLine.ToString().ToLowerInvariant();
      var sectionXmlString = $@"
        <section name=""{ctrl.NewSectionName}"" showlabel=""{showLabel}"" showbar=""{showLine}"" locklevel=""0"" id=""{sectionId}"" IsUserDefined=""0"" layout=""varwidth"" columns=""1"" labelwidth=""115"" celllabelalignment=""Left"" celllabelposition=""Left"">
          <labels>
	          <label description=""{ctrl.NewSectionLabel}"" languagecode=""{langCode}"" />
          </labels>
          <rows>
	        <row>
	          <cell id=""{controlCellId}"" showlabel=""false"" colspan=""1"" auto=""false"" rowspan=""{ctrl.NumberOfRows}"">
		        <labels>
		          <label description=""."" languagecode=""{langCode}"" />
		        </labels>
		        <control id=""{"WebResource_" + ctrl.NewSectionName}"" classid=""{{9FDF5F91-88B1-47f4-AD53-C11EFC01A01D}}"">
		          <parameters>
			          <Url>{ctrl.WebResource}</Url>
			          <Data>itemsetname=""{ctrl.ItemSetName}""</Data>
			          <PassParameters>true</PassParameters>
			          <ShowOnMobileClient>false</ShowOnMobileClient>
			          <Security>false</Security>
			          <Scrolling>auto</Scrolling>
			          <Border>false</Border>
			          <WebResourceId>{webResourceIdString}</WebResourceId>
		          </parameters>
		        </control>
		        <events>
		          <event name=""onload"" application=""0"">
			        <dependencies />
		          </event>
		        </events>
	          </cell>
	        </row>
	        {bottomRowsXmlString}
	        <row>
	          <cell id=""{dummySavingAttributeCellId}"" showlabel=""true"" locklevel=""0"" visible=""false"">
		        <labels>
		          <label description=""Hidden Dummy Saving Attribute"" languagecode=""{langCode}"" />
		        </labels>
		        <control id=""{ctrl.SavingAttributeName}"" classid=""{{4273EDBD-AC1D-40d3-9FB2-095C621B552D}}"" datafieldname=""{ctrl.SavingAttributeName}"" disabled=""false"" />
	          </cell>
	        </row>
          </rows>
        </section>";

      // TODO: check if the field has the same id for the same datafieldname

      return XDocument.Parse(sectionXmlString).Root;
    }
  }
}