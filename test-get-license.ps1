$licenseId = 'T28K-7PNT-OK0H-0C5U-IH7T'

Write-Host "Getting license details for: $licenseId"

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5173/api/licenses/$licenseId" -Method GET
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