namespace HttpRestClient
{
    internal class Test
    {
        public async System.Threading.Tasks.Task RunAsync()
        {
            var response = await RestClient.GetClient("default").ExecuteAsync(t =>
              {
                  t.Url = "http://xxx.com/api/product/list";
                  t.AddQueryStringData(new { a = 1, b = 2 });
              });

            if (!response.IsError)
            {

            }
        }
    }
}