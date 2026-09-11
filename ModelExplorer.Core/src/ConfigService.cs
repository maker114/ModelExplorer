using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace ModelExplorer
{
    /// <summary>
    /// 配置读写。GUI / CLI / SolidWorks 插件共用同一份 config.json。
    /// </summary>
    public static class ConfigService
    {
        /// <summary>配置目录：%APPDATA%\ModelExplorer。</summary>
        public static string ConfigFolder
        {
            get
            {
                string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(baseDir, "ModelExplorer");
            }
        }

        public static string ConfigPath
        {
            get { return Path.Combine(ConfigFolder, "config.json"); }
        }

        /// <summary>v2.4.1 及更早版本由插件 / CLI 使用的旧配置文件。</summary>
        public static string LegacyConfigPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ModelExplorerAddin",
                    "ModelExplorerAddin.config");
            }
        }

        public static event Action<string> Log;

        public static AppConfig Load()
        {
            AppConfig config = AppConfig.CreateDefault();

            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    AppConfig loaded = serializer.Deserialize<AppConfig>(json);
                    if (loaded != null)
                    {
                        config.LastDir = loaded.LastDir ?? "";
                        config.Theme = string.IsNullOrEmpty(loaded.Theme) ? AppConfig.DefaultTheme : loaded.Theme;
                        config.FontSize = loaded.FontSize > 0 ? loaded.FontSize : AppConfig.DefaultFontSize;
                        config.OpenBambu = loaded.OpenBambu;
                        config.ProjectNameUnchecked = loaded.ProjectNameUnchecked ?? new List<string>();
                        config.OrganizeByFolder = loaded.OrganizeByFolder;
                        config.KeepHistory = loaded.KeepHistory;
                        // 保持可空：字段缺失（旧版配置）时为 null，由 UseBinaryStl 解释为“二进制”
                        config.BinaryStl = loaded.BinaryStl;
                        config.StlUnits = string.IsNullOrEmpty(loaded.StlUnits) ? AppConfig.DefaultStlUnits : loaded.StlUnits;
                        config.StlQuality = string.IsNullOrEmpty(loaded.StlQuality) ? AppConfig.DefaultStlQuality : loaded.StlQuality;
                        config.BambuPath = loaded.BambuPath ?? "";
                        config.SolidWorksPath = loaded.SolidWorksPath ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                EmitLog("读取配置失败，已使用默认配置：" + ex.Message);
            }

            if (MigrateLegacyConfig(config))
            {
                Save(config);
            }

            return config;
        }

        public static void Save(AppConfig config)
        {
            if (config == null)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(ConfigFolder);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                File.WriteAllText(ConfigPath, serializer.Serialize(config));
            }
            catch (Exception ex)
            {
                EmitLog("保存配置失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 把旧插件的 key=value 配置迁移进统一配置，随后把旧文件改名为 .migrated，
        /// 保证只迁移一次、且旧数据不丢失。
        /// </summary>
        private static bool MigrateLegacyConfig(AppConfig config)
        {
            string legacy = LegacyConfigPath;
            try
            {
                if (!File.Exists(legacy))
                {
                    return false;
                }

                foreach (string rawLine in File.ReadAllLines(legacy))
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

                    string key = line.Substring(0, equalsIndex).Trim().ToLowerInvariant();
                    string value = line.Substring(equalsIndex + 1).Trim();
                    if (value.Length == 0)
                    {
                        continue;
                    }

                    if (key == "bambustudiopath" && string.IsNullOrEmpty(config.BambuPath))
                    {
                        config.BambuPath = value;
                    }
                    else if (key == "keephistory" && !config.KeepHistory)
                    {
                        config.KeepHistory = ParseBool(value, config.KeepHistory);
                    }
                    else if (key == "binarystl")
                    {
                        config.BinaryStl = ParseBool(value, config.UseBinaryStl);
                    }
                    else if (key == "stlunits")
                    {
                        config.StlUnits = value;
                    }
                    else if (key == "stlquality")
                    {
                        config.StlQuality = value;
                    }
                }

                string migratedPath = legacy + ".migrated";
                if (File.Exists(migratedPath))
                {
                    File.Delete(migratedPath);
                }
                File.Move(legacy, migratedPath);
                EmitLog("已将旧插件配置迁移到 " + ConfigPath);
                return true;
            }
            catch (Exception ex)
            {
                EmitLog("迁移旧插件配置失败：" + ex.Message);
                return false;
            }
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

        private static void EmitLog(string message)
        {
            Action<string> handler = Log;
            if (handler != null)
            {
                handler(message);
            }
        }
    }
}
