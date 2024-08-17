using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

var builder = WebApplication.CreateBuilder(args);


// Configure logging to filter out informational logs
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Result.ContentResult", LogLevel.Warning);
Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
Trace.AutoFlush = true;

var app = builder.Build();

async Task<JObject> TestProxyAsync(string host, string port, string proxyType, string timeout)
{
    using (var handler = new SocketsHttpHandler
    {
        Proxy = new WebProxy($"{proxyType}://{host}:{port}"),
        UseProxy = true
    })
    using (var client = new HttpClient(handler))
    {
        client.Timeout = TimeSpan.FromMilliseconds(int.Parse(timeout));
        string content = null;
        long responseTime = 0;
        bool isSuccess = false;
        try
        {
            var stopwatch = Stopwatch.StartNew();
            content = await client.GetStringAsync("http://ifconfig.me/ip");
            stopwatch.Stop();
            responseTime = stopwatch.ElapsedMilliseconds;

            // Check that an IP address string is returned by ifconfig.me
            Regex r = new Regex(@"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$");
            Match match = r.Match(content);

            if (match.Success)
            {
                isSuccess = true;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        JObject result = new JObject
        {
            ["successful"] = isSuccess,
            ["responseTime"] = responseTime
        };

        Trace.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Proxy: {proxyType.ToLower()}://{host}:{port} | Successful: {isSuccess} | ResponseTime: {responseTime}ms");

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

app.MapGet("/socks", async (HttpContext context) =>
{
    var host = context.Request.Query["host"].ToString();
    var port = context.Request.Query["port"].ToString();
    var timeout = context.Request.Query["timeout"].ToString();
    
    if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(port) || string.IsNullOrEmpty(timeout))
    {
        return Results.BadRequest("Missing query parameters.");
    }

    var result = await TestProxyAsync(host, port, "socks", timeout);
    return Results.Text(result.ToString(), "application/json");
});

app.MapGet("/http", async (HttpContext context) =>
{
    var host = context.Request.Query["host"].ToString();
    var port = context.Request.Query["port"].ToString();
    var timeout = context.Request.Query["timeout"].ToString();
    
    if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(port) || string.IsNullOrEmpty(timeout))
    {
        return Results.BadRequest("Missing query parameters.");
    }

    var result = await TestProxyAsync(host, port, "http", timeout);
    return Results.Text(result.ToString(), "application/json");
});

app.Urls.Add("http://*:80");
app.Run();
