using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using PlannerApplication.Models;
using PlannerApplication.Repositories;
using PlannerApplication.Services;
using PlannerApplication.Utility;
using PlannerApplication.View;
using Xunit;

namespace PlannerApplication.Tests
{
    public class AuthSecurityTests : IDisposable
    {
        private readonly string _userFile;
        private readonly string _configFile;
        private readonly UserRepository _userRepo;
        private readonly AuthConfigJson _configJson;

        public AuthSecurityTests()
        {
            string id = Guid.NewGuid().ToString("N");
            _userFile = $"test_auth_u_{id}.json";
            _configFile = $"test_auth_cfg_{id}.json";

            _userRepo = new UserRepository(new UserJson(_userFile));
            _configJson = new AuthConfigJson(_configFile);
        }

        public void Dispose()
        {
            if (File.Exists(_userFile)) File.Delete(_userFile);
            if (File.Exists(_configFile)) File.Delete(_configFile);
        }

        [Fact]
        public void Registration_GeneratesValid6DigitRecoveryCode_AndHashesSensitiveData()
        {
            var authService = new AuthService(_userRepo);
            var result = authService.AddUser("newuser", "securePass123");

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.RecoveryCode);
            Assert.Equal(6, result.RecoveryCode.Length);
            Assert.True(result.RecoveryCode.All(char.IsDigit), "Recovery code must be numeric digits.");

            // Verify user was stored with hashed password and hashed recovery code
            var users = _userRepo.GetUsers();
            var user = users.FirstOrDefault(u => u.Name == "newuser");
            Assert.NotNull(user);

            // Neither password nor recovery code should be stored in plaintext
            Assert.NotEqual("securePass123", user.Password);
            Assert.True(SecurityHelper.IsHashed(user.Password));

            Assert.NotEqual(result.RecoveryCode, user.RecoveryCodeHash);
            Assert.True(SecurityHelper.IsHashed(user.RecoveryCodeHash));

            // Verify that hashing verifies correctly
            Assert.True(SecurityHelper.VerifySecret("securePass123", user.Password));
            Assert.True(SecurityHelper.VerifySecret(result.RecoveryCode, user.RecoveryCodeHash));
        }

        [Fact]
        public void RecoveryCode_PreservesLeadingZeros_AndIsUnique()
        {
            var existingHashes = new System.Collections.Generic.List<string>();
            for (int i = 0; i < 50; i++)
            {
                string code = SecurityHelper.GenerateRecoveryCode(existingHashes);
                Assert.Equal(6, code.Length);
                Assert.True(code.All(char.IsDigit));

                string hash = SecurityHelper.HashSecret(code);
                Assert.DoesNotContain(hash, existingHashes);
                existingHashes.Add(hash);
            }
        }

        [Fact]
        public void ForgotPassword_ValidRecoveryCode_ResetsPasswordSuccessfully()
        {
            var authService = new AuthService(_userRepo);
            var reg = authService.AddUser("recoverme", "initialPassword");
            Assert.True(reg.IsSuccess);

            // Valid verification
            var verify = authService.VerifyRecoveryCode("recoverme", reg.RecoveryCode);
            Assert.True(verify.IsSuccess);

            // Reset password
            var reset = authService.ResetPassword("recoverme", reg.RecoveryCode, "brandNewPassword123");
            Assert.True(reset.IsSuccess);

            // Old password must fail
            var oldLogin = authService.LoginUser("recoverme", "initialPassword");
            Assert.False(oldLogin.IsSuccess);

            // New password must succeed
            var newLogin = authService.LoginUser("recoverme", "brandNewPassword123");
            Assert.True(newLogin.IsSuccess);
        }

        [Fact]
        public void ForgotPassword_InvalidRecoveryCode_ReturnsGenericErrorWithoutLeakingAccountInfo()
        {
            var authService = new AuthService(_userRepo);
            authService.AddUser("secretuser", "validPass1");

            // Wrong recovery code
            var result = authService.VerifyRecoveryCode("secretuser", "999999");
            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid username or recovery code.", result.ErrorMessage);

            // Non-existent user
            var result2 = authService.VerifyRecoveryCode("nonexistentuser", "123456");
            Assert.False(result2.IsSuccess);
            Assert.Equal("Invalid username or recovery code.", result2.ErrorMessage);

            // Empty inputs
            var result3 = authService.VerifyRecoveryCode("", "123456");
            Assert.False(result3.IsSuccess);
            Assert.Equal("Invalid username or recovery code.", result3.ErrorMessage);
        }

        [Fact]
        public void ForgotPassword_EnforcesPasswordValidationRules()
        {
            var authService = new AuthService(_userRepo);
            var reg = authService.AddUser("validationuser", "validPass1");

            // Password too short (< 4 chars)
            var resetShort = authService.ResetPassword("validationuser", reg.RecoveryCode, "12");
            Assert.False(resetShort.IsSuccess);
            Assert.Contains("at least 4 characters", resetShort.ErrorMessage);

            // Password empty
            var resetEmpty = authService.ResetPassword("validationuser", reg.RecoveryCode, "   ");
            Assert.False(resetEmpty.IsSuccess);
            Assert.Contains("cannot be empty", resetEmpty.ErrorMessage);
        }

        [Fact]
        public void LoginLockout_TriggersAfterConfiguredFailures_AndBlocksLogin()
        {
            var config = new AuthConfig(maxFailedAttempts: 3, lockoutDurationSeconds: 30);
            var authService = new AuthService(_userRepo, config);

            authService.AddUser("targetuser", "mypassword");

            // 1st failure
            var r1 = authService.LoginUser("targetuser", "wrong1");
            Assert.False(r1.IsSuccess);
            Assert.False(authService.IsLockedOut);
            Assert.Equal(1, authService.FailedAttempts);

            // 2nd failure
            var r2 = authService.LoginUser("targetuser", "wrong2");
            Assert.False(r2.IsSuccess);
            Assert.False(authService.IsLockedOut);
            Assert.Equal(2, authService.FailedAttempts);

            // 3rd failure -> Lockout triggered!
            var r3 = authService.LoginUser("targetuser", "wrong3");
            Assert.False(r3.IsSuccess);
            Assert.True(authService.IsLockedOut);
            Assert.True(authService.RemainingLockoutSeconds > 0);

            // 4th attempt during lockout -> blocked immediately
            var r4 = authService.LoginUser("targetuser", "mypassword");
            Assert.False(r4.IsSuccess);
            Assert.Contains("locked out", r4.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void LoginLockout_ResetsCounterOnSuccessfulLogin()
        {
            var config = new AuthConfig(maxFailedAttempts: 3, lockoutDurationSeconds: 30);
            var authService = new AuthService(_userRepo, config);
            authService.AddUser("user1", "goodpass");

            // 2 failures
            authService.LoginUser("user1", "bad1");
            authService.LoginUser("user1", "bad2");
            Assert.Equal(2, authService.FailedAttempts);

            // Successful login resets counter
            var success = authService.LoginUser("user1", "goodpass");
            Assert.True(success.IsSuccess);
            Assert.Equal(0, authService.FailedAttempts);
            Assert.False(authService.IsLockedOut);
        }

        [Fact]
        public void LoginLockout_AutomaticallyReEnablesAfterDurationExpires()
        {
            // Configure short lockout (1 second) for fast automated testing
            var config = new AuthConfig(maxFailedAttempts: 2, lockoutDurationSeconds: 1);
            var authService = new AuthService(_userRepo, config);
            authService.AddUser("user2", "goodpass");

            // Trigger lockout with 2 failures
            authService.LoginUser("user2", "bad1");
            authService.LoginUser("user2", "bad2");
            Assert.True(authService.IsLockedOut);

            // Wait for lockout to expire
            Thread.Sleep(1100);

            Assert.False(authService.IsLockedOut);
            Assert.Equal(0, authService.RemainingLockoutSeconds);

            // Now login should be accepted
            var success = authService.LoginUser("user2", "goodpass");
            Assert.True(success.IsSuccess);
        }

        [Fact]
        public void PasswordRecovery_WorksIndependentlyOfLockout_AndClearsLockout()
        {
            var config = new AuthConfig(maxFailedAttempts: 2, lockoutDurationSeconds: 30);
            var authService = new AuthService(_userRepo, config);
            var reg = authService.AddUser("lockeduser", "oldpassword");

            // Lock out user
            authService.LoginUser("lockeduser", "wrong1");
            authService.LoginUser("lockeduser", "wrong2");
            Assert.True(authService.IsLockedOut);

            // Password recovery works independently of lockout
            var resetResult = authService.ResetPassword("lockeduser", reg.RecoveryCode, "brandNewPass");
            Assert.True(resetResult.IsSuccess);

            // Lockout is now cleared
            Assert.False(authService.IsLockedOut);

            // User can immediately log in with new password
            var login = authService.LoginUser("lockeduser", "brandNewPass");
            Assert.True(login.IsSuccess);
        }

        [Fact]
        public void AuthConfig_LoadsDefaults_AndCustomSettingsFromConfigFile()
        {
            var configJson = new AuthConfigJson(_configFile);

            // Default
            var defaultConfig = configJson.LoadConfig();
            Assert.Equal(3, defaultConfig.MaxFailedAttempts);
            Assert.Equal(30, defaultConfig.LockoutDurationSeconds);

            // Custom save & load
            var customConfig = new AuthConfig(maxFailedAttempts: 5, lockoutDurationSeconds: 60);
            configJson.SaveConfig(customConfig);

            var loadedConfig = configJson.LoadConfig();
            Assert.Equal(5, loadedConfig.MaxFailedAttempts);
            Assert.Equal(60, loadedConfig.LockoutDurationSeconds);
        }

        [Fact]
        public void AuthMenu_ContainsForgotPasswordOption()
        {
            var authMenu = new AuthMenu();
            var field = typeof(AuthMenu).GetField("menu", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);

            string[]? menu = field.GetValue(authMenu) as string[];
            Assert.NotNull(menu);
            Assert.Equal(3, menu.Length);
            Assert.Equal(new[] { "Login", "Signup", "Forgot Password" }, menu);
        }
    }
}
