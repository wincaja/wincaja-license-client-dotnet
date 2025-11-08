using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WincajaLicenseManager.Models;

namespace WincajaLicenseManager.Core
{
    internal class ApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ApiClient(string baseUrl = null)
        {
            _baseUrl = baseUrl ?? "https://licencias.wincaja.mx/api/licenses";
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<ActivationResponse> ActivateLicenseAsync(string licenseKey, System.Collections.Generic.Dictionary<string, object> hardwareInfo, string sslNumber = null)
        {
            try
            {
                Logger.LogDebug($"ApiClient.ActivateLicenseAsync - INICIO");
                var request = new ActivationRequest
                {
                    LicenseKey = licenseKey,
                    HardwareInfo = hardwareInfo,
                    BindingMode = "flexible",
                    SslNumber = sslNumber // NUEVO: Incluir SSL si se proporciona
                };

                // Use camelCase naming to match server expectations
                var jsonSettings = new JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                };
                var json = JsonConvert.SerializeObject(request, jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Debug output
                System.Diagnostics.Debug.WriteLine($"Sending request to: {_baseUrl}/activate");
                System.Diagnostics.Debug.WriteLine($"Request body: {json}");

                var response = await _httpClient.PostAsync($"{_baseUrl}/activate", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Debug output mejorado con información de diagnóstico
                System.Diagnostics.Debug.WriteLine($"Response status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response body: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<ActivationResponse>(responseContent);
                    
                    // NUEVO: Log información de diagnóstico si está disponible
                    if (result?.Diagnostic != null)
                    {
                        Console.WriteLine($"[INFO] Activación - RequestId: {result.RequestId}");
                        Console.WriteLine($"[INFO] Activación - Fase: {result.Diagnostic.Phase}");
                        if (!string.IsNullOrEmpty(result.Diagnostic.Hint))
                        {
                            Console.WriteLine($"[INFO] Activación - Pista: {result.Diagnostic.Hint}");
                        }
                    }
                    
                    return result ?? new ActivationResponse { Success = false, Error = "Invalid response from server" };
                }
                else
                {
                    // Try to parse error response
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<ActivationResponse>(responseContent);
                        return errorResponse ?? new ActivationResponse { Success = false, Error = $"Server error: {response.StatusCode} - {responseContent}" };
                    }
                    catch
                    {
                        return new ActivationResponse { Success = false, Error = $"Server error: {response.StatusCode} - {responseContent}" };
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return new ActivationResponse { Success = false, Error = "Request timed out" };
            }
            catch (HttpRequestException ex)
            {
                return new ActivationResponse { Success = false, Error = $"Network error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ActivationResponse { Success = false, Error = $"Unexpected error: {ex.Message}" };
            }
        }

        public ActivationResponse ActivateLicense(string licenseKey, System.Collections.Generic.Dictionary<string, object> hardwareInfo, string sslNumber = null)
        {
            // Synchronous wrapper for COM compatibility
            try
            {
                Logger.LogDebug($"ApiClient.ActivateLicense - INICIO");
                var task = Task.Run(async () => await ActivateLicenseAsync(licenseKey, hardwareInfo, sslNumber));
                Logger.LogDebug($"ApiClient.ActivateLicense - Task creado, esperando resultado...");
                var result = task.Result;
                Logger.LogDebug($"ApiClient.ActivateLicense - Task completado: Success={result?.Success}");
                return result;
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerException;
                var errorMessage = innerEx?.Message ?? ex.Message ?? "Activation failed";
                Logger.LogDebug($"ApiClient.ActivateLicense - AggregateException: {errorMessage}");
                Logger.LogDebug($"ApiClient.ActivateLicense - InnerException: {innerEx?.GetType().Name}: {innerEx?.Message}");
                return new ActivationResponse { Success = false, Error = $"Activation error: {errorMessage}" };
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"ApiClient.ActivateLicense - Exception: {ex.GetType().Name}: {ex.Message}");
                return new ActivationResponse { Success = false, Error = $"Activation error: {ex.Message}" };
            }
        }

        public async Task<ValidationResponse> ValidateLicenseAsync(string licenseKey, string activationId, System.Collections.Generic.Dictionary<string, object> hardwareInfo, string sslNumber = null)
        {
            try
            {
                var request = new ValidationRequest
                {
                    LicenseKey = licenseKey,
                    ActivationId = activationId,
                    HardwareInfo = hardwareInfo,
                    IncludeHardwareCheck = false,
                    SslNumber = sslNumber // NUEVO: Incluir SSL si se proporciona
                };

                // Use camelCase naming to match server expectations
                var jsonSettings = new JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                };
                var json = JsonConvert.SerializeObject(request, jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/validate", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Debug output para ValidationLicenseAsync
                Console.WriteLine($"[DEBUG] ValidateLicenseAsync - URL: {_baseUrl}/validate");
                Console.WriteLine($"[DEBUG] ValidateLicenseAsync - Server response ({response.StatusCode}): {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    // Use camelCase deserialization settings to match server response
                    var jsonSettingsx = new JsonSerializerSettings
                    {
                        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                    };
                    var result = JsonConvert.DeserializeObject<ValidationResponse>(responseContent, jsonSettingsx);
                    
                    // NUEVO: Calcular HasLicense basado en los datos del servidor
                    if (result != null)
                    {
                        result.HasLicense = CalculateHasLicense(result);
                    }
                    
                    // Debug output del objeto deserializado
                    Console.WriteLine($"[DEBUG] ValidateLicenseAsync - Deserialized result: Success={result?.Success}, Valid={result?.Valid}, HasLicense={result?.HasLicense}");
                    if (result?.License != null)
                    {
                        Console.WriteLine($"[DEBUG] ValidateLicenseAsync - License.ExpiresAt: {result.License.ExpiresAt}");
                    }
                    if (result?.Data != null)
                    {
                        Console.WriteLine($"[DEBUG] ValidateLicenseAsync - Data.ExpiresAt: {result.Data.ExpiresAt}");
                    }
                    
                    return result ?? new ValidationResponse { Success = false, Error = "Invalid response from server" };
                }
                else
                {
                    // Try to parse error response
                    try
                    {
                        var jsonSettingsx = new JsonSerializerSettings
                        {
                            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                        };
                        var errorResponse = JsonConvert.DeserializeObject<ValidationResponse>(responseContent, jsonSettingsx);
                        if (errorResponse != null)
                        {
                            errorResponse.StatusCode = (int)response.StatusCode;
                            return errorResponse;
                        }
                        return new ValidationResponse { Success = false, Error = $"Server error: {response.StatusCode}", StatusCode = (int)response.StatusCode };
                    }
                    catch
                    {
                        // Check if this is a 401 Unauthorized with license-related error
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && responseContent.ToLowerInvariant().Contains("license"))
                        {
                            return new ValidationResponse { 
                                Success = false, 
                                Error = "License has been revoked", 
                                StatusCode = 401 
                            };
                        }
                        return new ValidationResponse { Success = false, Error = $"Server error: {response.StatusCode}", StatusCode = (int)response.StatusCode };
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return new ValidationResponse { Success = false, Error = "Request timed out" };
            }
            catch (HttpRequestException ex)
            {
                return new ValidationResponse { Success = false, Error = $"Network error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ValidationResponse { Success = false, Error = $"Unexpected error: {ex.Message}" };
            }
        }

        public ValidationResponse ValidateLicense(string licenseKey, string activationId, System.Collections.Generic.Dictionary<string, object> hardwareInfo, string sslNumber = null)
        {
            // Synchronous wrapper for COM compatibility
            try
            {
                var task = Task.Run(async () => await ValidateLicenseAsync(licenseKey, activationId, hardwareInfo, sslNumber));
                return task.Result;
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerException;
                return new ValidationResponse { Success = false, Error = innerEx?.Message ?? "Validation failed" };
            }
        }

        public async Task<ValidationResponse> ValidateLicenseHardwareAsync(string licenseKey, string hardwareFingerprint, string activationId = null, string sslNumber = null)
        {
            try
            {
                // Crear request dinámico para omitir sslNumber cuando es null
                var request = new Dictionary<string, object>
                {
                    ["licenseKey"] = licenseKey,
                    ["includeHardwareCheck"] = true,
                    ["hardwareFingerprint"] = hardwareFingerprint,
                    ["activationId"] = activationId
                };

                // Solo agregar sslNumber si NO es null o vacío
                if (!string.IsNullOrEmpty(sslNumber))
                {
                    request["sslNumber"] = sslNumber;
                }

                // Use camelCase naming
                var jsonSettings = new JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                };
                var json = JsonConvert.SerializeObject(request, jsonSettings);
                
                // Debug output mejorado para ValidateLicenseHardwareAsync
                Console.WriteLine($"[DEBUG] ValidateLicenseHardwareAsync - URL: {_baseUrl}/validate");
                Console.WriteLine($"[DEBUG] ValidateLicenseHardwareAsync - Request body: {json}");
                Console.WriteLine($"[DEBUG] ValidateLicenseHardwareAsync - SSL Number: {(string.IsNullOrEmpty(sslNumber) ? "OMITIDO (null/vacío)" : sslNumber)}");
                Console.WriteLine($"[DEBUG] ValidateLicenseHardwareAsync - SSL incluido en request: {!string.IsNullOrEmpty(sslNumber)}");
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/validate", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[DEBUG] Server response ({response.StatusCode}): {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    // Use camelCase deserialization settings to match server response
                    var jsonSettingsx = new JsonSerializerSettings
                    {
                        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                    };
                    var result = JsonConvert.DeserializeObject<ValidationResponse>(responseContent, jsonSettingsx);


                    // NUEVO: Calcular HasLicense basado en los datos del servidor
                    if (result != null)
                    {
                        result.HasLicense = CalculateHasLicense(result);

                        if (result.HasLicense)
                        {
                            result.Valid = true;
                            result.Success = true;
                        }
                    }


                    if (result.Validation.HardwareValid == false)
                    {
                        Console.WriteLine($"[DEBUG] Hardware validation failed for license {licenseKey} with activation {activationId}");
                        result.Valid = true;
                        result.Success = true;
                        result.HasLicense = false;
                    }

                    if (result.Validation.HardwareValidationDetails != null)
                    {
                        Console.WriteLine($"[DEBUG] HardwareValidationDetails: {JsonConvert.SerializeObject(result.Validation.HardwareValidationDetails)}");
                       // [DEBUG] HardwareValidationDetails: { "reason":"Activation not found for this license"}
                       if(result.Validation.HardwareValidationDetails is Newtonsoft.Json.Linq.JObject jObject &&
                          jObject.TryGetValue("reason", out var reasonToken))
                       {
                           var reason = reasonToken.ToString();
                           Console.WriteLine($"[DEBUG] Hardware validation failed reason: {reason}");
                        }
                    }

                 
                    
                    // Debug output del objeto deserializado
                    Console.WriteLine($"[DEBUG] ValidateLicenseHardwareAsync - Deserialized result: Success={result?.Success}, Valid={result?.Valid}, HasLicense={result?.HasLicense}");
                    if (result?.License != null)
                    {
                        Console.WriteLine($"[DEBUG] ValidateLicenseHardwareAsync - License.ExpiresAt: {result.License.ExpiresAt}");
                    }
                    if (result?.Data != null)
                    {
                        Console.WriteLine($"[DEBUG] ValidateLicenseHardwareAsync - Data.ExpiresAt: {result.Data.ExpiresAt}");
                    }
                    
                    return result ?? new ValidationResponse { Success = false, Error = "Invalid response from server" };
                }
                else
                {
                    try
                    {
                        var jsonSettingsx = new JsonSerializerSettings
                        {
                            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                        };
                        var errorResponse = JsonConvert.DeserializeObject<ValidationResponse>(responseContent, jsonSettingsx);
                        if (errorResponse != null)
                        {
                            errorResponse.StatusCode = (int)response.StatusCode;
                            return errorResponse;
                        }
                        return new ValidationResponse { Success = false, Error = $"Server error: {response.StatusCode}", StatusCode = (int)response.StatusCode };
                    }
                    catch
                    {
                        // Check if this is a 401 Unauthorized with license-related error
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && responseContent.ToLowerInvariant().Contains("license"))
                        {
                            return new ValidationResponse { 
                                Success = false, 
                                Error = "License has been revoked", 
                                StatusCode = 401 
                            };
                        }
                        return new ValidationResponse { Success = false, Error = $"Server error: {response.StatusCode}", StatusCode = (int)response.StatusCode };
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return new ValidationResponse { Success = false, Error = "Request timed out" };
            }
            catch (HttpRequestException ex)
            {
                return new ValidationResponse { Success = false, Error = $"Network error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ValidationResponse { Success = false, Error = $"Unexpected error: {ex.Message}" };
            }
        }

        public ValidationResponse ValidateLicenseHardware(string licenseKey, string hardwareFingerprint, string activationId = null, string sslNumber = null)
        {
            try
            {
                var task = Task.Run(async () => await ValidateLicenseHardwareAsync(licenseKey, hardwareFingerprint, activationId, sslNumber));
                return task.Result;
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerException;
                return new ValidationResponse { Success = false, Error = innerEx?.Message ?? "Validation failed" };
            }
        }

        // NUEVO: Método para validar licencia sin verificación de hardware
        // Útil para consultar el estado ssl.used antes de activar
        public async Task<ValidationResponse> ValidateLicenseAsync(string licenseKey)
        {
            try
            {
                var request = new
                {
                    licenseKey = licenseKey,
                    includeHardwareCheck = false
                };

                var jsonSettings = new JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                };
                var json = JsonConvert.SerializeObject(request, jsonSettings);
                
                Console.WriteLine($"[DEBUG] ValidateLicenseAsync - URL: {_baseUrl}/validate");
                Console.WriteLine($"[DEBUG] ValidateLicenseAsync - Request body: {json}");
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/validate", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[DEBUG] Server response ({response.StatusCode}): {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<ValidationResponse>(responseContent, jsonSettings);
                    
                    if (result != null)
                    {
                        result.HasLicense = CalculateHasLicense(result);
                        
                        // Debug output del objeto deserializado
                        Console.WriteLine($"[DEBUG] ValidateLicenseAsync - Deserialized result: Valid={result.Valid}, HasLicense={result.HasLicense}");
                        if (result.Ssl != null)
                        {
                            Console.WriteLine($"[DEBUG] ValidateLicenseAsync - SSL Info: Required={result.Ssl.Required}, Used={result.Ssl.Used}, Migrated={result.Ssl.MigratedFromLegacy}");
                        }
                    }
                    
                    return result ?? new ValidationResponse { Success = false, Error = "Invalid response from server" };
                }
                else
                {
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<ValidationResponse>(responseContent, jsonSettings);
                        if (errorResponse != null)
                        {
                            errorResponse.StatusCode = (int)response.StatusCode;
                            errorResponse.HasLicense = CalculateHasLicense(errorResponse);
                            return errorResponse;
                        }
                        return new ValidationResponse { Success = false, Error = $"Server error: {response.StatusCode}", StatusCode = (int)response.StatusCode };
                    }
                    catch
                    {
                        return new ValidationResponse { Success = false, Error = $"Server error: {response.StatusCode}", StatusCode = (int)response.StatusCode };
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return new ValidationResponse { Success = false, Error = "Request timed out" };
            }
            catch (HttpRequestException ex)
            {
                return new ValidationResponse { Success = false, Error = $"Network error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ValidationResponse { Success = false, Error = $"Unexpected error: {ex.Message}" };
            }
        }

        public ValidationResponse ValidateLicense(string licenseKey)
        {
            try
            {
                var task = Task.Run(async () => await ValidateLicenseAsync(licenseKey));
                return task.Result;
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerException;
                return new ValidationResponse { Success = false, Error = innerEx?.Message ?? "Validation failed" };
            }
        }

        public async Task<DeactivationResponse> DeactivateLicenseAsync(string licenseKey, string activationId, string reason = null)
        {
            try
            {
                var request = new
                {
                    licenseKey = licenseKey,
                    activationId = activationId,
                    reason = reason ?? "User requested deactivation"
                };

                // Use camelCase naming to match server expectations
                var jsonSettings = new JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                };
                var json = JsonConvert.SerializeObject(request, jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/deactivate", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<DeactivationResponse>(responseContent);
                    return result ?? new DeactivationResponse { Success = false, Error = "Invalid response from server" };
                }
                else
                {
                    // Try to parse error response
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<DeactivationResponse>(responseContent);
                        return errorResponse ?? new DeactivationResponse { Success = false, Error = $"Server error: {response.StatusCode}" };
                    }
                    catch
                    {
                        return new DeactivationResponse { Success = false, Error = $"Server error: {response.StatusCode} - {responseContent}" };
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return new DeactivationResponse { Success = false, Error = "Request timed out" };
            }
            catch (HttpRequestException ex)
            {
                return new DeactivationResponse { Success = false, Error = $"Network error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new DeactivationResponse { Success = false, Error = $"Unexpected error: {ex.Message}" };
            }
        }

        public DeactivationResponse DeactivateLicense(string licenseKey, string activationId, string reason = null)
        {
            // Synchronous wrapper for COM compatibility
            try
            {
                var task = Task.Run(async () => await DeactivateLicenseAsync(licenseKey, activationId, reason));
                return task.Result;
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerException;
                var errorMessage = innerEx?.Message ?? ex.Message ?? "Deactivation failed";
                return new DeactivationResponse { Success = false, Error = $"Deactivation error: {errorMessage}" };
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        // NUEVO: Método para calcular si la PC tiene licencia
        private bool CalculateHasLicense(ValidationResponse response)
        {
            if (response == null) return false;

            // Caso 1: Si hay activaciones registradas, tiene licencia
            if (response.Validation?.CurrentActivations > 0)
            {
                return true;
            }

            // Caso 2: Si hay información de licencia válida, tiene licencia
            if (response.License != null && !string.IsNullOrEmpty(response.License.LicenseKey))
            {
                return true;
            }

            // Caso 3: Si hay información SSL usada, tiene licencia
            if (response.Ssl?.Used == true)
            {
                return true;
            }

            // Caso 4: Si hay datos de validación con información de licencia
            if (response.Data != null && !string.IsNullOrEmpty(response.Data.Status))
            {
                return true;
            }

            return false;
        }
    }
}