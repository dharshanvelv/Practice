using System;
using System.IO;
using System.Text.Json;
using PlannerApplication.Models;

namespace PlannerApplication.Repositories
{
    public class AuthConfigJson
    {
        private readonly string _filepath;
        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public AuthConfigJson(string filepath = "auth_config.json")
        {
            _filepath = filepath;
        }

        public AuthConfig LoadConfig()
        {
            try
            {
                if (File.Exists(_filepath))
                {
                    string content = File.ReadAllText(_filepath);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var config = JsonSerializer.Deserialize<AuthConfig>(content, _options);
                        if (config != null)
                        {
                            if (config.MaxFailedAttempts <= 0) config.MaxFailedAttempts = 3;
                            if (config.LockoutDurationSeconds <= 0) config.LockoutDurationSeconds = 30;
                            return config;
                        }
                    }
                }
            }
            catch
            {
                // Silently fall back to default configuration
            }

            var defaultConfig = new AuthConfig();
            SaveConfig(defaultConfig);
            return defaultConfig;
        }

        public void SaveConfig(AuthConfig config)
        {
            if (config == null) return;
            try
            {
                string content = JsonSerializer.Serialize(config, _options);
                File.WriteAllText(_filepath, content);
            }
            catch
            {
                // Silently ignore save errors to avoid disrupting runtime
            }
        }
    }
}
