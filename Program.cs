using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

JObject TestProxy(string host, string port, string proxyType)
{
    using (var handler = new SocketsHttpHandler
    {
        Proxy = new WebProxy($"{proxyType}://{host}:{port}"),
        UseProxy = true
    })
    using (var client = new HttpClient(handler))
    {
        client.Timeout = TimeSpan.FromSeconds(10);
        string content = null;
        long responseTime = 0;
        bool isSuccess = false;
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            content = client.GetStringAsync("http://ifconfig.me/ip").GetAwaiter().GetResult();
            stopwatch.Stop();
            responseTime = stopwatch.ElapsedMilliseconds;

            //Check that an IP is address string is returned by ifconfig.me
            Regex r = new Regex(@"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$");
            Match match = r.Match(content);

            if(match.Success){
                isSuccess = true;
            }

        }
        catch (Exception){}

        JObject result = new JObject();
        result.Add("successful", isSuccess);
        result.Add("responseTime", responseTime);
        
        if(isSuccess){
            Console.WriteLine($"[{host}:{port}] Type: {proxyType.ToUpper()} | Successful: {isSuccess} | ResponseTime: {responseTime}ms ");
        }else{
            Console.WriteLine($"[{host}:{port}] Type: {proxyType.ToUpper()} | Successful: {isSuccess}");
        }
        

        return result;
    }
}

app.MapGet("/", async context =>
{
    var response = 
    "<b>SOCKS usage:</b><br>" +
    "GET /http?host=1.2.3.4&port=8080<br><br>" +
    "<b>HTTP usage:</b><br>" +
    "GET /socks?host=4.3.2.1&port=1080";
    
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(response);
});

app.MapGet("/socks", (string host, string port) => {
    string jsonResult = TestProxy(host, port, "socks").ToString(Formatting.None);
    return Results.Text(jsonResult, "application/json"); 
});

app.MapGet("/http", (string host, string port) => {
    string jsonResult = TestProxy(host, port, "http").ToString(Formatting.None);
    return Results.Text(jsonResult, "application/json"); 
});

app.Urls.Add("http://*:80");
app.Run();
