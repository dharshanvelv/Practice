namespace PlannerApplication.Services
{
    public class UserResult
    {
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        public Guid Id { get; set; } = Guid.Empty;
        public string UserName { get; set; } = string.Empty;

        public UserResult(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public UserResult(Guid id, string userName, bool isSuccess, string errormessage)
        {
            this.Id = id;
            this.UserName = userName;
            this.IsSuccess = isSuccess;
            this.ErrorMessage = errormessage;
        }

        /// <summary>
        /// Returns a result instance with true 
        /// </summary>
        /// <returns>Result</returns>
        public static UserResult Success(Guid id, string userName)
        {
            return new UserResult(id, userName, true, string.Empty);
        }

        /// <summary>
        /// Returns a result instance with false along with the errormessage.
        /// </summary>
        /// <param name="errorMessage">errormessage</param>
        /// <returns>Result</returns>
        public static UserResult Failure(string errorMessage)
        {
            return new UserResult(Guid.Empty, string.Empty, false, errorMessage);
        }
    }
}