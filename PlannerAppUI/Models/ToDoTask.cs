namespace PlannerApplication.Models
{
    public class ToDoTask
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProjectId { get; set; } = Guid.Empty;
        public Guid? UserId { get; set; } = null;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TaskState Status { get; set; } = TaskState.NotStarted;
        public DateOnly TargetStartDate { get; set; }
        public DateOnly TargetEndDate { get; set; }

        public ToDoTask()
        {
        }

        public ToDoTask(string name, string desc, DateOnly startdate, DateOnly enddate, Guid projectId, Guid? userId = null)
        {
            this.Name = name;
            this.Description = desc;
            this.TargetStartDate = startdate;
            this.TargetEndDate = enddate;
            this.ProjectId = projectId;
            this.UserId = userId;
        }


        public ToDoTask(string name)
        {
            this.Name = name;
        }
    }
}