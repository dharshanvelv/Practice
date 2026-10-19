using PlannerApplication.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlannerApplication.Repositories
{
    public class TaskJson
    {
        private readonly string _filepath;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new DateOnlyJsonConverter(), new LegacyTaskIdJsonConverter() },
        };

        public TaskJson(string filepath = "tasks.json")
        {
            _filepath = filepath;
        }

        public void SaveTasks(List<ToDoTask> tasks)
        {
            if (tasks == null) return;

            try
            {
                string content = JsonSerializer.Serialize(tasks, _jsonOptions);
                File.WriteAllText(_filepath, content);
            }
            catch
            {
                // Silently handle IO exceptions during save to prevent crashing
            }
        }

        public List<ToDoTask> GetTasks()
        {
            try
            {
                if (!File.Exists(_filepath))
                {
                    return new List<ToDoTask>();
                }

                string content = File.ReadAllText(_filepath);
                if (string.IsNullOrWhiteSpace(content))
                {
                    return new List<ToDoTask>();
                }

                return JsonSerializer.Deserialize<List<ToDoTask>>(content, _jsonOptions) ?? new List<ToDoTask>();
            }
            catch
            {
                return new List<ToDoTask>();
            }
        }
    }

    public class LegacyTaskIdJsonConverter : JsonConverter<Guid>
    {
        public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String && Guid.TryParse(reader.GetString(), out Guid id))
                return id;
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int legacyId))
            {
                byte[] bytes = new byte[16];
                BitConverter.GetBytes(legacyId).CopyTo(bytes, 0);
                return new Guid(bytes);
            }
            return Guid.Empty;
        }

        public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}