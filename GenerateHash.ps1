# Script para generar hash BCrypt de Admin123!
Add-Type -AssemblyName "System.Web"

# Simulamos BCrypt con un hash conocido - usaremos el endpoint de la API
$json = '{"password":"Admin123!"}'
$body = [System.Text.Encoding]::UTF8.GetBytes($json)

try {
	$request = [System.Net.HttpWebRequest]::Create("https://localhost:7269/api/auth/generate-hash")
	$request.Method = "POST"
	$request.ContentType = "application/json"
	$request.ContentLength = $body.Length
	$request.ServerCertificateValidationCallback = {$true}

	$stream = $request.GetRequestStream()
	$stream.Write($body, 0, $body.Length)
	$stream.Close()

	$response = $request.GetResponse()
	$reader = New-Object System.IO.StreamReader($response.GetResponseStream())
	$result = $reader.ReadToEnd()

	Write-Host "Respuesta de la API:" -ForegroundColor Green
	Write-Host $result

	$reader.Close()
	$response.Close()
}
catch {
	Write-Host "Error: $_" -ForegroundColor Red
}
