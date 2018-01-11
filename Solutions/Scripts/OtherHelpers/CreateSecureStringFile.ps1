# use it to create a file containing encrypted password as needed if you'd like
$invokationFolder = Split-Path -Parent $MyInvocation.MyCommand.Path
Read-Host -Prompt "Enter your tenant password" -AsSecureString | ConvertFrom-SecureString | Out-File "$invokationFolder\..\testcred.txt"
