using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await EchoLoop(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

async Task EchoLoop(WebSocket webSocket)
{
    var buffer = new byte[1024 * 4];
    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

    while (!result.CloseStatus.HasValue)
    {
        string filePath = "DataBase.json";
        var receivedText = Encoding.UTF8.GetString(buffer, 0, result.Count);


        SaveTextToJsonFile(receivedText, filePath);

        Console.WriteLine($"Received: {receivedText}");

        var sendText = Encoding.UTF8.GetBytes($"Echo: {receivedText}");
        await webSocket.SendAsync(new ArraySegment<byte>(sendText, 0, sendText.Length), result.MessageType, result.EndOfMessage, CancellationToken.None);

        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    }

    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
}
static void SaveTextToJsonFile(string text, string filePath)
{
    var textMessage = new TextMessage
    {
        Timestamp = DateTime.Now,
        Content = text
    };

    using (StreamWriter writer = new StreamWriter(filePath, true))
    {
        writer.WriteLine(JsonConvert.SerializeObject(textMessage));
        writer.WriteLine();
    }
}
app.Run("http://localhost:8855");

public class TextMessage
{
    public DateTime Timestamp { get; set; }
    public string Content { get; set; }
}
