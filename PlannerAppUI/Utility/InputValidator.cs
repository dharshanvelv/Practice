using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace PlannerApplication.Utility
{
    public static class InputValidator
    {
        public static bool IsValidInt(string input, out int result)
        {
            return int.TryParse(input, out result);
        }
        public static bool IsValidDouble(string input, out double result)
        {
            return double.TryParse(input, out result);
        }
        public static bool IsValidDecimal(string input, out decimal result)
        {
            return decimal.TryParse(input, out result);
        }
        public static bool IsValidString(string input)
        {
            return !string.IsNullOrWhiteSpace(input);
        }

        public static bool IsValidUsername(string? username, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(username))
            {
                errorMessage = "Username cannot be empty";
                return false;
            }
            string trimmed = username.Trim();
            if (trimmed.Length < 3)
            {
                errorMessage = "Username must be at least 3 characters";
                return false;
            }
            if (trimmed.Length > 25)
            {
                errorMessage = "Username cannot exceed 25 characters";
                return false;
            }
            return true;
        }

        public static bool IsValidPassword(string? password, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Password cannot be empty";
                return false;
            }
            if (password.Length < 4)
            {
                errorMessage = "Password must be at least 4 characters";
                return false;
            }
            if (password.Length > 50)
            {
                errorMessage = "Password cannot exceed 50 characters";
                return false;
            }
            return true;
        }

        public static bool IsValidDateRange(DateOnly start, DateOnly end, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (end < start)
            {
                errorMessage = "End date cannot be earlier than start date";
                return false;
            }
            return true;
        }

        //To check values are within range and under maxcharacters

        public static bool IsIntWithinRange(int value, int min, int max)
        {
            return value >= min && value <= max;
        }

        public static bool IsDoubleWithinRange(double value, double min, double max)
        {
            return value >= min && value <= max;
        }

        public static bool IsStringWithinMaxLength(string input, int maxLength)
        {
            return input.Length <= maxLength;
        }

        //To check other types

        public static bool IsValidDate(string input, out DateOnly date)
        {
            return DateOnly.TryParse(input, out date);
        }

        public static bool IsValidPastDate(DateOnly date, int maxYears)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            DateOnly cutOffDate = today.AddYears(-maxYears);
            return date <= today && date >= cutOffDate;
        }

        public static bool IsValidFutureDate(DateOnly date, int maxYears)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            DateOnly cutOffDate = today.AddYears(maxYears);
            return date >= today && date <= cutOffDate;
        }
    }
}