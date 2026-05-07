$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$files = @(
    @{ Path = 'c:\Users\monte\Desktop\GASTRONOMIA - PELIGRO\Inventario equipos.xlsb'; Name = 'equipos'; StartRow = 11; Type = 'equipment' },
    @{ Path = 'c:\Users\monte\Desktop\GASTRONOMIA - PELIGRO\inventario utensilios.xlsx'; Name = 'utensilios'; StartRow = 8; Type = 'utensil' },
    @{ Path = 'c:\Users\monte\Desktop\GASTRONOMIA - PELIGRO\tipos de utensilios v.xlsx'; Name = 'tipos_utensilios'; StartRow = 2; Type = 'type' }
)

$allData = @{}

foreach ($f in $files) {
    Write-Host "Processing $($f.Path)..."
    if (Test-Path $f.Path) {
        $wb = $excel.Workbooks.Open($f.Path)
        $sh = $wb.Sheets.Item(1)
        
        # Determine last row
        $lastRow = $sh.UsedRange.Rows.Count
        $data = @()
        
        for ($r=$f.StartRow; $r -le $lastRow; $r++) {
            $rowObj = @{}
            
            # Read columns based on type
            if ($f.Type -eq 'equipment') {
                $rowObj.InventoryNumber = $sh.Cells.Item($r, 2).Text
                $rowObj.TypeName = $sh.Cells.Item($r, 3).Text
                $rowObj.Description = $sh.Cells.Item($r, 4).Text
                $rowObj.OldCode = $sh.Cells.Item($r, 5).Text
            } elseif ($f.Type -eq 'utensil') {
                $rowObj.InventoryNumber = $sh.Cells.Item($r, 1).Text
                $rowObj.Name = $sh.Cells.Item($r, 2).Text
                $rowObj.Presentation = $sh.Cells.Item($r, 3).Text
                $rowObj.Unit = $sh.Cells.Item($r, 4).Text
                $rowObj.Stock = $sh.Cells.Item($r, 7).Text # INVENTARIO I/2020
                $rowObj.Notes = $sh.Cells.Item($r, 8).Text
            } elseif ($f.Type -eq 'type') {
                $rowObj.Name = $sh.Cells.Item($r, 1).Text
                $rowObj.Details = $sh.Cells.Item($r, 2).Text
                $rowObj.Unit = $sh.Cells.Item($r, 3).Text
                $rowObj.Quantity = $sh.Cells.Item($r, 4).Text
                $rowObj.Category = $sh.Cells.Item($r, 5).Text
            }
            
            # Only add if it has some data
            if ($rowObj.Values | Where-Object { $_ -ne "" }) {
                $data += $rowObj
            }
        }
        $allData[$f.Name] = $data
        $wb.Close($false)
    }
}

$excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null

$allData | ConvertTo-Json -Depth 10 | Out-File -FilePath "d:\Vuelta a Bolivia\Proyecto-Laboratorios-Univalle\tmp\migration_data.json" -Encoding utf8
Write-Host "Export completed to migration_data.json"
