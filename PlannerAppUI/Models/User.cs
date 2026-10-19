namespace PlannerApplication.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public User()
        {
        }

        public User(string name, string password)
        {
            this.Name = name;
            this.Password = password;
        }
    }
}