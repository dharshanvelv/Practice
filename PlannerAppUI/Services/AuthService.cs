using System;
using System.Collections.Generic;
using System.Linq;
using PlannerApplication.Models;
using PlannerApplication.Repositories;
using PlannerApplication.Utility;

namespace PlannerApplication.Services
{
    public class AuthService
    {
        private readonly UserRepository userRepository;
        private AuthConfig authConfig;
        private int failedAttempts = 0;
        private DateTime? lockoutEndTime = null;

        public AuthConfig Config
        {
            get => authConfig;
            set => authConfig = value ?? new AuthConfig();
        }

        public int FailedAttempts => failedAttempts;

        public bool IsLockedOut => lockoutEndTime.HasValue && DateTime.UtcNow < lockoutEndTime.Value;

        public int RemainingLockoutSeconds =>
            IsLockedOut ? (int)Math.Ceiling((lockoutEndTime!.Value - DateTime.UtcNow).TotalSeconds) : 0;

        public AuthService(UserRepository? userRepository = null, AuthConfig? authConfig = null)
        {
            this.userRepository = userRepository ?? new UserRepository();
            this.authConfig = authConfig ?? new AuthConfigJson().LoadConfig();
        }

        public RegisterResult AddUser(string username, string password, string? recoveryCode = null)
        {
            if (!InputValidator.IsValidUsername(username, out string userErr))
            {
                return RegisterResult.Failure(userErr);
            }

            if (!InputValidator.IsValidPassword(password, out string passErr))
            {
                return RegisterResult.Failure(passErr);
            }

            if (userRepository.GetUsers().Any(p => string.Equals(p.Name, username.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return RegisterResult.Failure("Username already exists. Please choose another username.");
            }

            // Generate or validate 6-digit recovery code
            var existingHashes = userRepository.GetUsers()
                .Select(u => u.RecoveryCodeHash)
                .Where(h => !string.IsNullOrEmpty(h));

            string finalRecoveryCode = string.IsNullOrWhiteSpace(recoveryCode)
                ? SecurityHelper.GenerateRecoveryCode(existingHashes)
                : recoveryCode.Trim();

            // Store hashed password and hashed recovery code
            string passwordHash = SecurityHelper.HashSecret(password);
            string recoveryCodeHash = SecurityHelper.HashSecret(finalRecoveryCode);

            User user = new User(username.Trim(), passwordHash, recoveryCodeHash);
            userRepository.AddUser(user);

            return RegisterResult.Success(finalRecoveryCode);
        }

        public UserResult LoginUser(string username, string password)
        {
            // If locked out, block login attempts
            if (IsLockedOut)
            {
                return UserResult.Failure($"Account locked out. Try again in {RemainingLockoutSeconds}s.");
            }

            // If a previous lockout expired, automatically reset it
            if (lockoutEndTime.HasValue && DateTime.UtcNow >= lockoutEndTime.Value)
            {
                ResetLockout();
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                RecordFailedAttempt();
                if (IsLockedOut)
                {
                    return UserResult.Failure($"Maximum login attempts exceeded. Locked out for {RemainingLockoutSeconds}s.");
                }
                return UserResult.Failure("Invalid username or password");
            }

            var user = userRepository.GetUsers().FirstOrDefault(p =>
                string.Equals(p.Name, username.Trim(), StringComparison.OrdinalIgnoreCase) &&
                SecurityHelper.VerifySecret(password, p.Password));

            if (user == null)
            {
                RecordFailedAttempt();
                if (IsLockedOut)
                {
                    return UserResult.Failure($"Maximum login attempts exceeded. Locked out for {RemainingLockoutSeconds}s.");
                }
                return UserResult.Failure("Invalid username or password");
            }

            // Successful authentication resets the failed attempts counter
            ResetLockout();

            // Upgrade legacy plaintext password to secure hash if applicable
            if (user.Password == password && !SecurityHelper.IsHashed(user.Password))
            {
                user.Password = SecurityHelper.HashSecret(password);
                userRepository.UpdateUser(user);
            }

            return UserResult.Success(user.Id, user.Name);
        }

        public Result VerifyRecoveryCode(string username, string recoveryCode)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(recoveryCode))
            {
                return Result.Failure("Invalid username or recovery code.");
            }

            var user = userRepository.GetUsers().FirstOrDefault(p =>
                string.Equals(p.Name, username.Trim(), StringComparison.OrdinalIgnoreCase));

            if (user == null || string.IsNullOrEmpty(user.RecoveryCodeHash))
            {
                // Generic error message to prevent username enumeration
                return Result.Failure("Invalid username or recovery code.");
            }

            if (!SecurityHelper.VerifySecret(recoveryCode.Trim(), user.RecoveryCodeHash))
            {
                return Result.Failure("Invalid username or recovery code.");
            }

            return Result.Success();
        }

        public Result ResetPassword(string username, string recoveryCode, string newPassword)
        {
            var verifyResult = VerifyRecoveryCode(username, recoveryCode);
            if (!verifyResult.IsSuccess)
            {
                return verifyResult;
            }

            if (!InputValidator.IsValidPassword(newPassword, out string passErr))
            {
                return Result.Failure(passErr);
            }

            var user = userRepository.GetUsers().First(p =>
                string.Equals(p.Name, username.Trim(), StringComparison.OrdinalIgnoreCase));

            // Set new password hash
            user.Password = SecurityHelper.HashSecret(newPassword);
            userRepository.UpdateUser(user);

            // Password recovery works independently of lockout and clears any lockout
            ResetLockout();

            return Result.Success();
        }

        public void RecordFailedAttempt()
        {
            failedAttempts++;
            if (failedAttempts >= authConfig.MaxFailedAttempts)
            {
                lockoutEndTime = DateTime.UtcNow.AddSeconds(authConfig.LockoutDurationSeconds);
            }
        }

        public void ResetLockout()
        {
            failedAttempts = 0;
            lockoutEndTime = null;
        }

        public void SetLockout(int seconds)
        {
            failedAttempts = authConfig.MaxFailedAttempts;
            lockoutEndTime = DateTime.UtcNow.AddSeconds(seconds);
        }
    }
}