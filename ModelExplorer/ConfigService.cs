using System;
using System.IO;
using System.Web.Script.Serialization;

namespace ModelExplorer
{
    public class AppConfig
    {
        public string LastDir { get; set; }
        public bool KeepHistory { get; set; }
        public bool OpenBambu { get; set; }
        public string BambuPath { get; set; }
        public string SolidWorksPath { get; set; }
        public string Theme { get; set; }
        public int FontSize { get; set; }
    }

    public static class ConfigService
    {
        private static string ConfigFolder
        {
            get
            {
                string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(baseDir, "ModelExplorer");
            }
        }

        private static string ConfigFile
        {
            get { return Path.Combine(ConfigFolder, "config.json"); }
        }

        public static AppConfig Load()
        {
            AppConfig config = new AppConfig();
            config.LastDir = "";
            config.KeepHistory = false;
            config.OpenBambu = true;
            config.BambuPath = "";
            config.SolidWorksPath = "";
            config.Theme = "终末地配色";
            config.FontSize = 12;

            try
            {
                if (File.Exists(ConfigFile))
                {
                    string json = File.ReadAllText(ConfigFile);
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    AppConfig loaded = serializer.Deserialize<AppConfig>(json);
                    if (loaded != null)
                    {
                        config.LastDir = loaded.LastDir ?? "";
                        config.KeepHistory = loaded.KeepHistory;
                        config.OpenBambu = loaded.OpenBambu;
                        config.BambuPath = loaded.BambuPath ?? "";
                        config.SolidWorksPath = loaded.SolidWorksPath ?? "";
                        config.Theme = loaded.Theme ?? "终末地配色";
                        config.FontSize = loaded.FontSize > 0 ? loaded.FontSize : 12;
                    }
                }
            }
            catch
            {
            }

            return config;
        }

        public static void Save(AppConfig config)
        {
            try
            {
                Directory.CreateDirectory(ConfigFolder);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                File.WriteAllText(ConfigFile, serializer.Serialize(config));
            }
            catch
            {
            }
        }
    }
}
