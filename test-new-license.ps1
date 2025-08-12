$body = @{
    licenseKey = 'SQCN-92ZZ-AJI2-6WS8-7CM5'
    includeHardwareCheck = $false
} | ConvertTo-Json

Write-Host "Testing validation WITHOUT hardware check for new license"
Write-Host "Body: $body"

try {
    $response = Invoke-RestMethod -Uri 'http://localhost:5173/api/licenses/validate' -Method POST -Body $body -ContentType 'application/json'
    Write-Host "Response:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 10
} catch {
    Write-Host "Error occurred:" -ForegroundColor Red
    Write-Host $_.Exception.Message
    if ($_.Exception.Response) {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response body:" -ForegroundColor Yellow
        Write-Host $responseBody
    }
} 