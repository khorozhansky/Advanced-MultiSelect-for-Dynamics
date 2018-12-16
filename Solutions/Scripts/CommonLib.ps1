Import-Module Microsoft.Xrm.Data.Powershell -Force

$script:MainSolutionUniqueName = "AdvancedMultiSelectForDynamics"
$script:DemoSolutionUniqueName = "DemoForAdvancedMultiSelect"

function Validate-CrmConnection {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory=$False)]
    [Microsoft.Xrm.Tooling.Connector.CrmServiceClient]$CrmConn
  )

  if($CrmConn -eq $null)
  {
    throw 'A connection to CRM is not specified.'
  }
}

function Get-SolutionsFolder {
  return "$PSScriptRoot\..\Solutions\"
}

function Test-SolutionFilePath {
  [CmdletBinding()]
  param(
    [parameter(Mandatory=$True)]
    [string]$Path
  )

  if(-not (Test-Path $Path)){
    throw [System.IO.FileNotFoundException] "Solution file ('$Path') not found."
  }
}

function Build-AdvancedMultiSelectManagedSolution {
  <#
    .SYNOPSIS
    Build Managed Solution file containing Site Map changes (application navigation link to the "AdvancedMultiSelect Item Set Configuration" view)
    .DESCRIPTION
    Why: We cannot include Site Map changes in the Unmanaged solution directly 
    as it will overwrite all custom Site Map changes on the destination instance where 
    the unmanaged solution will be installed. Therefore Site Map is not included into 'AdvancedMultiSelect for Dynamics' solution by default.
    In order to get 'AdvancedMultiSelect for Dynamics' Managed solution containing the Site Map we use this function.
    (Site Map customizations inside a Managed Solution won't overwrite other existing Site Map customizations).
    Action steps: 
    - exports solution as managed from an given instance
    - unpacks the solution
    - adds "Site Map" definition file (SiteMap_managed.xml) to the solution 
      as well as makes changes to solution.xml and customization.xml to define the Site Map changes
    - packs then new Managed Solution and saves in into Solutions sub-folder
    .PARAMETER conn
    Connection to the Dynamics CRM/365 instance/organization where Unmanaged 'AdvancedMultiSelectForDynamics' solution is placed
  #>
  [CmdletBinding()]
  param(
    [Parameter(Mandatory=$True)]
    [Microsoft.Xrm.Tooling.Connector.CrmServiceClient]$CrmConn

  )

  $solutonsFolder = Get-SolutionsFolder
  $solutionName = $script:MainSolutionUniqueName
  Write-Verbose "Exporting '$solutionName' Managed solution"
  $exportResult = Export-CrmSolution -conn $CrmConn `
    -SolutionName $solutionName `
    -SolutionFilePath $solutonsFolder `
    -Managed
  
  $solutionFilePath = $exportResult."SolutionPath"
  Write-Verbose "'$solutionName' Managed solution has been exported to $solutionFilePath"
  $tempExtractFolder = "$invokationFolder\..\Temp\Preparation\"
  $solutionPackagerPath = "$invokationFolder\..\Tools\SolutionPackager.exe"

  # cleaning the temporary folder as a separate step (not along with unpack in order to keep .gitignore) 
  Write-Verbose "Cleaning up '$tempExtractFolder' temporary folder."
  Get-ChildItem "$tempExtractFolder" -Exclude .gitignore |  Remove-Item -Recurse -Force
  Write-Verbose "The temporary folder has been cleaned up."
  
  Write-Verbose "Extracting '$solutionFilePath' Managed solution file into a temporary folder ($tempExtractFolder)"
  & $solutionPackagerPath /action:extract /allowDelete:No /zipFile:$solutionFilePath /folder:$tempExtractFolder /packagetype:Managed #| Tee-Object -Variable scriptOutput | Out-Null

  Write-Verbose "The '$solutionFilePath' Managed solution file has been extracted into $tempExtractFolder temporary folder."
  Write-Verbose "Adding SiteMap_managed.xml to the extracted solution."
  # Add SiteMap details definition to the solution folder
  Copy-Item "$invokationFolder\ModificationTemplates\SiteMap_managed.xml" "$tempExtractFolder\Other\SiteMap_managed.xml"

  Write-Verbose "Adjusting Solution.xml file."
  # Add SiteMap definition to the Solution.xml file
  $solutionXmlFile = "$tempExtractFolder\Other\Solution.xml"
  [xml] $xml = (Get-Content $solutionXmlFile)
  $siteMapRootComponent = $xml.CreateElement("RootComponent")
  $siteMapRootComponent.SetAttribute("type", "62");
  $siteMapRootComponent.SetAttribute("behavior", "0");
  $rootComponents = Select-XML -XML $xml -XPath '//ImportExportXml/SolutionManifest/RootComponents'
  $rootComponents.Node.AppendChild($siteMapRootComponent)
  $xml.Save($solutionXmlFile)

  Write-Verbose "Adjusting Customizations.xml file."
  # Add SiteMap definition to the Customizations.xml file (adds just before <EntityMaps/> node!)
  $customizationsXmlFile = "$tempExtractFolder\Other\Customizations.xml"
  [xml] $xml = (Get-Content $customizationsXmlFile)
  [System.Xml.XmlElement] $siteMapNode = $xml.CreateElement("SiteMap")
  $importExportXmlNode = Select-XML -XML $xml -XPath '//ImportExportXml'
  $entityMapsNode = Select-XML -XML $xml -XPath '//ImportExportXml/EntityMaps'
  $importExportXmlNode.Node.InsertBefore($siteMapNode, $entityMapsNode.Node)
  $xml.Save($customizationsXmlFile)

  Write-Verbose "Packing new Managed Solution file ($solutionFilePath)."
  # Pack final managed solution
  & $solutionPackagerPath /action:pack /allowDelete:Yes /zipFile:$solutionFilePath /folder:$tempExtractFolder /packagetype:Managed
  Write-Verbose "The '$solutionFilePath' Managed Solution has been packed."

  Write-Verbose "Cleaning up '$tempExtractFolder' temporary folder."
  Get-ChildItem "$tempExtractFolder" -Exclude .gitignore |  Remove-Item -Recurse -Force
  Write-Verbose "The temporary folder has been cleaned up."
}

function Export-AdvancedMultiSelectSolutions {
  <#
    .SYNOPSIS
      a. Exports 'AdvancedMultiSelectForDynamics' Unmanaged solution
      b. Exports and Rebuilds 'AdvancedMultiSelectForDynamics' Managed solution adding Site Map customization to it
    .PARAMETER conn
    Connection to the Dynamics CRM/365 instance/organization where Unmanaged 'AdvancedMultiSelectForDynamics' solution is placed
  #>
  [CmdletBinding()]
  param(
    [Parameter(Mandatory=$True)]
    [Microsoft.Xrm.Tooling.Connector.CrmServiceClient]$CrmConn
  )

  $solutonsFolder = Get-SolutionsFolder
  $solutionName = $script:MainSolutionUniqueName
  Write-Verbose "Exporting '$solutionName' Unmanaged solution"
  $exportResult = Export-CrmSolution -conn $CrmConn `
    -SolutionName $solutionName `
    -SolutionFilePath $solutonsFolder `
  
  $solutionFilePath = $exportResult."SolutionPath"
  Write-Verbose "'$solutionName' Unmanaged solution has been exported to $solutionFilePath"
  Build-AdvancedMultiSelectManagedSolution -CrmConn $CrmConn
  Write-Output "Both managed and unmanaged '$solutionName' solutions have been exported and ready to use."
}

function Export-DemoForAdvancedMultiSelectSolutions {
  <#
    .SYNOPSIS
      Exports 'DemoForAdvancedMultiSelect' Unmanaged and Managed solutions
    .PARAMETER conn
    Connection to the Dynamics CRM/365 instance/organization where Unmanaged 'DemoForAdvancedMultiSelect' solution is placed
  #>
  [CmdletBinding()]
  param(
    [Parameter(Mandatory=$True)]
    [Microsoft.Xrm.Tooling.Connector.CrmServiceClient]$CrmConn
  )

  $solutonsFolder = Get-SolutionsFolder
  $solutionName = $script:DemoSolutionUniqueName
  Write-Verbose "Exporting '$solutionName' Unmanaged solution"
  $exportResult = Export-CrmSolution -conn $CrmConn `
    -SolutionName $solutionName `
    -SolutionFilePath $solutonsFolder `

  $solutionFilePath = $exportResult."SolutionPath"
  Write-Verbose "'$solutionName' Unmanaged solution has been exported to $solutionFilePath"

  Write-Verbose "Exporting '$solutionName' Managed solution"
  $exportResult = Export-CrmSolution -conn $CrmConn `
    -SolutionName $solutionName `
    -SolutionFilePath $solutonsFolder `
    -Managed

  $solutionFilePath = $exportResult."SolutionPath"
  Write-Verbose "'$solutionName' Managed solution has been exported to $solutionFilePath"
  Write-Output "Both managed and unmanaged '$solutionName' solutions have been exported and ready to use."
}

function Import-AdvancedMultiSelectSolutions {
  <#
    .SYNOPSIS
      Exports 'DemoForAdvancedMultiSelect' Unmanaged and Managed solutions
    .PARAMETER conn
    Connection to the Dynamics CRM/365 instance/organization where Unmanaged 'DemoForAdvancedMultiSelect' solution is placed
  #>
  [CmdletBinding()]
  param(
    [Parameter(Mandatory=$True)]
    [Microsoft.Xrm.Tooling.Connector.CrmServiceClient]$CrmConn,
    [parameter(Mandatory=$True)]
    [string]$Version,
    [parameter(Mandatory=$False)]
    [switch]$Managed,
    [parameter(Mandatory=$False)]
    [switch]$OverwriteUnManagedCustomizations,
    [parameter(Mandatory=$False)]
    [switch]$ImportAsHoldingSolution
  )

  $solutonsFolder = Get-SolutionsFolder
  $solutionTypeSuffix = if($Managed) {"_managed_"} else {"_unmanaged_"}
  $Version = $Version.Replace('.','_')
  $mainSolutionFilePath = "$solutonsFolder$script:MainSolutionUniqueName$solutionTypeSuffix$Version.zip"
  Test-SolutionFilePath $mainSolutionFilePath
  $demoSolutionFilePath = "$solutonsFolder$script:DemoSolutionUniqueName$solutionTypeSuffix$Version.zip"
  Test-SolutionFilePath $demoSolutionFilePath
  $OverwriteUnManagedCustomizations = $managed -and $OverwriteUnManagedCustomizations
  $ImportAsHoldingSolution = $managed -and $ImportAsHoldingSolution
  $publishChanges = -not $managed
  Write-Verbose "Importing '$mainSolutionFilePath' solution..."
  $importResult = Import-CrmSolution -conn $CrmConn `
    -SolutionFilePath $mainSolutionFilePath `
    -ActivatePlugIns $True `
    -OverwriteUnManagedCustomizations:$OverwriteUnManagedCustomizations `
    -ImportAsHoldingSolution:$ImportAsHoldingSolution `
    -PublishChanges:$publishChanges
  Write-Verbose "'$script:MainSolutionUniqueName' solution has been imported."

  Write-Verbose "Importing '$demoSolutionFilePath' solution..."
  $importResult = Import-CrmSolution -conn $CrmConn `
    -SolutionFilePath $demoSolutionFilePath `
    -ActivatePlugIns $True `
    -OverwriteUnManagedCustomizations:$OverwriteUnManagedCustomizations `
    -ImportAsHoldingSolution:$ImportAsHoldingSolution `
    -PublishChanges:$publishChanges
  Write-Verbose "'$script:DemoSolutionUniqueName' solution has been imported."
}

function Choose-ConnectionType {
	[CmdletBinding()]
	param (
		$caption = "CRM Connection Type",
		$message = "Select a CRM connection type:"
	)

	process {
		$choices = New-Object System.Collections.ObjectModel.Collection[System.Management.Automation.Host.ChoiceDescription]
		$choices.Add((New-Object System.Management.Automation.Host.ChoiceDescription "On-&Premise", "On-Premise"))
		$choices.Add((New-Object System.Management.Automation.Host.ChoiceDescription "&Office 365", "Office 365"))
		$choice = $host.ui.PromptForChoice($caption, $message, $choices, 0)
		
    if($choice -eq 0){
      return "onprem"
    } else{
      return "online"
    }
	}
}

function Build-CrmConnection {
    [CmdletBinding()]
    param(
        [parameter(Mandatory=$false, ParameterSetName="ServerUrl")]
        [PSCredential]$Credential, 
     		[Parameter(Mandatory=$true, ParameterSetName="ServerUrl")]
        [ValidatePattern('http(s)?://[\w-]+(/[\w- ./?%&=]*)?')]
        [Uri]$ServerUrl,
        [Parameter(Mandatory=$false, ParameterSetName="ServerUrl")]
        [string]$OrganizationName,
        [Parameter(Mandatory=$false, ParameterSetName="ServerUrl")]
        [string]$HomeRealmUrl,
        [Parameter(Mandatory=$false, ParameterSetName="InteractiveMode")]
        [switch]$InteractiveMode
    )

  [Microsoft.Xrm.Tooling.Connector.CrmServiceClient]$result = $null;
  if ($InteractiveMode){
		  $result = Get-CrmConnection -InteractiveMode -Verbose
  } else {
    $onlineDeployment = $ServerUrl -match '.*\.dynamics\.com$'
    if ($onlineDeployment){
      $result = Connect-CrmOnline -Credential $Credential -ServerUrl $ServerUrl
    } else {
      $result = Connect-CrmOnPremDiscovery -Credential $Credential -ServerUrl $ServerUrl -OrganizationName $OrganizationName -HomeRealmUrl $HomeRealmUrl
    }
  }

  Validate-CrmConnection $result
  return $result
}
