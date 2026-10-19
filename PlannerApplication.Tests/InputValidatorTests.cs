using System;
using PlannerApplication.Utility;
using Xunit;

namespace PlannerApplication.Tests
{
    public class InputValidatorTests
    {
        [Theory]
        [InlineData("admin", true)]
        [InlineData("alice123", true)]
        [InlineData("ab", false)] // too short
        [InlineData("", false)] // empty
        [InlineData("   ", false)] // whitespace
        public void IsValidUsername_ValidatesCorrectly(string username, bool expected)
        {
            bool result = InputValidator.IsValidUsername(username, out string err);
            Assert.Equal(expected, result);
            if (!expected)
            {
                Assert.NotEmpty(err);
            }
        }

        [Theory]
        [InlineData("password123", true)]
        [InlineData("1234", true)]
        [InlineData("123", false)] // too short
        [InlineData("", false)]
        public void IsValidPassword_ValidatesCorrectly(string password, bool expected)
        {
            bool result = InputValidator.IsValidPassword(password, out string err);
            Assert.Equal(expected, result);
            if (!expected)
            {
                Assert.NotEmpty(err);
            }
        }

        [Fact]
        public void IsValidDateRange_ValidatesProperly()
        {
            DateOnly start = new DateOnly(2026, 1, 1);
            DateOnly end = new DateOnly(2026, 1, 10);

            Assert.True(InputValidator.IsValidDateRange(start, end, out _));
            Assert.False(InputValidator.IsValidDateRange(end, start, out string err));
            Assert.NotEmpty(err);
        }

        [Theory]
        [InlineData("123", true, 123)]
        [InlineData("abc", false, 0)]
        public void IsValidInt_ParsesCorrectly(string input, bool expectedValid, int expectedVal)
        {
            bool valid = InputValidator.IsValidInt(input, out int result);
            Assert.Equal(expectedValid, valid);
            if (expectedValid)
            {
                Assert.Equal(expectedVal, result);
            }
        }

        [Theory]
        [InlineData("2026-10-18", true)]
        [InlineData("invalid-date", false)]
        public void IsValidDate_ParsesCorrectly(string input, bool expectedValid)
        {
            bool valid = InputValidator.IsValidDate(input, out DateOnly date);
            Assert.Equal(expectedValid, valid);
            if (expectedValid)
            {
                Assert.Equal(2026, date.Year);
            }
        }
    }
}
