$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$files = @(
    'c:\Users\monte\Desktop\GASTRONOMIA - PELIGRO\Inventario equipos.xlsb',
    'c:\Users\monte\Desktop\GASTRONOMIA - PELIGRO\inventario utensilios.xlsx',
    'c:\Users\monte\Desktop\GASTRONOMIA - PELIGRO\tipos de equipos.xlsb',
    'c:\Users\monte\Desktop\GASTRONOMIA - PELIGRO\tipos de utensilios v.xlsx'
)

$results = ""

foreach ($f in $files) {
    $results += "`n=== FILE: $f ===`n"
    if (Test-Path $f) {
        try {
            $wb = $excel.Workbooks.Open($f)
            $sh = $wb.Sheets.Item(1)
            for ($r=1; $r -le 20; $r++) {
                $line = "$($r): "
                for ($c=1; $c -le 10; $c++) {
                    $val = $sh.Cells.Item($r, $c).Text
                    $line += "[$val] | "
                }
                $results += "$line`n"
            }
            $wb.Close($false)
        } catch {
            $results += "Error processing $f : $($_.Exception.Message)`n"
        }
    } else {
        $results += "File not found: $f`n"
    }
}
$excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
$results | Out-File -FilePath "d:\Vuelta a Bolivia\Proyecto-Laboratorios-Univalle\tmp\excel_analysis_v2.txt" -Encoding utf8
