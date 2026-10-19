namespace PlannerApplication.Models
{
    public class Project
    {
        public Guid ProjectId { get; set; } = Guid.NewGuid();

        public Guid Id
        {
            get => ProjectId;
            set => ProjectId = value;
        }

        public Project()
        {

        }
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectDescription { get; set; } = string.Empty;
        public DateOnly CreatedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public List<Guid> UserIds { get; set; } = new List<Guid>();

        public Project(string name)
        {
            this.ProjectName = name;
        }

        public Project(string name, string description = "", List<Guid>? userIds = null)
        {
            ProjectId = Guid.NewGuid();
            ProjectName = name;
            ProjectDescription = description;
            CreatedDate = DateOnly.FromDateTime(DateTime.Today);
            if (userIds != null)
            {
                UserIds = userIds;
            }
        }
    }
}
