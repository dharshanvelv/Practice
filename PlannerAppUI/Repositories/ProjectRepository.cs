using PlannerApplication.Models;

namespace PlannerApplication.Repositories
{
    public class ProjectRepository
    {
        private List<Project> projects = new();
        private ProjectJson projectjson;

        public ProjectRepository(ProjectJson? projectJson = null)
        {
            this.projectjson = projectJson ?? new ProjectJson();
            projects = projectjson.GetProjects();
        }

        public void AddProject(Project project)
        {
            if (project.ProjectId == Guid.Empty) project.ProjectId = Guid.NewGuid();
            projects = projectjson.GetProjects();
            projects.Add(project);
            projectjson.SaveProjects(projects);
        }

        public List<Project> GetProjects()
        {
            projects = projectjson.GetProjects();
            return projects;
        }

        public bool UpdateProject(Project project)
        {
            projects = projectjson.GetProjects();
            Project? existingProject = projects.FirstOrDefault(t => t.ProjectId == project.ProjectId || t.Id == project.ProjectId);
            if (existingProject == null) return false;

            existingProject.ProjectName = project.ProjectName;
            existingProject.ProjectDescription = project.ProjectDescription;
            existingProject.UserIds = project.UserIds ?? new List<Guid>();
            projectjson.SaveProjects(projects);
            return true;
        }

        public bool DeleteProject(Guid id)
        {
            projects = projectjson.GetProjects();
            Project? project = projects.FirstOrDefault(t => t.ProjectId == id || t.Id == id);
            if (project == null) return false;

            projects.Remove(project);
            projectjson.SaveProjects(projects);
            return true;
        }
    }
}