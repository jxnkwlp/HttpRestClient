using System.Net;
using System.Text;

namespace HttpRestClient
{
    /// <summary>
    ///  表示响应/包装
    /// </summary>
    public partial class RestResponse
    {
        public byte[] Data { get; }

        public HttpStatusCode StatusCode { get; }

        public bool IsError { get; }

        public bool IsHttpError { get; }

        public string DataString { get; private set; }

        public RestRequest Request { get; }

        public string ErrorMessage { get; set; }

        public RestResponse(RestRequest request, HttpStatusCode statusCode, byte[] data)
        {
            this.Request = request;
            this.StatusCode = statusCode;
            this.Data = data;

            if (statusCode != HttpStatusCode.OK)
            {
                this.IsError = true;
                this.IsHttpError = true;
            }

            ToDataString();
        }

        public string ToDataString()
        {
            if (this.Data == null)
                return null;

            if (string.IsNullOrEmpty(DataString))
            {
                try
                {
                    this.DataString = Encoding.UTF8.GetString(this.Data);
                }
                catch
                {
                }
            }

            return this.DataString;
        }
    }
}