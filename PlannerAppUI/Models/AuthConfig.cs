namespace PlannerApplication.Models
{
    public class AuthConfig
    {
        public int MaxFailedAttempts { get; set; } = 3;
        public int LockoutDurationSeconds { get; set; } = 30;

        public AuthConfig()
        {
        }

        public AuthConfig(int maxFailedAttempts, int lockoutDurationSeconds)
        {
            MaxFailedAttempts = maxFailedAttempts;
            LockoutDurationSeconds = lockoutDurationSeconds;
        }
    }
}
