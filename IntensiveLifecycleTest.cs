using System;
using System.Threading;
using Newtonsoft.Json.Linq;
using WincajaLicenseManager;

class IntensiveLifecycleTest
{
    static IWincajaLicenseManager licenseManager;
    static string testLicenseKey = "Y77X-M297-IJGQ-R3NM-O2IJ";
    static int testNumber = 0;
    
    static void Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("INTENSIVE LICENSE LIFECYCLE TESTING");
        Console.WriteLine("========================================");
        Console.WriteLine($"Test License: {testLicenseKey}");
        Console.WriteLine($"Test Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine("========================================\n");

        try
        {
            // Initialize license manager
            licenseManager = new WincajaLicenseManagerImpl();
            
            // Run all tests
            RunTest("Hardware Fingerprint Check", TestHardwareFingerprint);
            RunTest("Fresh License Activation", TestFreshActivation);
            RunTest("Online Validation After Activation", TestOnlineValidation);
            RunTest("License Status Check", TestLicenseStatus);
            RunTest("Force Online Validation", TestForceOnlineValidation);
            RunTest("Deactivate License", TestDeactivation);
            RunTest("Attempt Validation After Deactivation", TestValidationAfterDeactivation);
            RunTest("Reactivate After Deactivation", TestReactivation);
            RunTest("Test Suspended License (L8S6-BPR)", TestSuspendedLicense);
            RunTest("Test Revoked License (T28K-7PN)", TestRevokedLicense);
            
            Console.WriteLine("\n========================================");
            Console.WriteLine("ALL TESTS COMPLETED");
            Console.WriteLine($"Test Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("========================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nFATAL ERROR: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static void RunTest(string testName, Func<bool> testMethod)
    {
        testNumber++;
        Console.WriteLine($"\n--- TEST {testNumber}: {testName} ---");
        Console.WriteLine($"Started: {DateTime.Now:HH:mm:ss}");
        
        try
        {
            bool success = testMethod();
            
            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ TEST PASSED");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ TEST FAILED");
            }
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ TEST EXCEPTION: {ex.Message}");
            Console.ResetColor();
        }
        
        Console.WriteLine($"Completed: {DateTime.Now:HH:mm:ss}");
        Thread.Sleep(1000); // Brief pause between tests
    }

    static bool TestHardwareFingerprint()
    {
        Console.WriteLine("Getting hardware fingerprint...");
        var result = licenseManager.GetHardwareFingerprint();
        var json = JObject.Parse(result);
        
        if (json["success"]?.Value<bool>() == true)
        {
            Console.WriteLine($"Fingerprint: {json["fingerprint"]}");
            
            if (json["hardware"] != null)
            {
                var hw = json["hardware"];
                Console.WriteLine("Hardware Components:");
                Console.WriteLine($"  - CPU: {hw["cpuId"]}");
                Console.WriteLine($"  - Motherboard: {hw["motherboardSerial"]}");
                Console.WriteLine($"  - System UUID: {hw["systemUuid"]}");
            }
            return true;
        }
        
        Console.WriteLine($"Error: {json["error"]}");
        return false;
    }

    static bool TestFreshActivation()
    {
        Console.WriteLine($"Activating license: {testLicenseKey}");
        var result = licenseManager.ActivateLicense(testLicenseKey);
        var json = JObject.Parse(result);
        
        if (json["success"]?.Value<bool>() == true)
        {
            Console.WriteLine($"Activation ID: {json["activationId"]}");
            Console.WriteLine($"Hardware Fingerprint: {json["hardwareFingerprint"]}");
            Console.WriteLine($"Remaining Activations: {json["remainingActivations"]}");
            return true;
        }
        
        Console.WriteLine($"Error: {json["error"]}");
        return false;
    }

    static bool TestOnlineValidation()
    {
        Console.WriteLine("Performing online validation...");
        var result = licenseManager.ValidateLicense();
        var json = JObject.Parse(result);
        
        if (json["success"]?.Value<bool>() == true)
        {
            Console.WriteLine($"Valid: {json["valid"]}");
            Console.WriteLine($"Status: {json["status"]}");
            
            if (json["license"] != null)
            {
                var license = json["license"];
                Console.WriteLine($"License Type: {license["licenseType"]}");
                Console.WriteLine($"Expires: {license["expiresAt"]}");
            }
            return true;
        }
        
        Console.WriteLine($"Error: {json["error"]}");
        return false;
    }

    static bool TestLicenseStatus()
    {
        Console.WriteLine("Getting license status...");
        var result = licenseManager.GetLicenseStatus();
        var json = JObject.Parse(result);
        
        Console.WriteLine($"Success: {json["success"]}");
        Console.WriteLine($"Status: {json["status"]}");
        Console.WriteLine($"License Key: {json["licenseKey"]}");
        
        if (json["expiresAt"] != null)
        {
            Console.WriteLine($"Expires: {json["expiresAt"]}");
            Console.WriteLine($"Days Until Expiration: {json["daysUntilExpiration"]}");
        }
        
        return json["success"]?.Value<bool>() == true;
    }

    static bool TestForceOnlineValidation()
    {
        Console.WriteLine("Forcing online validation...");
        var result = licenseManager.ValidateLicenseForceOnline();
        var json = JObject.Parse(result);
        
        Console.WriteLine($"Valid: {json["valid"]}");
        Console.WriteLine($"Success: {json["success"]}");
        
        if (json["validation"] != null)
        {
            var validation = json["validation"];
            Console.WriteLine($"Hardware Valid: {validation["hardwareValid"]}");
            Console.WriteLine($"Current Activations: {validation["currentActivations"]}");
            Console.WriteLine($"Activation Limit: {validation["activationLimit"]}");
        }
        
        return json["success"]?.Value<bool>() == true;
    }

    static bool TestDeactivation()
    {
        Console.WriteLine("Deactivating license...");
        var result = licenseManager.DeactivateLicense();
        var json = JObject.Parse(result);
        
        Console.WriteLine($"Success: {json["success"]}");
        
        if (json["success"]?.Value<bool>() == true)
        {
            Console.WriteLine($"Message: {json["message"]}");
            Console.WriteLine($"Deactivation Type: {json["deactivationType"]}");
            
            if (json["remainingActivations"] != null)
            {
                Console.WriteLine($"Remaining Activations: {json["remainingActivations"]}");
            }
            return true;
        }
        
        Console.WriteLine($"Error: {json["error"]}");
        return false;
    }

    static bool TestValidationAfterDeactivation()
    {
        Console.WriteLine("Attempting validation after deactivation...");
        var result = licenseManager.ValidateLicense();
        var json = JObject.Parse(result);
        
        Console.WriteLine($"Success: {json["success"]}");
        Console.WriteLine($"Valid: {json["valid"]}");
        
        // This should fail, so we expect success=false
        return json["success"]?.Value<bool>() == false;
    }

    static bool TestReactivation()
    {
        Console.WriteLine($"Reactivating license: {testLicenseKey}");
        var result = licenseManager.ActivateLicense(testLicenseKey);
        var json = JObject.Parse(result);
        
        if (json["success"]?.Value<bool>() == true)
        {
            Console.WriteLine($"Reactivation successful!");
            Console.WriteLine($"Activation ID: {json["activationId"]}");
            Console.WriteLine($"Remaining Activations: {json["remainingActivations"]}");
            return true;
        }
        
        Console.WriteLine($"Error: {json["error"]}");
        return false;
    }

    static bool TestSuspendedLicense()
    {
        Console.WriteLine("Testing suspended license (L8S6-BPR-MJ7F-FY6W-P1CY)...");
        
        // First deactivate current license
        licenseManager.DeactivateLicense();
        
        // Try to activate suspended license
        var result = licenseManager.ActivateLicense("L8S6-BPR-MJ7F-FY6W-P1CY");
        var json = JObject.Parse(result);
        
        Console.WriteLine($"Success: {json["success"]}");
        Console.WriteLine($"Error: {json["error"]}");
        
        // We expect this to fail because the license is suspended
        bool testPassed = json["success"]?.Value<bool>() == false && 
                         json["error"]?.ToString().ToLower().Contains("suspended") == true;
        
        if (testPassed)
        {
            Console.WriteLine("Correctly rejected suspended license");
        }
        
        return testPassed;
    }

    static bool TestRevokedLicense()
    {
        Console.WriteLine("Testing revoked license (T28K-7PNT-OK0H-0C5U-IH7T)...");
        
        // Try to activate revoked license
        var result = licenseManager.ActivateLicense("T28K-7PNT-OK0H-0C5U-IH7T");
        var json = JObject.Parse(result);
        
        Console.WriteLine($"Success: {json["success"]}");
        Console.WriteLine($"Error: {json["error"]}");
        
        // We expect this to fail because the license is revoked
        bool testPassed = json["success"]?.Value<bool>() == false && 
                         json["error"]?.ToString().ToLower().Contains("revoked") == true;
        
        if (testPassed)
        {
            Console.WriteLine("Correctly rejected revoked license");
        }
        
        return testPassed;
    }
}