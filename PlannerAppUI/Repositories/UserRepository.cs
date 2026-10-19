using PlannerApplication.Models;

namespace PlannerApplication.Repositories
{
    public class UserRepository
    {
        private List<User> users = new();
        private UserJson userjson;

        public UserRepository(UserJson? userJson = null)
        {
            this.userjson = userJson ?? new UserJson();
            users = userjson.GetUsers();
        }

        public void AddUser(User user)
        {
            users = userjson.GetUsers();
            users.Add(user);
            userjson.SaveUsers(users);
        }

        public List<User> GetUsers()
        {
            users = userjson.GetUsers();
            return users;
        }

        public void UpdateUser(User updatedUser)
        {
            if (updatedUser == null) return;
            users = userjson.GetUsers();
            int idx = users.FindIndex(u => u.Id == updatedUser.Id);
            if (idx >= 0)
            {
                users[idx] = updatedUser;
                userjson.SaveUsers(users);
            }
        }
    }
}