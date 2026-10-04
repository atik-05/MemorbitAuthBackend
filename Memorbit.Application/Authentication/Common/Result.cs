namespace Memorbit.Application.Authentication.Common
{
    public class Result
    {
        public bool IsSuccess { get; }

        public AuthError? Error { get; }

        protected Result(bool isSuccess, AuthError? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new(true, null);

        public static Result Failure(AuthError error) => new(false, error);

        public static implicit operator Result(AuthError error) => Failure(error);
    }

    public sealed class Result<T> : Result
    {
        public T? Value { get; }

        private Result(bool isSuccess, T? value, AuthError? error)
            : base(isSuccess, error)
        {
            Value = value;
        }

        public static Result<T> Success(T value) => new(true, value, null);

        public static new Result<T> Failure(AuthError error) => new(false, default, error);

        public static implicit operator Result<T>(AuthError error) => Failure(error);
    }
}
