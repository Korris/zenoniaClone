namespace ZenoniaProto.Api
{
    /// <summary>Outcome of one API call. ApiClient never throws for network / HTTP errors: check Ok, then Code / Message.</summary>
    public sealed class ApiResult<T> where T : class
    {
        public bool Ok;
        public T Data;
        public int Status;      // HTTP status, 0 when the request never got an answer
        public string Code;     // ApiErrorCodes value or server error code; null when Ok
        public string Message;  // human readable (server text or client side reason)
        public int RetryAfterSec;   // from a 429: how long until the next try is allowed, 0 when not said
        public string InvalidField; // from a validation_error: the first field the server refused ("email", "username", ...)

        public static ApiResult<T> Success(T data, int status) { return new ApiResult<T> { Ok = true, Data = data, Status = status }; }
        public static ApiResult<T> Fail(int status, string code, string message) { return new ApiResult<T> { Ok = false, Status = status, Code = code, Message = message }; }
        public static ApiResult<T> From<U>(ApiResult<U> other) where U : class { var r = Fail(other.Status, other.Code, other.Message); r.RetryAfterSec = other.RetryAfterSec; r.InvalidField = other.InvalidField; return r; }
    }
}
