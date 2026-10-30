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

        public static ApiResult<T> Success(T data, int status) { return new ApiResult<T> { Ok = true, Data = data, Status = status }; }
        public static ApiResult<T> Fail(int status, string code, string message) { return new ApiResult<T> { Ok = false, Status = status, Code = code, Message = message }; }
        public static ApiResult<T> From<U>(ApiResult<U> other) where U : class { return Fail(other.Status, other.Code, other.Message); }
    }
}
