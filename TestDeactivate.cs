using System;
using WincajaLicenseManager;
using Newtonsoft.Json.Linq;

class TestDeactivate
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("Testing License Deactivation");
            Console.WriteLine("============================\n");

            // Create license manager
            var licenseManager = new WincajaLicenseManagerImpl();

            // Check current status first
            Console.WriteLine("1. Checking current license status...");
            var statusResult = licenseManager.GetLicenseStatus();
            var statusJson = JObject.Parse(statusResult);
            
            Console.WriteLine($"Current Status: {statusJson["status"]}");
            Console.WriteLine($"Valid: {statusJson["success"]}");
            Console.WriteLine($"License Key: {statusJson["licenseKey"]}");
            Console.WriteLine();

            if (statusJson["success"].Value<bool>())
            {
                Console.WriteLine("2. Deactivating license...");
                var deactivateResult = licenseManager.DeactivateLicense();
                var deactivateJson = JObject.Parse(deactivateResult);
                
                Console.WriteLine("Deactivation Result:");
                Console.WriteLine(deactivateJson.ToString(Newtonsoft.Json.Formatting.Indented));
                Console.WriteLine();

                if (deactivateJson["success"].Value<bool>())
                {
                    Console.WriteLine("✓ License deactivated successfully!");
                    
                    // Check status again to confirm
                    Console.WriteLine("\n3. Verifying deactivation...");
                    var newStatusResult = licenseManager.GetLicenseStatus();
                    var newStatusJson = JObject.Parse(newStatusResult);
                    
                    Console.WriteLine("Status After Deactivation:");
                    Console.WriteLine(newStatusJson.ToString(Newtonsoft.Json.Formatting.Indented));
                    
                    if (!newStatusJson["success"].Value<bool>())
                    {
                        Console.WriteLine("\n✓ Confirmed - License is no longer active locally");
                    }
                }
                else
                {
                    Console.WriteLine("✗ Deactivation failed!");
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