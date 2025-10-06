using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

class Program
{
    static async Task Main()
    {
        using TcpClient client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, 5000);
        Console.WriteLine("Connected to server");

        NetworkStream stream = client.GetStream();

        // List of example requests
        var requests = new List<object>
        {
            new { method="read", path="/api/categories", date=DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            new { method="read", path="/api/categories/1", date=DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            new { method="create", path="/api/categories", date=DateTimeOffset.UtcNow.ToUnixTimeSeconds(), body=new { name="Seafood" } },
            new { method="update", path="/api/categories/3", date=DateTimeOffset.UtcNow.ToUnixTimeSeconds(), body=new { cid=3, name="Test" } },
            new { method="delete", path="/api/categories/3", date=DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            new { method="echo", path="/test", date=DateTimeOffset.UtcNow.ToUnixTimeSeconds(), body="Hello, server!" },
        };

        foreach (var req in requests)
        {
            await SendRequest(stream, req);
        }

        client.Close();
        Console.WriteLine("All requests sent.");
    }

    static async Task SendRequest(NetworkStream stream, object requestObj)
    {
        string requestJson = JsonSerializer.Serialize(requestObj);
        byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson);
        await stream.WriteAsync(requestBytes, 0, requestBytes.Length);

        byte[] buffer = new byte[8192];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        string responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        var response = JsonSerializer.Deserialize<CJTPResponse>(responseJson);

        // Print like assignment examples
        string method = requestObj.GetType().GetProperty("method")?.GetValue(requestObj)?.ToString();
        string path = requestObj.GetType().GetProperty("path")?.GetValue(requestObj)?.ToString();

        Console.Write($"{method} {path} {response.Status}");

        if (!string.IsNullOrEmpty(response.Body))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<object>(response.Body);
                string pretty = JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine($" {pretty}");
            }
            catch
            {
                // Plain text for echo
                Console.WriteLine($" {response.Body}");
            }
        }
        else
        {
            Console.WriteLine();
        }

        Console.WriteLine(new string('-', 60));
    }

    class CJTPResponse
    {
        public string Status { get; set; }
        public string Body { get; set; }
    }
}
