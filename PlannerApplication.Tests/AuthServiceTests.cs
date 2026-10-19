using System;
using System.IO;
using PlannerApplication.Models;
using PlannerApplication.Repositories;
using PlannerApplication.Services;
using Xunit;

namespace PlannerApplication.Tests
{
    public class AuthServiceTests : IDisposable
    {
        private readonly string _testFile;
        private readonly UserRepository _userRepo;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _testFile = $"test_users_{Guid.NewGuid():N}.json";
            _userRepo = new UserRepository(new UserJson(_testFile));
            _authService = new AuthService(_userRepo);
        }

        public void Dispose()
        {
            if (File.Exists(_testFile))
            {
                File.Delete(_testFile);
            }
        }

        [Fact]
        public void AddUser_ValidInput_ReturnsSuccess()
        {
            var result = _authService.AddUser("alice", "secret123");
            Assert.True(result.IsSuccess);
            Assert.Empty(result.ErrorMessage);

            var users = _userRepo.GetUsers();
            Assert.Single(users);
            Assert.Equal("alice", users[0].Name);
        }

        [Theory]
        [InlineData("", "password")]
        [InlineData("   ", "password")]
        [InlineData("ab", "password")] // Less than 3 chars
        public void AddUser_InvalidUsername_ReturnsFailure(string username, string password)
        {
            var result = _authService.AddUser(username, password);
            Assert.False(result.IsSuccess);
            Assert.NotEmpty(result.ErrorMessage);
        }

        [Theory]
        [InlineData("bob", "")]
        [InlineData("bob", "12")] // Less than 4 chars
        public void AddUser_InvalidPassword_ReturnsFailure(string username, string password)
        {
            var result = _authService.AddUser(username, password);
            Assert.False(result.IsSuccess);
            Assert.NotEmpty(result.ErrorMessage);
        }

        [Fact]
        public void AddUser_DuplicateUsername_CaseInsensitive_ReturnsFailure()
        {
            _authService.AddUser("charlie", "pass123");
            var result = _authService.AddUser("CHARLIE", "pass456");

            Assert.False(result.IsSuccess);
            Assert.Contains("already exists", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void LoginUser_ValidCredentials_ReturnsSuccessWithUserId()
        {
            _authService.AddUser("david", "mypassword");

            var result = _authService.LoginUser("david", "mypassword");
            Assert.True(result.IsSuccess);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal("david", result.UserName);
        }

        [Fact]
        public void LoginUser_WrongPassword_ReturnsFailure()
        {
            _authService.AddUser("emma", "correctpass");

            var result = _authService.LoginUser("emma", "wrongpass");
            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid username or password", result.ErrorMessage);
        }

        [Fact]
        public void LoginUser_NonExistentUser_ReturnsFailure()
        {
            var result = _authService.LoginUser("ghost", "anypassword");
            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid username or password", result.ErrorMessage);
        }
    }
}
