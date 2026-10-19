using PlannerApplication.Repositories;
using PlannerApplication.Models;
using PlannerApplication.Utility;

namespace PlannerApplication.Services
{
    public class AuthService
    {
        private readonly UserRepository userRepository;

        public AuthService(UserRepository? userRepository = null)
        {
            this.userRepository = userRepository ?? new UserRepository();
        }

        public Result AddUser(string username, string password)
        {
            if (!InputValidator.IsValidUsername(username, out string userErr))
            {
                return Result.Failure(userErr);
            }

            if (!InputValidator.IsValidPassword(password, out string passErr))
            {
                return Result.Failure(passErr);
            }

            if (userRepository.GetUsers().Any(p => string.Equals(p.Name, username.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return Result.Failure("Username already exists. Please choose another username.");
            }

            User user = new User(username.Trim(), password);

            userRepository.AddUser(user);
            return Result.Success();
        }

        public UserResult LoginUser(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return UserResult.Failure("Invalid username or password");
            }

            var user = userRepository.GetUsers().FirstOrDefault(p =>
                string.Equals(p.Name, username.Trim(), StringComparison.OrdinalIgnoreCase) && p.Password == password);
            if (user == null)
            {
                return UserResult.Failure("Invalid username or password");
            }
            return UserResult.Success(user.Id, user.Name);
        }
    }
}