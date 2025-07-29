using System;
using WincajaLicenseManager;
using Newtonsoft.Json.Linq;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("Testing Hybrid License Deactivation");
            Console.WriteLine("===================================\n");

            // Create license manager
            var licenseManager = new WincajaLicenseManagerImpl();

            // Step 1: Check initial status
            Console.WriteLine("1. Checking initial license status...");
            var initialStatusResult = licenseManager.GetLicenseStatus();
            var initialStatusJson = JObject.Parse(initialStatusResult);
            
            Console.WriteLine($"Initial Status: {initialStatusJson["status"]}");
            Console.WriteLine($"Valid: {initialStatusJson["isValid"]}");
            Console.WriteLine();

            // Step 2: Activate license if not already active
            string testLicenseKey = "LK2H-A28M-BQDU-SH5K-XWZ1";
            
            if (!initialStatusJson["isValid"].Value<bool>())
            {
                Console.WriteLine("2. Activating license...");
                Console.WriteLine($"License key: {testLicenseKey}");
                
                var activateResult = licenseManager.ActivateLicense(testLicenseKey);
                var activateJson = JObject.Parse(activateResult);
                
                Console.WriteLine("Activation Result:");
                Console.WriteLine(activateJson.ToString(Newtonsoft.Json.Formatting.Indented));
                Console.WriteLine();

                if (activateJson["success"].Value<bool>())
                {
                    Console.WriteLine("✓ License activated successfully!");
                }
                else
                {
                    Console.WriteLine("✗ License activation failed!");
                    Console.WriteLine("Cannot proceed with deactivation test.");
                    return;
                }
            }
            else
            {
                Console.WriteLine("2. License is already active, proceeding to deactivation...");
            }

            // Step 3: Verify license is now active
            Console.WriteLine("\n3. Verifying license is active...");
            var activeStatusResult = licenseManager.GetLicenseStatus();
            var activeStatusJson = JObject.Parse(activeStatusResult);
            
            Console.WriteLine($"Current Status: {activeStatusJson["status"]}");
            Console.WriteLine($"Valid: {activeStatusJson["isValid"]}");
            Console.WriteLine($"License Key: {activeStatusJson["licenseKey"]}");
            Console.WriteLine();

            // Step 4: Test hybrid deactivation
            if (activeStatusJson["isValid"].Value<bool>())
            {
                Console.WriteLine("4. Testing hybrid deactivation...");
                Console.WriteLine("Attempting server-side deactivation first...");
                
                var deactivateResult = licenseManager.DeactivateLicense();
                var deactivateJson = JObject.Parse(deactivateResult);
                
                Console.WriteLine("Deactivation Result:");
                Console.WriteLine(deactivateJson.ToString(Newtonsoft.Json.Formatting.Indented));
                Console.WriteLine();

                if (deactivateJson["success"].Value<bool>())
                {
                    string deactivationType = deactivateJson["deactivationType"]?.Value<string>() ?? "Unknown";
                    bool serverUpdated = deactivateJson["serverUpdated"]?.Value<bool>() ?? false;
                    bool localOnly = deactivateJson["localOnly"]?.Value<bool>() ?? false;

                    Console.WriteLine($"✓ License deactivated successfully!");
                    Console.WriteLine($"Deactivation type: {deactivationType}");
                    Console.WriteLine($"Server updated: {serverUpdated}");
                    Console.WriteLine($"Local only: {localOnly}");
                    
                    if (deactivateJson["remainingActivations"] != null)
                    {
                        Console.WriteLine($"Remaining activations: {deactivateJson["remainingActivations"]}");
                    }

                    if (!string.IsNullOrEmpty(deactivateJson["warning"]?.Value<string>()))
                    {
                        Console.WriteLine($"⚠️  Warning: {deactivateJson["warning"]}");
                    }
                    
                    // Step 5: Verify deactivation
                    Console.WriteLine("\n5. Verifying deactivation...");
                    var finalStatusResult = licenseManager.GetLicenseStatus();
                    var finalStatusJson = JObject.Parse(finalStatusResult);
                    
                    Console.WriteLine("Status After Deactivation:");
                    Console.WriteLine(finalStatusJson.ToString(Newtonsoft.Json.Formatting.Indented));
                    
                    if (!finalStatusJson["isValid"].Value<bool>())
                    {
                        Console.WriteLine("\n✅ SUCCESS - License is no longer active locally");
                        if (serverUpdated)
                        {
                            Console.WriteLine("🌐 Server database has been updated");
                            Console.WriteLine("🎉 Hybrid deactivation completed successfully!");
                        }
                        else if (localOnly)
                        {
                            Console.WriteLine("⚠️  Local-only deactivation completed");
                            Console.WriteLine("💡 Contact support if you need to free up the server activation slot");
                        }
                    }
                    else
                    {
                        Console.WriteLine("\n✗ WARNING - License still appears to be active");
                    }
                }
                else
                {
                    Console.WriteLine("❌ Deactivation failed!");
                    Console.WriteLine($"Error: {deactivateJson["error"]}");
                    
                    bool canForceLocal = deactivateJson["canForceLocal"]?.Value<bool>() ?? false;
                    if (canForceLocal)
                    {
                        Console.WriteLine("\n💡 Option available: Force local-only deactivation");
                        Console.WriteLine("This will remove the license locally but won't update the server.");
                        Console.WriteLine("The activation slot on the server will remain consumed.");
                        
                        // For demonstration, automatically proceed with local-only deactivation
                        Console.WriteLine("Automatically proceeding with local-only deactivation for demo...");
                        
                        Console.WriteLine("\n6. Attempting force local-only deactivation...");
                        var forceLocalResult = licenseManager.DeactivateLicense(true);
                        var forceLocalJson = JObject.Parse(forceLocalResult);
                        
                        Console.WriteLine("Force Local Deactivation Result:");
                        Console.WriteLine(forceLocalJson.ToString(Newtonsoft.Json.Formatting.Indented));
                        
                        if (forceLocalJson["success"].Value<bool>())
                        {
                            Console.WriteLine("\n✅ Local-only deactivation completed");
                            Console.WriteLine("⚠️  Important: The server activation slot is still consumed");
                            Console.WriteLine("💡 Contact support to free up the server activation slot");
                            
                            // Verify final status
                            Console.WriteLine("\n7. Verifying final status...");
                            var finalLocalStatus = licenseManager.GetLicenseStatus();
                            var finalLocalJson = JObject.Parse(finalLocalStatus);
                            
                            Console.WriteLine("Final Status After Local Deactivation:");
                            Console.WriteLine(finalLocalJson.ToString(Newtonsoft.Json.Formatting.Indented));
                            
                            if (!finalLocalJson["isValid"].Value<bool>())
                            {
                                Console.WriteLine("\n✅ SUCCESS: Local license file removed");
                                Console.WriteLine("⚠️  IMPORTANT: Database record still exists (server not updated)");
                                Console.WriteLine("📊 This demonstrates the hybrid approach working correctly!");
                            }
                        }
                        else
                        {
                            Console.WriteLine("\n❌ Local-only deactivation also failed!");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("No active license found to deactivate.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
