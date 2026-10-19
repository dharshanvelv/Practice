namespace PlannerApplication.Services
{
    public class RegisterResult : Result
    {
        public string RecoveryCode { get; }

        public RegisterResult(bool isSuccess, string errorMessage, string recoveryCode = "")
            : base(isSuccess, errorMessage)
        {
            RecoveryCode = recoveryCode;
        }

        public static RegisterResult Success(string recoveryCode)
        {
            return new RegisterResult(true, string.Empty, recoveryCode);
        }

        public new static RegisterResult Failure(string errorMessage)
        {
            return new RegisterResult(false, errorMessage, string.Empty);
        }
    }
}
