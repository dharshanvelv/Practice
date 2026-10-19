using System.Text.Json;
using PlannerApplication.Models;

namespace PlannerApplication.Repositories
{
    public class ProjectJson
    {
        private readonly string _filepath;
        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new DateOnlyJsonConverter() }
        };

        public ProjectJson(string filepath = "projects.json")
        {
            _filepath = filepath;
        }

        public void SaveProjects(List<Project> projects)
        {
            if (projects == null) return;

            try
            {
                string content = JsonSerializer.Serialize(projects, _options);
                File.WriteAllText(_filepath, content);
            }
            catch
            {
                // Silently handle IO exceptions during save to prevent crashing
            }
        }

        public List<Project> GetProjects()
        {
            try
            {
                if (!File.Exists(_filepath))
                {
                    return new List<Project>();
                }

                string content = File.ReadAllText(_filepath);
                if (string.IsNullOrWhiteSpace(content))
                {
                    return new List<Project>();
                }

                return JsonSerializer.Deserialize<List<Project>>(content, _options) ?? new List<Project>();
            }
            catch
            {
                return new List<Project>();
            }
        }
    }
}