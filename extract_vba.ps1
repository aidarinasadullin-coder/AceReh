# Extract VBA using Office COM via PowerShell
$excel = $null
$workbook = $null

try {
    # Create Excel application
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $excel.DisplayAlerts = $false
    
    # Open workbook
    $filePath = "C:\Айдар\IA\ace\план-исправлений\gidravlica.xls"
    $workbook = $excel.Workbooks.Open($filePath)
    
    # Access VBA project
    $vbaProject = $workbook.VBProject
    
    $output = @()
    $output += "=== VBA Code from gidravlica.xls ==="
    $output += ""
    
    # Extract all modules
    foreach ($component in $vbaProject.VBComponents) {
        if ($component.Type -eq 1 -or $component.Type -eq 2 -or $component.Type -eq 3) {
            # Type 1 = Standard Module, 2 = Class Module, 3 = UserForm
            $moduleName = $component.Name
            $codeModule = $component.CodeModule
            $lineCount = $codeModule.CountOfLines
            
            if ($lineCount -gt 0) {
                $output += "=== Module: $moduleName ==="
                $code = $codeModule.Lines(1, $lineCount)
                $output += $code
                $output += ""
                $output += "---"
                $output += ""
            }
        }
    }
    
    # Save output
    $outputFile = "C:\Айдар\IA\ace\Work\vba_full_code.txt"
    $output | Out-File -FilePath $outputFile -Encoding UTF8
    
    Write-Host "VBA code extracted successfully!"
    Write-Host "Saved to: $outputFile"
    Write-Host ""
    Write-Host "=== Extracted VBA Code Preview ==="
    $output | Select-Object -First 100 | ForEach-Object { Write-Host $_ }
    
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message
} finally {
    # Cleanup
    if ($workbook) {
        $workbook.Close($false)
    }
    if ($excel) {
        $excel.Quit()
    }
    
    # Release COM objects
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($workbook) | Out-Null
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
    [GC]::Collect()
}
