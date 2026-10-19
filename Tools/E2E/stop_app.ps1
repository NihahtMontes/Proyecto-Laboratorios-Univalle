# Detiene la app que escucha en el puerto 5000 (app de QA o desarrollo) e imprime cuántos procesos siguen escuchando (0 = libre).
Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }
Start-Sleep 2
(Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue | Measure-Object).Count
