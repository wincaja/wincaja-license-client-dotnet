using System;
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

        public async Task<ActivationResponse> ActivateLicenseAsync(string licenseKey, System.Collections.Generic.Dictionary<string, object> hardwareInfo)
        {
            try
            {
                var request = new ActivationRequest
                {
                    LicenseKey = licenseKey,
                    HardwareInfo = hardwareInfo,
                    BindingMode = "flexible"
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

                // Debug output
                System.Diagnostics.Debug.WriteLine($"Response status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response body: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<ActivationResponse>(responseContent);
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

        public ActivationResponse ActivateLicense(string licenseKey, System.Collections.Generic.Dictionary<string, object> hardwareInfo)
        {
            // Synchronous wrapper for COM compatibility
            try
            {
                var task = Task.Run(async () => await ActivateLicenseAsync(licenseKey, hardwareInfo));
                return task.Result;
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerException;
                var errorMessage = innerEx?.Message ?? ex.Message ?? "Activation failed";
                return new ActivationResponse { Success = false, Error = $"Activation error: {errorMessage}" };
            }
        }

        public async Task<ValidationResponse> ValidateLicenseAsync(string licenseKey, string activationId, System.Collections.Generic.Dictionary<string, object> hardwareInfo)
        {
            try
            {
                var request = new ValidationRequest
                {
                    LicenseKey = licenseKey,
                    ActivationId = activationId,
                    HardwareInfo = hardwareInfo,
                    IncludeHardwareCheck = false
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

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<ValidationResponse>(responseContent);
                    return result ?? new ValidationResponse { Success = false, Error = "Invalid response from server" };
                }
                else
                {
                    // Try to parse error response
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<ValidationResponse>(responseContent);
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

        public ValidationResponse ValidateLicense(string licenseKey, string activationId, System.Collections.Generic.Dictionary<string, object> hardwareInfo)
        {
            // Synchronous wrapper for COM compatibility
            try
            {
                var task = Task.Run(async () => await ValidateLicenseAsync(licenseKey, activationId, hardwareInfo));
                return task.Result;
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerException;
                return new ValidationResponse { Success = false, Error = innerEx?.Message ?? "Validation failed" };
            }
        }

        public async Task<ValidationResponse> ValidateLicenseHardwareAsync(string licenseKey, string hardwareFingerprint, string activationId = null)
        {
            try
            {
                var request = new
                {
                    licenseKey = licenseKey,
                    includeHardwareCheck = true,
                    hardwareFingerprint = hardwareFingerprint,
                    activationId = activationId
                };

                // Use camelCase naming
                var jsonSettings = new JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                };
                var json = JsonConvert.SerializeObject(request, jsonSettings);
                Console.WriteLine($"[DEBUG] Sending validation request: {json}");
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/validate", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[DEBUG] Server response ({response.StatusCode}): {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<ValidationResponse>(responseContent);
                    return result ?? new ValidationResponse { Success = false, Error = "Invalid response from server" };
                }
                else
                {
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<ValidationResponse>(responseContent);
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

        public ValidationResponse ValidateLicenseHardware(string licenseKey, string hardwareFingerprint, string activationId = null)
        {
            try
            {
                var task = Task.Run(async () => await ValidateLicenseHardwareAsync(licenseKey, hardwareFingerprint, activationId));
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
    }
}