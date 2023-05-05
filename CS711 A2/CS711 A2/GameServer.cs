using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CS711_A2
{
    public class GameServer
{
    private TcpListener _listener;

    public GameServer(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public async Task StartAsync()
    {
        _listener.Start();
        Console.WriteLine("Game Server Starting");

        while (true)
        {
            TcpClient client = await _listener.AcceptTcpClientAsync();
            Console.WriteLine("connected with: " + client.Client.RemoteEndPoint);
            Task clientTask = Task.Run(() => HandleClientAsync(client));
            Console.WriteLine("Game Server Started");
            
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    StreamReader reader = new StreamReader(stream);
                    StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
                    while (true)
                    {
                        
                        string requestLine = await reader.ReadLineAsync();
                        if (requestLine == null)
                        {
                            Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}");
                            break;
                        }
                        int requestStringIndexGet = requestLine.ToUpper().IndexOf("GET");
                        int requestStringIndexPost = requestLine.ToUpper().IndexOf("POST");
                        if (requestStringIndexGet > 0)
                        {
                            requestLine = requestLine.Substring(requestStringIndexGet);
                        }else if (requestStringIndexPost > 0)
                        {
                            requestLine = requestLine.Substring(requestStringIndexPost);
                        }
                        if (string.IsNullOrEmpty(requestLine))
                        {
                            writer.WriteLine("HTTP/1.1 405 Method Not Allowed");
                            writer.WriteLine("Content-Type: text/plain");
                            writer.WriteLine();
                            writer.WriteLine("Method not allowed.");
                        }
                        
                        Console.WriteLine($"Thread: {requestLine}");
                        string[] tokens = requestLine.Split(' ');
                        string method = tokens[0].ToUpper();
                        string path = tokens[1].ToLower();
                        string queryString = "";
                        int queryStringIndex = path.IndexOf('?');
                        if (queryStringIndex >= 0)
                        {
                            queryString = path.Substring(queryStringIndex + 1);
                            path = path.Substring(0, queryStringIndex);
                        }

                        Dictionary<string, string> parameters = Parse(queryString);
                        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: {method} {path} {parameters}");

                        // Handle GET requests
                        if (method == "GET")
                        {
                            if (path.StartsWith("/register"))
                            {
                                Console.WriteLine("register");
                            }
                            else if (path.StartsWith("/pairme"))
                            {
                                Console.WriteLine("pairme");
                                parameters.TryGetValue("player", out string username);
                                Console.WriteLine(username);

                            }
                            else if (path.StartsWith("/mymove"))
                            {
                                Console.WriteLine("mymove");
                            }
                            else if (path.StartsWith("/theirmove"))
                            {
                                Console.WriteLine("theirmove");

                            }
                            else if (path.StartsWith("/quit"))
                            {
                                Console.WriteLine("quit");

                            }
                            else
                            {
                                // Invalid request
                                writer.WriteLine("HTTP/1.1 400 Bad Request");
                                writer.WriteLine("Content-Type: text/plain");
                                writer.WriteLine();
                                writer.WriteLine("Invalid request.");
                            }
                        }
                        else
                        {
                            // Unsupported method
                            writer.WriteLine("HTTP/1.1 405 Method Not Allowed");
                            writer.WriteLine("Content-Type: text/plain");
                            writer.WriteLine();
                            writer.WriteLine("Method not allowed.");
                        }
                    }
                    
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}. Exception: {ex.Message}");
            }
            
        }
    }
    public static Dictionary<string, string> Parse(string queryString)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(queryString))
        {
            return parameters;
        }

        string[] pairs = queryString.Split('&');
        foreach (string pair in pairs)
        {
            string[] keyValue = pair.Split('=');

            if (keyValue.Length == 2)
            {
                string key = Uri.UnescapeDataString(keyValue[0]);
                string value = Uri.UnescapeDataString(keyValue[1]);

                parameters[key] = value;
            }
        }

        return parameters;
    }
}

}