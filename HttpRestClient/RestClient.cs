using Polly;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HttpRestClient
{
    /// <summary>
    ///  http 包装，用于请求接口 
    /// </summary>
    public class RestClient
    {
        private static Dictionary<string, RestClient> _instance = new Dictionary<string, RestClient>();

        private static Dictionary<string, HttpClient> _httpclients = new Dictionary<string, HttpClient>();

        private static object lockKey = new object();

        public string ClientName { get; private set; }

        /// <summary>
        ///  获取 client
        /// </summary>
        public static RestClient GetClient(string name)
        {
            lock (lockKey)
            {
                if (!_instance.ContainsKey(name))
                {
                    _instance[name] = new RestClient(name);
                }

                if (!_httpclients.ContainsKey(name))
                {
                    _httpclients[name] = CreateDefaultHttpClient();
                }

                return _instance[name];
            }
        }

        protected RestClient(string name)
        {
            this.ClientName = name;
        }

        /// <summary>
        ///  发起请求并获取结果
        /// </summary>
        public async Task<RestResponse> ExecuteAsync(RestRequest request)
        {
            return await ExecuteCoreAsync(request);
        }

        /// <summary>
        ///  发起请求并获取结果
        /// </summary>
        public async Task<RestResponse> ExecuteAsync(Action<RestRequest> action)
        {
            var request = new RestRequest();
            action?.Invoke(request);

            return await ExecuteAsync(request);
        }

        private async Task<RestResponse> ExecuteCoreAsync(RestRequest request)
        {
            var httpClient = _httpclients[this.ClientName];

            HttpRequestMessage requestMessage = BuilderRequestMessage(request);
            HttpResponseMessage responseMessage = null;

            string requestUrl = request.Url;
            RestResponse response = null;
            HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError;

            int timeout = (int)request.Timeout.TotalSeconds;

            if (timeout < 1)
            {
                throw new Exception("超时时间需大于1s");
            }
            if (timeout > 300) //5min
            {
                throw new Exception("超时时间需小于300s(5min)");
            }

            try
            {
                //CancellationTokenSource cancellationToken = new CancellationTokenSource();

                Policy timeoutPolicy = Policy.TimeoutAsync(timeout, TimeoutStrategy.Optimistic); //乐观

                responseMessage = await timeoutPolicy.ExecuteAsync(async (ct) => await httpClient.SendAsync(requestMessage, ct), CancellationToken.None);

                httpStatusCode = responseMessage.StatusCode;

                responseMessage.EnsureSuccessStatusCode();

                var data = await responseMessage.Content.ReadAsByteArrayAsync();

                response = new RestResponse(request, responseMessage.StatusCode, data);
            }
            catch (HttpRequestException ex) // 请求异常。eg:网络异常
            {
                response = new RestResponse(request, httpStatusCode, null);
                response.ErrorMessage = "操作失败：网络异常";
            }
            catch (TimeoutException ex) // 请求超时。
            {
                response = new RestResponse(request, httpStatusCode, null);
                response.ErrorMessage = "操作失败：请求已超时";
            }
            catch (TaskCanceledException ex) // 请求超时。
            {
                response = new RestResponse(request, httpStatusCode, null);
                response.ErrorMessage = "操作失败：请求已超时";
            }
            catch (Exception ex) // 其他错误。
            {
                response = new RestResponse(request, httpStatusCode, null);
                response.ErrorMessage = "操作失败：接口请求失败";
            }

            return response;
        }

        #region methods

        private static HttpClient CreateDefaultHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Connection.Add("keep-alive");

            httpClient.Timeout = TimeSpan.FromMinutes(10);

            return httpClient;
        }

        private HttpRequestMessage BuilderRequestMessage(RestRequest request)
        {
            var fullUrl = BuildRequestUrl(this.ClientName, request);

            var message = new HttpRequestMessage(request.HttpMethod, fullUrl);

            message.Method = request.HttpMethod;

            if (message.Method == HttpMethod.Get)
            {
                // nothing todo
            }
            else if (message.Method == HttpMethod.Post)
            {
                var formUrlData = request.FormUrlData.Where(t => !string.IsNullOrEmpty(t.Value)).ToList();

                if (formUrlData.Count > 0)
                {
                    //  Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                    message.Content = new FormUrlEncodedContent(formUrlData);
                }

                if (request.FormData.Count() > 0)
                {
                    if (formUrlData.Count > 0)
                    {
                        foreach (var item in formUrlData)
                        {
                            request.FormData.Add(new StringContent(item.Value), item.Key);
                        }
                    }

                    message.Content = request.FormData;
                }
            }
            else
            {
                throw new NotSupportedException("message.Method");
            }

            return message;
        }

        private static string BuildRequestUrl(string name, RestRequest request)
        {
            if (request.Url.Contains("http"))
                return request.Url;

            var url = request.Url;

            var queryString = string.Join("&", request.QueryString.Where(t => !string.IsNullOrEmpty(t.Value)).Select(t => t.Key + "=" + t.Value));

            if (url.Contains("?"))
                url = url + "&" + queryString;
            else
                url = url + "?" + queryString;

            return url;
        }

        #endregion methods
    }
}