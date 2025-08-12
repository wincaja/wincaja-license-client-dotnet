$body = @{
    licenseKey = 'SQCN-92ZZ-AJI2-6WS8-7CM5'
    includeHardwareCheck = $true
    hardwareFingerprint = '64b50fc5409ca29a'
    activationId = 'fbc4fe74-82cc-4f0f-9038-ebed0bb8358b'
} | ConvertTo-Json

Write-Host "Testing validation with exact database values"
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