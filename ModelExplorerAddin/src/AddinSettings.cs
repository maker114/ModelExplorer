using System;
using System.IO;

namespace ModelExplorerAddin
{
    public sealed class AddinSettings
    {
        public string BambuStudioPath { get; set; }
        public bool KeepHistory { get; set; }
        public bool BinaryStl { get; set; }
        public string StlUnits { get; set; }
        public string StlQuality { get; set; }

        public static string ConfigDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ModelExplorerAddin");
            }
        }

        public static string ConfigPath
        {
            get
            {
                return Path.Combine(ConfigDirectory, "ModelExplorerAddin.config");
            }
        }

        public static AddinSettings Load()
        {
            AddinSettings settings = new AddinSettings();
            settings.BambuStudioPath = DefaultBambuStudioPath();
            settings.KeepHistory = false;
            settings.BinaryStl = true;
            settings.StlUnits = "mm";
            settings.StlQuality = "Fine";

            if (File.Exists(ConfigPath))
            {
                string[] lines = File.ReadAllLines(ConfigPath);
                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                    {
                        continue;
                    }

                    int equalsIndex = line.IndexOf('=');
                    if (equalsIndex <= 0)
                    {
                        continue;
                    }

                    string key = line.Substring(0, equalsIndex).Trim();
                    string value = line.Substring(equalsIndex + 1).Trim();

                    switch (key.ToLowerInvariant())
                    {
                        case "bambustudiopath":
                            if (value.Length > 0)
                            {
                                settings.BambuStudioPath = value;
                            }
                            break;
                        case "keephistory":
                            settings.KeepHistory = ParseBool(value, settings.KeepHistory);
                            break;
                        case "binarystl":
                            settings.BinaryStl = ParseBool(value, settings.BinaryStl);
                            break;
                        case "stlunits":
                            if (value.Length > 0)
                            {
                                settings.StlUnits = value;
                            }
                            break;
                        case "stlquality":
                            if (value.Length > 0)
                            {
                                settings.StlQuality = value;
                            }
                            break;
                    }
                }
            }

            settings.Save();
            return settings;
        }

        public void Save()
        {
            Directory.CreateDirectory(ConfigDirectory);

            string[] lines = new string[]
            {
                "# ModelExplorerAddin settings",
                "# BambuStudioPath: full path to bambu-studio.exe",
                "BambuStudioPath=" + BambuStudioPath,
                "# KeepHistory: true = create timestamped copies; false = overwrite same-name STL",
                "KeepHistory=" + (KeepHistory ? "true" : "false"),
                "BinaryStl=" + (BinaryStl ? "true" : "false"),
                "StlUnits=" + StlUnits,
                "StlQuality=" + StlQuality
            };

            File.WriteAllLines(ConfigPath, lines);
        }

        public static string DefaultBambuStudioPath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] candidates = new string[]
            {
                @"E:\Bambu Studio\bambu-studio.exe",
                Path.Combine(localAppData, "Programs", "Bambu Studio", "bambu-studio.exe"),
                @"C:\Program Files\Bambu Studio\bambu-studio.exe",
                @"C:\Program Files (x86)\Bambu Studio\bambu-studio.exe"
            };

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return candidates[0];
        }

        private static bool ParseBool(string value, bool fallback)
        {
            bool result;
            if (bool.TryParse(value, out result))
            {
                return result;
            }
            return fallback;
        }
    }
}
