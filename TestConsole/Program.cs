using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WincajaLicenseManager;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Wincaja License Manager Test Console");
            Console.WriteLine("====================================\n");

            try
            {
                // Create the license manager instance
                IWincajaLicenseManager licenseManager = new WincajaLicenseManagerImpl();

                // Test the specific license key first
                string testLicenseKey = "LK2H-A28M-BQDU-SH5K-XWZ1";
                Console.WriteLine($"Testing license key: {testLicenseKey}");
                Console.WriteLine("=====================================\n");

                // Get hardware fingerprint first
                Console.WriteLine("1. Getting hardware fingerprint...");
                var fingerprintResult = licenseManager.GetHardwareFingerprint();
                var fingerprintJson = JObject.Parse(fingerprintResult);
                Console.WriteLine($"Fingerprint success: {fingerprintJson["success"]}");
                if (fingerprintJson["success"].Value<bool>())
                {
                    Console.WriteLine($"Hardware fingerprint: {fingerprintJson["fingerprint"]}");
                }
                Console.WriteLine();

                // Try to activate the license
                Console.WriteLine("2. Attempting license activation...");
                var activationResult = licenseManager.ActivateLicense(testLicenseKey);
                var activationJson = JObject.Parse(activationResult);
                
                Console.WriteLine("Activation Result:");
                Console.WriteLine(activationJson.ToString(Formatting.Indented));
                Console.WriteLine();

                if (activationJson["success"].Value<bool>())
                {
                    Console.WriteLine("✓ License activated successfully!");
                    
                    // Test validation
                    Console.WriteLine("\n3. Testing license validation...");
                    var validateResult = licenseManager.ValidateLicense();
                    var validateJson = JObject.Parse(validateResult);
                    Console.WriteLine("Validation Result:");
                    Console.WriteLine(validateJson.ToString(Formatting.Indented));
                }
                else
                {
                    Console.WriteLine("✗ License activation failed!");
                    Console.WriteLine($"Error: {activationJson["error"]}");
                }

                Console.WriteLine("\n" + new string('=', 50));
                Console.WriteLine("Interactive Mode - Choose an option:");

                // Continue with interactive mode
                while (true)
                {
                    Console.WriteLine("\nOptions:");
                    Console.WriteLine("1. Get Hardware Fingerprint");
                    Console.WriteLine("2. Activate License");
                    Console.WriteLine("3. Validate License");
                    Console.WriteLine("4. Get License Status");
                    Console.WriteLine("5. Deactivate License");
                    Console.WriteLine("6. Exit");
                    Console.Write("\nSelect option: ");

                    var option = Console.ReadLine();

                    switch (option)
                    {
                        case "1":
                            GetHardwareFingerprint(licenseManager);
                            break;

                        case "2":
                            ActivateLicense(licenseManager);
                            break;

                        case "3":
                            ValidateLicense(licenseManager);
                            break;

                        case "4":
                            GetLicenseStatus(licenseManager);
                            break;

                        case "5":
                            DeactivateLicense(licenseManager);
                            break;

                        case "6":
                            return;

                        default:
                            Console.WriteLine("Invalid option");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void GetHardwareFingerprint(IWincajaLicenseManager licenseManager)
        {
            Console.WriteLine("\nGetting hardware fingerprint...");
            var result = licenseManager.GetHardwareFingerprint();
            var json = JObject.Parse(result);
            
            Console.WriteLine($"\nResult: {json.ToString(Formatting.Indented)}");
            
            if (json["success"]?.Value<bool>() == true)
            {
                Console.WriteLine($"\nFingerprint: {json["fingerprint"]}");
            }
        }

        static void ActivateLicense(IWincajaLicenseManager licenseManager)
        {
            Console.Write("\nEnter license key: ");
            var licenseKey = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                Console.WriteLine("License key cannot be empty");
                return;
            }

            Console.WriteLine("\nActivating license...");
            var result = licenseManager.ActivateLicense(licenseKey);
            var json = JObject.Parse(result);
            
            Console.WriteLine($"\nResult: {json.ToString(Formatting.Indented)}");
        }

        static void ValidateLicense(IWincajaLicenseManager licenseManager)
        {
            Console.WriteLine("\nValidating license...");
            var result = licenseManager.ValidateLicense();
            var json = JObject.Parse(result);
            
            Console.WriteLine($"\nResult: {json.ToString(Formatting.Indented)}");
        }

        static void GetLicenseStatus(IWincajaLicenseManager licenseManager)
        {
            Console.WriteLine("\nGetting license status...");
            var result = licenseManager.GetLicenseStatus();
            var json = JObject.Parse(result);
            
            Console.WriteLine($"\nResult: {json.ToString(Formatting.Indented)}");
        }

        static void DeactivateLicense(IWincajaLicenseManager licenseManager)
        {
            Console.Write("\nAre you sure you want to deactivate the license? (y/n): ");
            var confirm = Console.ReadLine();

            if (confirm?.ToLower() != "y")
            {
                Console.WriteLine("Deactivation cancelled");
                return;
            }

            Console.WriteLine("\nDeactivating license...");
            var result = licenseManager.DeactivateLicense();
            var json = JObject.Parse(result);
            
            Console.WriteLine($"\nResult: {json.ToString(Formatting.Indented)}");
        }
    }
}