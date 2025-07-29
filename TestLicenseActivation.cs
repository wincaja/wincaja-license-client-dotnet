using System;
using WincajaLicenseManager;
using Newtonsoft.Json.Linq;

class TestLicenseActivation
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("Testing License Activation");
            Console.WriteLine("=========================\n");

            // Create license manager
            var licenseManager = new WincajaLicenseManagerImpl();

            // Test specific license key
            string licenseKey = "2OMG-Q9G9-2ACW-O9CZ-6TQS";
            Console.WriteLine($"Attempting to activate license: {licenseKey}");
            
            // First get hardware fingerprint
            Console.WriteLine("\n1. Getting hardware fingerprint...");
            var fingerprintResult = licenseManager.GetHardwareFingerprint();
            var fingerprintJson = JObject.Parse(fingerprintResult);
            Console.WriteLine($"Fingerprint Result: {fingerprintJson["success"]}");
            if (fingerprintJson["success"].Value<bool>())
            {
                Console.WriteLine($"Hardware Fingerprint: {fingerprintJson["fingerprint"]}");
            }

            // Now try activation
            Console.WriteLine("\n2. Activating license...");
            var result = licenseManager.ActivateLicense(licenseKey);
            var json = JObject.Parse(result);
            
            Console.WriteLine($"\nActivation Result:");
            Console.WriteLine($"Success: {json["success"]}");
            Console.WriteLine($"Error: {json["error"]}");
            Console.WriteLine($"Full Response: {json.ToString(Newtonsoft.Json.Formatting.Indented)}");

            if (json["success"].Value<bool>())
            {
                Console.WriteLine("\n✓ License activated successfully!");
                
                // Test validation
                Console.WriteLine("\n3. Testing license validation...");
                var validateResult = licenseManager.ValidateLicense();
                var validateJson = JObject.Parse(validateResult);
                Console.WriteLine($"Validation Result: {validateJson.ToString(Newtonsoft.Json.Formatting.Indented)}");
            }
            else
            {
                Console.WriteLine("\n✗ License activation failed");
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