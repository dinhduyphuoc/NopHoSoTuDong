using System;
using System.Net;
using System.Net.Http;

namespace NopHoSoTuDong.Models
{
    public class ApiHttpException : HttpRequestException
    {
        public new HttpStatusCode StatusCode { get; }
        public string? ResponseBody { get; }

        public ApiHttpException(HttpStatusCode statusCode, string? responseBody, string message)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }

    public class AuthException : Exception
    {
        public AuthException(string message) : base(message) { }
    }
}
