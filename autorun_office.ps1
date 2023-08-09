Add-Type -AssemblyName Microsoft.VisualBasic
Add-Type -AssemblyName System.Windows.Forms

$appProcessName = "Unity"
$appWindowTitle = "sciencebirds_level_generator - LevelGenerator - PC, Mac & Linux Standalone - Unity 2019.3.4f1 Personal <DX11>"
$keystrokes = "^p"

$process = [System.Diagnostics.Process]::GetProcessesByName($appProcessName) | Where-Object {$_.MainWindowTitle -like "*$appWindowTitle*"}

if ($process) {
	while($true){
		[Microsoft.VisualBasic.Interaction]::AppActivate($process.Id)
		Start-Sleep -Milliseconds 500
		[System.Windows.Forms.SendKeys]::SendWait($keystrokes)
		Write-Host "Waiting 30 seconds..."
		Start-Sleep -Seconds 40
		[System.Windows.Forms.SendKeys]::SendWait($keystrokes)
		Write-Host "Waiting 5 seconds..."
		Start-Sleep -Seconds 2
	}

	
} else {
    Write-Host "Process '$appProcessName' not found."
}