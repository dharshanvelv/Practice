using System.Text.Json;
using PlannerApplication.Models;

namespace PlannerApplication.Repositories
{
    public class UserJson
    {
        private readonly string _filepath;
        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public UserJson(string filepath = "users.json")
        {
            _filepath = filepath;
        }

        public void SaveUsers(List<User> users)
        {
            if (users == null) return;

            try
            {
                string content = JsonSerializer.Serialize(users, _options);
                File.WriteAllText(_filepath, content);
            }
            catch
            {
                // Silently handle IO exceptions during save to prevent crashing
            }
        }

        public List<User> GetUsers()
        {
            try
            {
                if (!File.Exists(_filepath))
                {
                    return new List<User>();
                }

                string content = File.ReadAllText(_filepath);
                if (string.IsNullOrWhiteSpace(content))
                {
                    return new List<User>();
                }

                return JsonSerializer.Deserialize<List<User>>(content, _options) ?? new List<User>();
            }
            catch
            {
                return new List<User>();
            }
        }
    }
}