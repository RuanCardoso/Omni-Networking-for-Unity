namespace Omni.Core
{
    public class Response
    {
        public byte Status { get; set; }
        public string Message { get; set; }
    }

    public class Response<T> : Response
    {
        public T Data { get; set; }
    }
}
