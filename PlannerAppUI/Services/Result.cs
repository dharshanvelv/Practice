namespace PlannerApplication.Services
{
    public class Result
    {
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        public Result(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public static Result Success()
        {
            return new Result(true, string.Empty);
        }

        public static Result Failure(string errorMessage)
        {
            return new Result(false, errorMessage);
        }
    }
}