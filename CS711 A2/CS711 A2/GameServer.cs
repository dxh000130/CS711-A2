using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
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

    private List<Dictionary<string, string>> _pairDictionary = new List<Dictionary<string, string>>();
    private List<string> _usedWords = new List<string>();
    private List<string> _waitForPair = new List<string>();

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
                                Random random = new Random();
                                int randomIndex = random.Next(_englishWords.Length);
                                string randomUsername = _englishWords[randomIndex];
                                while (_usedWords.Contains(randomUsername))
                                {
                                    randomIndex = random.Next(_englishWords.Length);
                                    randomUsername = _englishWords[randomIndex];
                                }
                                _usedWords.Add(randomUsername);
                                writer.WriteLine("HTTP/1.1 200 Ok");
                                writer.WriteLine("Content-Type: text/plain");
                                writer.WriteLine();
                                writer.WriteLine(randomUsername);
                            }
                            else if (path.StartsWith("/pairme"))
                            {
                                if (parameters.ContainsKey("player"))
                                {
                                    string username = parameters["player"];
                                    if (_usedWords.Contains(username))
                                    {
                                        if (_waitForPair.Count == 0)
                                        {
                                            
                                            _waitForPair.Add(username);
                                            Guid myGuid = Guid.NewGuid();
                                            Dictionary<string, string> dic = new Dictionary<string, string>
                                            {
                                                { "id", myGuid.ToString() }, { "player1", username }, { "player2", "" },
                                                { "state", "wait" }, { "lastMovePlayer1", "" }, { "lastMovePlayer2", "" }
                                            };
                                            _pairDictionary.Add( dic);
                                            writer.WriteLine("HTTP/1.1 200 Ok");
                                            writer.WriteLine("Content-Type: application/json");
                                            writer.WriteLine();
                                            writer.WriteLine(JsonConvert.SerializeObject(dic));
                                        }
                                        else
                                        {
                                            string player1 = _waitForPair[0];
                                            _waitForPair.RemoveAt(0);
                                            Dictionary<string, string> foundDictionary = _pairDictionary.Find(d => d.ContainsKey("player1") && d["player1"] == player1);
                                            if (foundDictionary != null)
                                            {
                                                foundDictionary["player2"] = username;
                                                foundDictionary["state"] = "progress";
                                            }
                                            writer.WriteLine("HTTP/1.1 200 Ok");
                                            writer.WriteLine("Content-Type: text/plain");
                                            writer.WriteLine();
                                            writer.WriteLine(JsonConvert.SerializeObject(foundDictionary));
                                            Console.WriteLine(JsonConvert.SerializeObject(_pairDictionary));
                                        }
                                    }
                                    else
                                    {
                                        writer.WriteLine("HTTP/1.1 400 Bad Request");
                                        writer.WriteLine("Content-Type: text/plain");
                                        writer.WriteLine();
                                        writer.WriteLine("Wrong username");
                                    }
                                    
                                }
                                else
                                {
                                    writer.WriteLine("HTTP/1.1 400 Bad Request");
                                    writer.WriteLine("Content-Type: text/plain");
                                    writer.WriteLine();
                                    writer.WriteLine("Please input parameters [player]");
                                }

                                //Console.WriteLine(username);

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
                                 if (parameters.ContainsKey("player") && parameters.ContainsKey("id"))
                                {
                                    string username = parameters["player"];
                                    string id = parameters["id"];
                                    Dictionary<string, string> foundDictionary = _pairDictionary.Find(d => d.ContainsKey("player1") && (d["player1"] == username || d["player2"] == username) && d["id"] == id);
                                    if (foundDictionary != null)
                                    {
                                        if (foundDictionary["player1"] == username)
                                        {
                                            _waitForPair.Add(foundDictionary["player2"]);
                                            _usedWords.Remove(foundDictionary["player1"]);
                                        }else if(foundDictionary["player2"] == username)
                                        {
                                            _waitForPair.Add(foundDictionary["player1"]);
                                            _usedWords.Remove(foundDictionary["player2"]);
                                        }

                                        _pairDictionary.Remove(foundDictionary);
                                        writer.WriteLine("HTTP/1.1 200 Ok");
                                        writer.WriteLine("Content-Type: text/plain");
                                        writer.WriteLine();
                                        writer.WriteLine(JsonConvert.SerializeObject(_waitForPair));
                                        writer.WriteLine(JsonConvert.SerializeObject(_usedWords));
                                    }
                                    else
                                    {
                                        writer.WriteLine("HTTP/1.1 404 Not Found");
                                        writer.WriteLine("Content-Type: text/plain");
                                        writer.WriteLine();
                                        writer.WriteLine("Wrong username or ID");
                                    }
                                    
                                }
                                else
                                {
                                    writer.WriteLine("HTTP/1.1 400 Bad Request");
                                    writer.WriteLine("Content-Type: text/plain");
                                    writer.WriteLine();
                                    writer.WriteLine("Please input parameters [player, id]");
                                }
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
                    }
                    
                }
            }
            catch (IOException ex)
            {
                //Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}. Exception: {ex.Message}");
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