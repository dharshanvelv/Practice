namespace PlannerApplication.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string RecoveryCodeHash { get; set; } = string.Empty;

        public User()
        {
        }

        public User(string name, string password, string recoveryCodeHash = "")
        {
            this.Name = name;
            this.Password = password;
            this.RecoveryCodeHash = recoveryCodeHash;
        }
    }
}