using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CS711_A2
{
    public class GameServer
{
    private TcpListener _listener;
    private IPAddress _IP;
    private int _port;
    private string[] _englishWords = new string[] { "apple", "banana", "orange", "kiwi", "mango", "grape", "pear", "pineapple", "watermelon", "strawberry", 
        "lemon", "peach", "plum", "avocado", "blueberry", "coconut", "pomegranate", "cherry", "fig", "apricot", 
        "papaya", "guava", "mangosteen", "dragonfruit", "deer", "lion", "tiger", "giraffe", "elephant", "monkey", 
        "zebra", "kangaroo", "rhinoceros", "crocodile", "hippopotamus", "panda", "koala", "gorilla", "leopard", 
        "cheetah", "hyena", "camel", "gazelle", "buffalo", "penguin", "seagull", "dolphin", "shark", "whale", 
        "jellyfish", "octopus", "seahorse", "starfish", "lobster", "crab", "turtle" };

    private Dictionary<string[], string> _pairDictionary = new Dictionary<string[], string>();
    private List<string> _usedWords = new List<string>();


    public GameServer(IPAddress IP, int port)
    {
        _listener = new TcpListener(IP, port);
        _IP = IP;
        _port = port;

    }

    public async Task StartAsync()
    {
        _listener.Start();
        Console.WriteLine("Game Server Starting");

        while (true)
        {
            Console.WriteLine("Game Server Started");
            Console.WriteLine($"Listening at {_IP}:{_port} ");
            TcpClient client = await _listener.AcceptTcpClientAsync();
            Console.WriteLine("Connection established with: " + client.Client.RemoteEndPoint);
            Task clientTask = Task.Run(() => HandleClientAsync(client));
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
                        // if (string.IsNullOrEmpty(requestLine))
                        // {
                        //     writer.WriteLine("HTTP/1.1 405 Method Not Allowed");
                        //     writer.WriteLine("Content-Type: text/plain");
                        //     writer.WriteLine("IsNullOrEmpty");
                        //     writer.WriteLine("Method not allowed.");
                        // }
                        
                        // Console.WriteLine($"Thread: {requestLine}");
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
                        //Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: {method} {path}");
                        
                        // Handle GET requests
                        if (method == "GET")
                        {
                            if (path.StartsWith("/favicon.ico"))
                            {
                                try
                                {
                                    byte[] iconBytes = File.ReadAllBytes("../../favicon.ico");
                                    writer.WriteLine("HTTP/1.1 200 OK");
                                    writer.WriteLine("Content-Type: image/x-icon");
                                    writer.WriteLine($"Content-Length: {iconBytes.Length}");
                                    writer.WriteLine();
                                    await writer.FlushAsync();
                                    await stream.WriteAsync(iconBytes, 0, iconBytes.Length);
                                    
                                }
                                catch (FileNotFoundException)
                                {
                                    writer.WriteLine("HTTP/1.1 404 Not Found");
                                    writer.WriteLine("Content-Type: text/plain");
                                    writer.WriteLine();
                                    writer.WriteLine("Favicon not found.");
                                }
                            }
                            if (path.StartsWith("/register"))
                            {
                                Console.WriteLine("register");
                                //writer.WriteLine("register");
                                writer.WriteLine("HTTP/1.1 200 Ok");
                                writer.WriteLine("Content-Type: text/plain");
                                writer.WriteLine();
                                writer.WriteLine("register");
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
                            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} sent response to {client.Client.RemoteEndPoint} for {path}");
                        }
                        // else
                        // {
                        //     // Unsupported method
                        //     writer.WriteLine("HTTP/1.1 405 Method Not Allowed");
                        //     writer.WriteLine("Content-Type: text/plain");
                        //     writer.WriteLine();
                        //     writer.WriteLine("Method not allowed.");
                        // }
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