using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AspnetCoreStarter.Helpers
{
    public class MaintenanceSettings
    {
        public int Year { get; set; }
        public DateTime? ScheduledDate { get; set; }
    }

    public static class MaintenanceSettingsHelper
    {
        private static readonly string FilePath = Path.Combine(Directory.GetCurrentDirectory(), "maintenance_settings.json");

        public static async Task<MaintenanceSettings> GetSettingsAsync(int year)
        {
            if (!File.Exists(FilePath))
            {
                return new MaintenanceSettings { Year = year };
            }

            try
            {
                var json = await File.ReadAllTextAsync(FilePath);
                var settingsList = JsonSerializer.Deserialize<System.Collections.Generic.List<MaintenanceSettings>>(json);
                if (settingsList != null)
                {
                    var settings = settingsList.Find(s => s.Year == year);
                    if (settings != null) return settings;
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return new MaintenanceSettings { Year = year };
        }

        public static async Task SaveSettingsAsync(int year, DateTime date)
        {
            var settingsList = new System.Collections.Generic.List<MaintenanceSettings>();
            if (File.Exists(FilePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(FilePath);
                    var existing = JsonSerializer.Deserialize<System.Collections.Generic.List<MaintenanceSettings>>(json);
                    if (existing != null) settingsList = existing;
                }
                catch { }
            }

            var existingSetting = settingsList.Find(s => s.Year == year);
            if (existingSetting != null)
            {
                existingSetting.ScheduledDate = date;
            }
            else
            {
                settingsList.Add(new MaintenanceSettings { Year = year, ScheduledDate = date });
            }

            var newJson = JsonSerializer.Serialize(settingsList, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(FilePath, newJson);
        }
    }
}
