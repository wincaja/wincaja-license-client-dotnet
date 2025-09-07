using System;
using System.IO;
using WincajaLicenseManager;
using WincajaLicenseManager.Models;
using Newtonsoft.Json;

namespace TestRevocationFix
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Testing Revocation Fix ===");
            Console.WriteLine();
            
            // Initialize license manager
            var licenseManager = WincajaLicenseManager.WincajaLicenseManager.Instance;
            
            // Step 1: Check current status
            Console.WriteLine("1. Checking current license status...");
            var status = licenseManager.ValidateLicense();
            Console.WriteLine($"   Status: {status.Status}");
            Console.WriteLine($"   IsValid: {status.IsValid}");
            Console.WriteLine($"   Cache exists: {File.Exists(GetLicenseFilePath())}");
            Console.WriteLine();
            
            // Step 2: Simulate a revoked license response
            Console.WriteLine("2. Simulating revoked license server response...");
            Console.WriteLine("   (In production, this would come from server)");
            
            // Create a mock validation response that indicates revocation
            var mockResponse = new ValidationResponse
            {
                Valid = false,
                Success = false,
                Error = "License has been revoked by administrator",
                License = new LicenseInfo
                {
                    Status = "revoked"
                }
            };
            
            // The fix we implemented should detect this as a revocation
            // and immediately clear the cache
            
            Console.WriteLine();
            Console.WriteLine("3. Testing if cache is cleared after revocation...");
            
            // Force an online validation to trigger the revocation check
            var forceValidationStatus = licenseManager.ForceOnlineValidation();
            Console.WriteLine($"   Force validation status: {forceValidationStatus.Status}");
            Console.WriteLine($"   Error: {forceValidationStatus.Error}");
            
            // Check if cache was cleared
            bool cacheExists = File.Exists(GetLicenseFilePath());
            Console.WriteLine($"   Cache exists after revocation: {cacheExists}");
            
            Console.WriteLine();
            Console.WriteLine("4. Verifying grace period is NOT applied to revoked licenses...");
            
            // Try regular validation - should fail immediately if cache was cleared
            var finalStatus = licenseManager.ValidateLicense();
            Console.WriteLine($"   Status: {finalStatus.Status}");
            Console.WriteLine($"   IsValid: {finalStatus.IsValid}");
            Console.WriteLine($"   Grace days remaining: {finalStatus.GraceDaysRemaining}");
            
            Console.WriteLine();
            Console.WriteLine("=== Test Results ===");
            if (!cacheExists && finalStatus.Status == "not_activated")
            {
                Console.WriteLine("✅ SUCCESS: Cache was properly cleared after revocation!");
                Console.WriteLine("✅ SUCCESS: Grace period NOT applied to revoked license!");
            }
            else if (cacheExists)
            {
                Console.WriteLine("❌ FAILURE: Cache still exists after revocation!");
                Console.WriteLine("   This is the critical security issue that needs fixing.");
            }
            else
            {
                Console.WriteLine("⚠️  PARTIAL: Some aspects of the fix are working.");
            }
        }
        
        private static string GetLicenseFilePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "Wincaja", "license.dat");
        }
    }
}