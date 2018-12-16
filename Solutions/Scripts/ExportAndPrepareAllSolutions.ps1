#
# This script connects to a Dynamics CRM/365 instance/organization 
# where both 'AdvancedMultiSelectForDynamics' and 'DemoForAdvancedMultiSelect' UNMANAGED solutions are placed
# and exports each of them as both Managed and Unmanaged solutions into ..\Solutions folder
#
# ! NOTE ! : This script also adjusts 'AdvancedMultiSelectForDynamics' MANAGED solution FILE adding Site Map customization to it. 
#

try{
  Clear-Host 
  $error.clear()

  $invokationFolder = $PSScriptRoot
  write-output $hst.Name
  Import-Module ("$invokationFolder\CommonLib.ps1") -Force

  ## ----------------------- UNCOMMENT THIS AS NEEDED TO SETUP SMOOTH CONNECTION -----------------------------
  ## Note: 
  ## You have to store a password hash in a file. You can use \OtherHelpers\CreateSecureStringFile.ps1 
  ## to build its secure content.

  #Write-Output "Prepare credentials to connect to Dynamics smoothly (using saved credentials)..."

  #$serverUrl = "http://test01"
  #$orgName = "org01" # only On-Premise will need it
  #$userName = "test\administrator"

  #$pathToCred = "$invokationFolder\testcred.txt"
  #$pass = Get-Content $pathToCred | ConvertTo-SecureString
  #$cred = new-object -typename System.Management.Automation.PSCredential -argumentlist $userName, $pass

  #Write-Output "Connecting to Dynamics..."

  ## Connecting to On-Premise (note: uncomment one of the two "$conn=" rows below)  
  #$conn = Connect-CrmOnPremDiscovery -Credential $cred -ServerUrl $serverUrl -OrganizationName $orgName 

  ## Connecting to On-Online (note: uncomment one of the two "$conn=" rows above/below)  
  #$conn = Connect-CrmOnline -Credential $cred -ServerUrl $serverUrl
  
  ## ---------------------------------------------------------------------------------------------------------
  
  # Connecting to a Dynamics instance/organization using "INTERACTIVE MODE" / WIZARD MODE
  # Note: Comment the row below in case you decided to uncomment the block above to connect smoothly   
  $conn = Build-CrmConnection -InteractiveMode -Verbose
  Set-CrmConnectionTimeout -conn $conn -TimeoutInSeconds 600
  Export-AdvancedMultiSelectSolutions -CrmConn $conn -Verbose

  Export-DemoForAdvancedMultiSelectSolutions -CrmConn $conn -Verbose
}
catch [System.Managment.Automation.ActionPreferenceStopException]{
  throw $_.Exception
}

catch [Exception] {
  throw $_.Exception
}