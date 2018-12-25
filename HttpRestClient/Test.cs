namespace HttpRestClient
{
    internal class Test
    {
        public Test()
        {
            var response = RestClient.GetClient("default").ExecuteAsync(t =>
              {
                  t.Url = "http://xxx.com/api/product/list";
                  t.AddQueryStringData(new { a = 1, b = 2 });
              });
        }
    }
}