// See https://aka.ms/new-console-template for more information


using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using WatsonTcp;

Console.WriteLine("Test SERVER 02 по шагам от ИИ");

/*
var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 20000);
listener.Start();
Console.WriteLine("Diagnostic server started...");

using var client = listener.AcceptTcpClient();
Console.WriteLine("Client connected!");

var stream = client.GetStream();
byte[] buffer = new byte[4];

// 1. Read handshake
stream.Read(buffer, 0, 4);
Console.WriteLine($"Handshake: {BitConverter.ToString(buffer)}");

// 2. Read length
stream.Read(buffer, 0, 4);
Console.WriteLine($"Length bytes: {BitConverter.ToString(buffer)}");
uint length = BitConverter.ToUInt32(buffer, 0);
Console.WriteLine($"Raw length value: {length} (0x{length:X8})");

// 3. Read data
buffer = new byte[length];
int read = stream.Read(buffer, 0, buffer.Length);
Console.WriteLine($"Received: {Encoding.UTF8.GetString(buffer, 0, read)}");

// 4. Send response
byte[] response = Encoding.UTF8.GetBytes("ACK: " + length);
stream.Write(BitConverter.GetBytes(response.Length), 0, 4);
stream.Write(response, 0, response.Length);
*/




StartServer(20000);
StartServer(20010);
Console.ReadLine();

void StartServer(int port)
{
    var server = new WatsonTcpServer("127.0.0.1", port);

    server.Events.ClientConnected += (s, e) =>
        Console.WriteLine($"[{port}] Client connected: {e.Client.IpPort}");

    server.Events.MessageReceived += (s, e) =>
    {
        string msg = Encoding.UTF8.GetString(e.Data);
        Console.WriteLine($"[{port}] Received: {msg}");
        server.SendAsync(e.Client.Guid, $"ACK from {port}: {msg}");
    };

    server.Events.MessageReceived += (s, e) => {
        Console.WriteLine($"[{port}] Получено {e.Data.Length} байт:");
        Console.WriteLine(HexDump(e.Data)); // Вывод сырых данных
        Console.WriteLine($"Текст: {Encoding.UTF8.GetString(e.Data)}");
    };

    server.Events.MessageReceived += (s, e) => {
        Console.WriteLine($"Raw data ({e.Data.Length} bytes):");
        Console.WriteLine(BitConverter.ToString(e.Data).Replace("-", " "));

        if (e.Data.Length >= 4)
        {
            var length = BitConverter.ToUInt32(e.Data, 0);
            Console.WriteLine($"Declared length: {length}");
        }
    };

    static string HexDump(byte[] data)
    {
        return BitConverter.ToString(data).Replace("-", " ");
    }
    server.Start();
    Console.WriteLine($"Server started on port {port}");
}

static string HexDump(byte[] data)
{
    return BitConverter.ToString(data).Replace("-", " ");
}
