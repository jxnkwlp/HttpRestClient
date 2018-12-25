using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace HttpRestClient
{
    /// <summary>
    ///  表示请求/构建
    /// </summary>
    public class RestRequest
    {
        public string Url { get; set; }

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        public HttpMethod HttpMethod { get; set; } = HttpMethod.Get;

        public IList<KeyValuePair<string, string>> QueryString { get; } = new List<KeyValuePair<string, string>>();

        public IList<KeyValuePair<string, string>> FormUrlData { get; } = new List<KeyValuePair<string, string>>();

        public MultipartFormDataContent FormData { get; } = new MultipartFormDataContent();

        public IList<KeyValuePair<string, IEnumerable<string>>> HeaderData { get; } = new List<KeyValuePair<string, IEnumerable<string>>>();

        public RestRequest(HttpMethod method, string url)
        {
            this.HttpMethod = method;
            Url = url;
        }

        public RestRequest()
        {
        }

        public RestRequest SetUrl(string url)
        {
            this.Url = url;
            return this;
        }

        public RestRequest SetHttpMethod(HttpMethod method)
        {
            this.HttpMethod = method;
            return this;
        }

        /// <summary>
        ///  超时 1-300s
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public RestRequest SetTimeout(TimeSpan time)
        {
            this.Timeout = time;
            return this;
        }

        public RestRequest AddQueryStringValue(string key, string value)
        {
            if (!string.IsNullOrEmpty(key))
                QueryString.Add(new KeyValuePair<string, string>(key, HttpUtility.UrlEncode(value)));
            return this;
        }

        public RestRequest SetQueryStringValue(string key, string value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                var qs = QueryString.FirstOrDefault(t => t.Key == key);
                QueryString.Remove(qs);

                QueryString.Add(new KeyValuePair<string, string>(key, HttpUtility.UrlEncode(value)));
            }
            return this;
        }

        public RestRequest AddQueryStringData(object data)
        {
            var list = ConventPropertyToParamList(data);

            foreach (var item in list)
            {
                QueryString.Add(new KeyValuePair<string, string>(item.Key, HttpUtility.UrlEncode(item.Value)));
            }

            return this;
        }

        public RestRequest RemoveQueryStringData(string key)
        {
            if (QueryString.Any(t => t.Key == key))
            {
                QueryString.Remove(QueryString.FirstOrDefault(t => t.Key == key));
            }

            return this;
        }

        public RestRequest AddHeaderValue(string key, string value)
        {
            if (!string.IsNullOrEmpty(key))
                HeaderData.Add(new KeyValuePair<string, IEnumerable<string>>(key, new string[] { value }));
            return this;
        }

        public RestRequest AddHeaderValue(string key, IEnumerable<string> value)
        {
            if (!string.IsNullOrEmpty(key))
                HeaderData.Add(new KeyValuePair<string, IEnumerable<string>>(key, value));
            return this;
        }

        public RestRequest AddFormUrlEncodedValue(string key, string value)
        {
            if (!string.IsNullOrEmpty(key))
                FormUrlData.Add(new KeyValuePair<string, string>(key, value));
            return this;
        }

        public RestRequest AddFormUrlEncodedData(object data)
        {
            var list = ConventPropertyToParamList(data);

            foreach (var item in list)
            {
                FormUrlData.Add(new KeyValuePair<string, string>(item.Key, item.Value));
            }

            return this;
        }

        public RestRequest RemoveFormUrlEncodedData(string key)
        {
            if (FormUrlData.Any(t => t.Key == key))
            {
                FormUrlData.Remove(FormUrlData.FirstOrDefault(t => t.Key == key));
            }

            return this;
        }

        public RestRequest AddFormData(string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                FormData.Add(new StringContent(value), key);
            return this;
        }

        public RestRequest AddFormData(string key, byte[] data)
        {
            if (!string.IsNullOrEmpty(key) && data != null)
                FormData.Add(new ByteArrayContent(data), key);
            return this;
        }

        public RestRequest AddFormData(byte[] data)
        {
            if (data != null)
                FormData.Add(new ByteArrayContent(data));
            return this;
        }

        public RestRequest AddFormData(string key, byte[] data, string fileName)
        {
            if (!string.IsNullOrEmpty(key) && data != null)
                FormData.Add(new ByteArrayContent(data), key, fileName);
            return this;
        }

        public RestRequest AddFormData(string key, Stream data)
        {
            if (!string.IsNullOrEmpty(key) && data != null)
                FormData.Add(new StreamContent(data), key);
            return this;
        }

        public RestRequest AddFormData(Stream data)
        {
            if (data != null)
                FormData.Add(new StreamContent(data));
            return this;
        }

        public RestRequest AddFormData(string key, Stream data, string fileName, string contentType = null)
        {
            if (!string.IsNullOrEmpty(key) && data != null)
            {
                var content = new StreamContent(data);
                if (!string.IsNullOrEmpty(contentType))
                {
                    if (System.Net.Http.Headers.MediaTypeHeaderValue.TryParse(contentType, out System.Net.Http.Headers.MediaTypeHeaderValue mediaType))
                    {
                        content.Headers.ContentType = mediaType;
                    }
                }

                FormData.Add(content, key, HttpUtility.UrlEncode(fileName));
            }

            return this;
        }

        private static IList<KeyValuePair<string, string>> ConventPropertyToParamList(object value)
        {
            if (value == null)
                return new List<KeyValuePair<string, string>>();

            if (value.GetType() == typeof(List<KeyValuePair<string, string>>))
            {
                return (value as List<KeyValuePair<string, string>>);
            }
            else if (value.GetType() == typeof(Dictionary<string, string>))
            {
                return (value as Dictionary<string, string>).ToArray();
            }
            else if (value.GetType() == typeof(Dictionary<string, object>))
            {
                var source = value as Dictionary<string, object>;

                return source.ToDictionary(t => t.Key, t => (t.Value == null ? null : t.Value.ToString().Trim())).ToArray();
            }
            else if (value.GetType().IsClass)
            {
                var result = new List<KeyValuePair<string, string>>();

                var props = value.GetType().GetProperties();
                foreach (var item in props)
                {
                    if (item.CustomAttributes.Any(c => c.AttributeType.Name == "HttpParamsIgnoreAttribute"))
                    {
                        continue;
                    }

                    if (item.CustomAttributes.Any(c => c.AttributeType.Name == "HttpParamIgnoreAttribute"))
                    {
                        continue;
                    }

                    object propValue = item.GetValue(value, null);

                    if (propValue is IList)
                    {
                        var listValue = propValue as IList;

                        for (int i = 0; i < listValue.Count; i++)
                        {
                            var v = listValue[i];
                            result.Add(new KeyValuePair<string, string>(item.Name, (v == null ? null : v.ToString().Trim())));
                        }
                    }
                    else
                        result.Add(new KeyValuePair<string, string>(item.Name, (propValue == null ? null : propValue.ToString().Trim())));
                }

                return result;
            }
            else
                throw new ArgumentException("Unknow the value type!");
        }
    }
}