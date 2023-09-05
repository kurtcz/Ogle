using System.Net;

namespace Ogle
{
    internal class NodeResponse<T>
    {
        public HttpStatusCode StatusCode { get; set; }
        public T? Payload { get; set; }
        public string? Error { get; set; }
    }
}

