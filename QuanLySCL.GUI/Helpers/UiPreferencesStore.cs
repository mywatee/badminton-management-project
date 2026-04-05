using System;
using System.IO;
using System.Text.Json;

namespace QuanLySCL.GUI.Helpers
{
    public sealed class UiPreferences
    {
        public int ScheduleViewMinutes { get; set; } = 60; // 30 or 60
    }

    public static class UiPreferencesStore
    {
        private static string GetPath()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuanLySCL");
            return Path.Combine(dir, "ui_prefs.json");
        }

        public static UiPreferences? Load()
        {
            string path = GetPath();
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return null;

            return JsonSerializer.Deserialize<UiPreferences>(json);
        }

        public static void Save(UiPreferences prefs)
        {
            if (prefs == null) return;

            string path = GetPath();
            string dir = Path.GetDirectoryName(path) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(prefs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}

