using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace WincajaLicenseManager.Core
{
    internal class HardwareFingerprinter
    {
        public class HardwareInfo
        {
            public CpuInfo Cpu { get; set; }
            public List<NetworkInfo> Network { get; set; }
            public List<DiskInfo> Disks { get; set; }
            public MotherboardInfo Motherboard { get; set; }
            public BiosInfo Bios { get; set; }
            public SystemInfo System { get; set; }
        }

        public class CpuInfo
        {
            public string Manufacturer { get; set; }
            public string Brand { get; set; }
            public uint Speed { get; set; }
            public uint Cores { get; set; }
            public uint PhysicalCores { get; set; }
            public uint Processors { get; set; }
        }

        public class NetworkInfo
        {
            public string MacAddress { get; set; }
            public string Description { get; set; }
        }

        public class DiskInfo
        {
            public string Name { get; set; }
            public string SerialNumber { get; set; }
            public ulong Size { get; set; }
        }

        public class MotherboardInfo
        {
            public string Manufacturer { get; set; }
            public string Model { get; set; }
            public string Serial { get; set; }
        }

        public class BiosInfo
        {
            public string Vendor { get; set; }
            public string Version { get; set; }
            public DateTime? ReleaseDate { get; set; }
        }

        public class SystemInfo
        {
            public string Uuid { get; set; }
            public string Manufacturer { get; set; }
            public string Model { get; set; }
        }

        public HardwareInfo GetHardwareInfo()
        {
            var info = new HardwareInfo
            {
                Cpu = GetCpuInfo(),
                Network = GetNetworkInfo(),
                Disks = GetDiskInfo(),
                Motherboard = GetMotherboardInfo(),
                Bios = GetBiosInfo(),
                System = GetSystemInfo()
            };

            return info;
        }

        public string GetHardwareFingerprint()
        {
            var hardwareInfo = GetHardwareInfo();
            var normalized = NormalizeHardwareInfo(hardwareInfo);
            return ComputeSha256Hash(normalized);
        }

        private CpuInfo GetCpuInfo()
        {
            var cpuInfo = new CpuInfo();

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                foreach (var obj in searcher.Get())
                {
                    cpuInfo.Manufacturer = obj["Manufacturer"]?.ToString() ?? "";
                    cpuInfo.Brand = obj["Name"]?.ToString() ?? "";
                    cpuInfo.Speed = Convert.ToUInt32(obj["MaxClockSpeed"] ?? 0);
                    cpuInfo.Cores = Convert.ToUInt32(obj["NumberOfCores"] ?? 0);
                    cpuInfo.PhysicalCores = Convert.ToUInt32(obj["NumberOfCores"] ?? 0);
                    cpuInfo.Processors = 1;
                    break;
                }
            }

            return cpuInfo;
        }

        private List<NetworkInfo> GetNetworkInfo()
        {
            var networkList = new List<NetworkInfo>();

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True"))
            {
                foreach (var obj in searcher.Get())
                {
                    var macAddress = obj["MACAddress"]?.ToString();
                    if (!string.IsNullOrEmpty(macAddress))
                    {
                        networkList.Add(new NetworkInfo
                        {
                            MacAddress = macAddress,
                            Description = obj["Description"]?.ToString() ?? ""
                        });
                    }
                }
            }

            return networkList;
        }

        private List<DiskInfo> GetDiskInfo()
        {
            var diskList = new List<DiskInfo>();

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE MediaType = 'Fixed hard disk media'"))
            {
                foreach (var obj in searcher.Get())
                {
                    diskList.Add(new DiskInfo
                    {
                        Name = obj["Model"]?.ToString() ?? "",
                        SerialNumber = obj["SerialNumber"]?.ToString()?.Trim() ?? "",
                        Size = Convert.ToUInt64(obj["Size"] ?? 0)
                    });
                }
            }

            return diskList.OrderBy(d => d.Name).ToList();
        }

        private MotherboardInfo GetMotherboardInfo()
        {
            var mbInfo = new MotherboardInfo();

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard"))
            {
                foreach (var obj in searcher.Get())
                {
                    mbInfo.Manufacturer = obj["Manufacturer"]?.ToString() ?? "";
                    mbInfo.Model = obj["Product"]?.ToString() ?? "";
                    mbInfo.Serial = obj["SerialNumber"]?.ToString() ?? "";
                    break;
                }
            }

            return mbInfo;
        }

        private BiosInfo GetBiosInfo()
        {
            var biosInfo = new BiosInfo();

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS"))
            {
                foreach (var obj in searcher.Get())
                {
                    biosInfo.Vendor = obj["Manufacturer"]?.ToString() ?? "";
                    biosInfo.Version = obj["Version"]?.ToString() ?? "";
                    
                    var releaseDateStr = obj["ReleaseDate"]?.ToString();
                    if (!string.IsNullOrEmpty(releaseDateStr) && releaseDateStr.Length >= 8)
                    {
                        try
                        {
                            var year = int.Parse(releaseDateStr.Substring(0, 4));
                            var month = int.Parse(releaseDateStr.Substring(4, 2));
                            var day = int.Parse(releaseDateStr.Substring(6, 2));
                            biosInfo.ReleaseDate = new DateTime(year, month, day);
                        }
                        catch
                        {
                            biosInfo.ReleaseDate = null;
                        }
                    }
                    break;
                }
            }

            return biosInfo;
        }

        private SystemInfo GetSystemInfo()
        {
            var sysInfo = new SystemInfo();

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystemProduct"))
            {
                foreach (var obj in searcher.Get())
                {
                    sysInfo.Uuid = obj["UUID"]?.ToString() ?? "";
                    sysInfo.Manufacturer = obj["Vendor"]?.ToString() ?? "";
                    sysInfo.Model = obj["Name"]?.ToString() ?? "";
                    break;
                }
            }

            return sysInfo;
        }

        private string NormalizeHardwareInfo(HardwareInfo info)
        {
            var sb = new StringBuilder();

            // CPU
            if (info.Cpu != null)
            {
                sb.AppendLine($"CPU:{info.Cpu.Manufacturer}|{info.Cpu.Brand}|{info.Cpu.Speed}|{info.Cpu.Cores}");
            }

            // Network - sort by MAC address for consistency
            if (info.Network != null)
            {
                var sortedMacs = info.Network
                    .Where(n => !string.IsNullOrEmpty(n.MacAddress))
                    .Select(n => n.MacAddress.ToUpper())
                    .OrderBy(m => m)
                    .ToList();
                
                foreach (var mac in sortedMacs)
                {
                    sb.AppendLine($"NET:{mac}");
                }
            }

            // Disks - already sorted by name
            if (info.Disks != null)
            {
                foreach (var disk in info.Disks.Where(d => !string.IsNullOrEmpty(d.SerialNumber)))
                {
                    sb.AppendLine($"DISK:{disk.Name}|{disk.SerialNumber}|{disk.Size}");
                }
            }

            // Motherboard
            if (info.Motherboard != null)
            {
                sb.AppendLine($"MB:{info.Motherboard.Manufacturer}|{info.Motherboard.Model}|{info.Motherboard.Serial}");
            }

            // BIOS
            if (info.Bios != null)
            {
                var releaseDate = info.Bios.ReleaseDate?.ToString("yyyy-MM-dd") ?? "";
                sb.AppendLine($"BIOS:{info.Bios.Vendor}|{info.Bios.Version}|{releaseDate}");
            }

            // System
            if (info.System != null)
            {
                sb.AppendLine($"SYS:{info.System.Uuid}|{info.System.Manufacturer}|{info.System.Model}");
            }

            return sb.ToString();
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                
                var builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public Dictionary<string, object> GetSimplifiedHardwareInfo()
        {
            var info = GetHardwareInfo();
            var result = new Dictionary<string, object>();

            // CPU
            if (info.Cpu != null)
            {
                result["cpu"] = new Dictionary<string, object>
                {
                    ["manufacturer"] = info.Cpu.Manufacturer,
                    ["brand"] = info.Cpu.Brand,
                    ["speed"] = info.Cpu.Speed,
                    ["cores"] = info.Cpu.Cores,
                    ["physicalCores"] = info.Cpu.PhysicalCores,
                    ["processors"] = info.Cpu.Processors
                };
            }

            // Network
            if (info.Network != null && info.Network.Any())
            {
                result["network"] = info.Network.Select(n => new Dictionary<string, object>
                {
                    ["iface"] = n.Description,
                    ["mac"] = n.MacAddress
                }).ToList();
            }

            // Disks
            if (info.Disks != null && info.Disks.Any())
            {
                result["disks"] = info.Disks.Select(d => new Dictionary<string, object>
                {
                    ["name"] = d.Name,
                    ["serial"] = d.SerialNumber,
                    ["size"] = d.Size
                }).ToList();
            }

            // Motherboard
            if (info.Motherboard != null)
            {
                result["baseboard"] = new Dictionary<string, object>
                {
                    ["manufacturer"] = info.Motherboard.Manufacturer,
                    ["model"] = info.Motherboard.Model,
                    ["serial"] = info.Motherboard.Serial
                };
            }

            // BIOS
            if (info.Bios != null)
            {
                result["bios"] = new Dictionary<string, object>
                {
                    ["vendor"] = info.Bios.Vendor,
                    ["version"] = info.Bios.Version,
                    ["releaseDate"] = info.Bios.ReleaseDate?.ToString("yyyy-MM-dd")
                };
            }

            // System
            if (info.System != null)
            {
                result["system"] = new Dictionary<string, object>
                {
                    ["uuid"] = info.System.Uuid,
                    ["manufacturer"] = info.System.Manufacturer,
                    ["model"] = info.System.Model
                };
            }

            return result;
        }
    }
}