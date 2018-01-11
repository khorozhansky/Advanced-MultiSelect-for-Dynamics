#
# This script connects to a Dynamics CRM/365 instance/organization 
# and imports both 'AdvancedMultiSelectForDynamics' and 'DemoForAdvancedMultiSelect' UNMANAGED solutions there
#  

try{
  Clear-Host 
  $error.clear()
  $invokationFolder = $PSScriptRoot
  Import-Module ("$invokationFolder\CommonLib.ps1")
  Write-Output "Connecting to a server..."
  $conn = Build-CrmConnection -InteractiveMode -Verbose
  $solutionFolder = "$invokationFolder\Solutions\"
  $version = "2.2.1.0"
  Import-AdvancedMultiSelectSolutions -CrmConn $conn -Version $version -OverwriteUnManagedCustomizations -Verbose
}

catch [Exception] {
  throw $_.Exception
}