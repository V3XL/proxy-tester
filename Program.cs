using System.Net;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string TestProxy(string host, string port, string proxyType){
    HttpClient client = new HttpClient(new SocketsHttpHandler()
    {
        Proxy = new WebProxy(proxyType + "://"+host+":"+port)
    });
    client.Timeout = TimeSpan.FromSeconds(10);
    string content = null;
    try{
        if(client.GetStringAsync("http://ipinfo.io/").Result != null){
            content = "{'status':'success'}";
        }
    }catch (Exception){
        content = "{'status':'error'}";
    }
    return content;
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

app.MapGet("/socks", (string host, string port) =>
    TestProxy(host, port, "socks")
);


app.MapGet("/http", (string host, string port) =>
    TestProxy(host, port, "http")
);

app.Run();
