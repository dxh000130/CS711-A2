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
    private Socket _listener;
    private IPAddress _IP;
    private int _port;
    private static object _multiLock = new object();
    private volatile Dictionary<string, string> currentPlayer = new Dictionary<string, string>();
    private volatile Dictionary<string, string> currentGameId = new Dictionary<string, string>();
    private string[] _englishWords = new string[] { "apple", "banana", "orange", "kiwi", "mango", "grape", "pear", "pineapple", "watermelon", "strawberry", 
        "lemon", "peach", "plum", "avocado", "blueberry", "coconut", "pomegranate", "cherry", "fig", "apricot", 
        "papaya", "guava", "mangosteen", "dragonfruit", "deer", "lion", "tiger", "giraffe", "elephant", "monkey", 
        "zebra", "kangaroo", "rhinoceros", "crocodile", "hippopotamus", "panda", "koala", "gorilla", "leopard", 
        "cheetah", "hyena", "camel", "gazelle", "buffalo", "penguin", "seagull", "dolphin", "shark", "whale", 
        "jellyfish", "octopus", "seahorse", "starfish", "lobster", "crab", "turtle" };

    private volatile List<Dictionary<string, string>> _pairDictionary = new List<Dictionary<string, string>>();
    private volatile List<string> _usedWords = new List<string>();
    private volatile List<string> _waitForPair = new List<string>();

    public GameServer(IPAddress IP, int port)
    {
        _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _IP = IP;
        _port = port;
    }

    public async Task StartAsync()
    {
        _listener.Bind(new IPEndPoint(_IP, _port));
        _listener.Listen(100);
        Console.WriteLine("Game Server Starting");
        Console.WriteLine("Game Server Started");
        Console.WriteLine($"Listening at {_IP}:{_port} ");

        while (true)
        {
            Socket  client = await _listener.AcceptAsync();
            Task clientTask = Task.Run(() => HandleClientAsync(client));
        }
    }
    private void CloseConnection(object state)
    {
        Socket client = (Socket)state;
        lock (_multiLock)
        {
            Console.WriteLine($"Client disconnected: {client.RemoteEndPoint}");
            Dictionary<string, string> foundDictionary = null;

            if (currentGameId.ContainsKey(client.RemoteEndPoint.ToString()))
            {
                Console.WriteLine($"Game ID:{currentGameId[client.RemoteEndPoint.ToString()]} has been end!");
            }

            if (currentPlayer.ContainsKey(client.RemoteEndPoint.ToString()))
            {
                Console.WriteLine($"Player:{currentPlayer[client.RemoteEndPoint.ToString()]} has been end!");
            }

            if (currentGameId.ContainsKey(client.RemoteEndPoint.ToString()) &&
                currentPlayer.ContainsKey(client.RemoteEndPoint.ToString()))
            {
                foundDictionary = _pairDictionary.Find(d => d.ContainsKey("player1") && (d["player1"] == currentPlayer[client.RemoteEndPoint.ToString()] || d["player2"] == currentPlayer[client.RemoteEndPoint.ToString()]) && d["id"] == currentGameId[client.RemoteEndPoint.ToString()]);
            }
            
            if (foundDictionary != null)
            {
                if (foundDictionary["player1"] == currentPlayer[client.RemoteEndPoint.ToString()])
                {
                    if (foundDictionary["player2"] != "")
                    {
                        _waitForPair.Add(foundDictionary["player1"]);
                        Guid myGuid = Guid.NewGuid();
                        Dictionary<string, string> dic = new Dictionary<string, string>
                        {
                            { "id", myGuid.ToString() }, { "player1", foundDictionary["player1"] }, { "player2", "" },
                            { "state", "wait" }, { "lastMovePlayer1", "" }, { "lastMovePlayer2", "" }
                        };
                        _pairDictionary.Add( dic);
                        currentGameId[client.RemoteEndPoint.ToString()] = myGuid.ToString();
                    }
                    _usedWords.Remove(foundDictionary["player1"]);
                }
                else if (foundDictionary["player2"] == currentPlayer[client.RemoteEndPoint.ToString()])
                {
                    if (foundDictionary["player1"] != "")
                    {
                        _waitForPair.Add(foundDictionary["player1"]);
                        Guid myGuid = Guid.NewGuid();
                        Dictionary<string, string> dic = new Dictionary<string, string>
                        {
                            { "id", myGuid.ToString() }, { "player1", foundDictionary["player1"] }, { "player2", "" },
                            { "state", "wait" }, { "lastMovePlayer1", "" }, { "lastMovePlayer2", "" }
                        };
                        _pairDictionary.Add( dic);
                        currentGameId[client.RemoteEndPoint.ToString()] = myGuid.ToString();
                    }
                    _usedWords.Remove(foundDictionary["player2"]);
                }

                _pairDictionary.Remove(foundDictionary);
                        
            }
            else
            {
                if (currentPlayer.ContainsKey(client.RemoteEndPoint.ToString()))
                {
                    if (_waitForPair.Contains(currentPlayer[client.RemoteEndPoint.ToString()]))
                    {
                        _waitForPair.Remove(currentPlayer[client.RemoteEndPoint.ToString()]);
                    }

                    if (_usedWords.Contains(currentPlayer[client.RemoteEndPoint.ToString()]))
                    {
                        _usedWords.Remove(currentPlayer[client.RemoteEndPoint.ToString()]);
                    }
                }

            }
        }
    }
    private async Task HandleClientAsync(Socket  client)
    {
        using (client)
        {
            try
            {
                Console.WriteLine("Connection established with: " + client.RemoteEndPoint);
                using (NetworkStream stream = new NetworkStream(client))
                {
                    StreamReader reader = new StreamReader(stream);
                    StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
                    //Timer timer = new Timer(CloseConnection, client, TimeSpan.FromSeconds(600), TimeSpan.Zero);

                    while (true)
                    {
                        // writer.WriteLine("Access-Control-Allow-Origin: *");
                        // writer.WriteLine("Access-Control-Allow-Methods: GET, POST, OPTIONS");
                        // writer.WriteLine("Access-Control-Allow-Headers: Content-Type, Accept");
                        
                        // char[] buffer = new char[1024];
                        // int offset = 0;
                        // int count = buffer.Length;
                        // int charsRead = await reader.ReadAsync(buffer, offset, count);
                        // if (charsRead == 0)
                        // {
                        //     CloseConnection(client);
                        //     break;
                        // }
                        // Console.WriteLine(charsRead);
                        // string requestLine = new string(buffer, 0, charsRead);
                        // Console.WriteLine(requestLine);
                        string requestLine = await reader.ReadLineAsync();
                        if (requestLine == null)
                        {
                            CloseConnection(client);
                            break;
                        }
                        // else
                        // {
                        //     //timer.Change(TimeSpan.FromSeconds(300), TimeSpan.Zero);
                        // }
                        
                        int requestStringIndexGet = requestLine.ToUpper().IndexOf("GET");
                        int requestStringIndexPost = requestLine.ToUpper().IndexOf("POST");
                        if (requestStringIndexGet > 0)
                        {
                            requestLine = requestLine.Substring(requestStringIndexGet);
                        }else if (requestStringIndexPost > 0)
                        {
                            requestLine = requestLine.Substring(requestStringIndexPost);
                        }

                        string[] tokens = requestLine.Split(' ');
                        string method = "";
                        string path = "";
                        if (tokens.Length >= 2)
                        {
                            method = tokens[0].ToUpper();
                            path = tokens[1].ToLower();
                        }
                        
                        string queryString = "";
                        int queryStringIndex = path.IndexOf('?');
                        if (queryStringIndex >= 0)
                        {
                            queryString = path.Substring(queryStringIndex + 1);
                            path = path.Substring(0, queryStringIndex);
                        }

                        Dictionary<string, string> parameters = Parse(queryString);

                        // Handle GET requests
                        if (method == "GET")
                        {
                            if (path.StartsWith("/debug"))
                            {
                                DateTime currentTime = DateTime.Now;
                                string re = $"Client: {client.RemoteEndPoint.ToString()} ({currentTime.ToString()})";
                                await sendMessage(200, re, writer, stream);
                            }
                            else if (path.StartsWith("/version"))
                            {
                                await sendMessage(200, "08/05/2023", writer, stream);
                            }
                            else if (path.StartsWith("/favicon.ico"))
                            {
                                try
                                {
                                    byte[] iconBytes = File.ReadAllBytes("../../favicon.ico");
                                    writer.WriteLine("HTTP/1.1 200 OK");
                                    writer.WriteLine("Content-Type: image/x-icon");
                                    writer.WriteLine("Access-Control-Allow-Origin: *");
                                    writer.WriteLine("Access-Control-Allow-Methods: GET, POST, OPTIONS");
                                    writer.WriteLine("Access-Control-Allow-Headers: Content-Type, Accept");
                                    writer.WriteLine($"Content-Length: {iconBytes.Length}");
                                    writer.WriteLine();
                                    await writer.FlushAsync();
                                    await stream.WriteAsync(iconBytes, 0, iconBytes.Length);
                                    
                                }
                                catch (FileNotFoundException)
                                {
                                    await sendMessage(404, "Favicon not found.", writer, stream);
                                }
                            }
                            else if (path.StartsWith("/register"))
                            {
                                
                                Random random = new Random();
                                int randomIndex = random.Next(_englishWords.Length);
                                string randomUsername = _englishWords[randomIndex];
                                lock (_multiLock)
                                {
                                    while (_usedWords.Contains(randomUsername))
                                    {
                                        randomIndex = random.Next(_englishWords.Length);
                                        randomUsername = _englishWords[randomIndex];
                                    }
                                    _usedWords.Add(randomUsername);
                                    currentPlayer[client.RemoteEndPoint.ToString()] = randomUsername;
                                }
                                
                                await sendMessage(200, randomUsername, writer, stream);
                                
                            }
                            else if (path.StartsWith("/pairme"))
                            {
                                if (parameters.ContainsKey("player"))
                                {
                                    string username = parameters["player"];
                                    lock (_multiLock)
                                    {
                                        if (_usedWords.Contains(username))
                                        {

                                            if (_waitForPair.Count == 0 && _pairDictionary.Find(d =>
                                                    (d.ContainsKey("player1") && d["player1"] == username) ||
                                                    (d.ContainsKey("player2") && d["player2"] == username)) == null)
                                            {

                                                Guid myGuid = Guid.NewGuid();
                                                Dictionary<string, string> dic = new Dictionary<string, string>
                                                {
                                                    { "id", myGuid.ToString() }, { "player1", username },
                                                    { "player2", "" },
                                                    { "state", "wait" }, { "lastMovePlayer1", "" },
                                                    { "lastMovePlayer2", "" }
                                                };

                                                _waitForPair.Add(username);
                                                _pairDictionary.Add(dic);
                                                currentGameId[client.RemoteEndPoint.ToString()] = myGuid.ToString();

                                                sendMessage(200, JsonConvert.SerializeObject(dic), writer,
                                                    stream);

                                            }
                                            else if (_waitForPair.Contains(username) || _pairDictionary.Find(d =>
                                                         (d.ContainsKey("player1") && d["player1"] == username) ||
                                                         (d.ContainsKey("player2") && d["player2"] == username)) !=
                                                     null)
                                            {
                                                Dictionary<string, string> foundDictionary = null;
                                                lock (_multiLock)
                                                {
                                                    foundDictionary = _pairDictionary.Find(d =>
                                                        (d.ContainsKey("player1") && d["player1"] == username) ||
                                                        (d.ContainsKey("player2") && d["player2"] == username));

                                                }

                                                sendMessage(200, JsonConvert.SerializeObject(foundDictionary),
                                                    writer, stream);
                                            }
                                            else
                                            {
                                                Dictionary<string, string> foundDictionary = null;
                                                lock (_multiLock)
                                                {
                                                    string player1 = _waitForPair[0];
                                                    foundDictionary = _pairDictionary.Find(d =>
                                                        d.ContainsKey("player1") && d["player1"] == player1);
                                                    if (foundDictionary["player2"] == "")
                                                    {
                                                        _waitForPair.RemoveAt(0);
                                                        if (foundDictionary != null)
                                                        {
                                                            foundDictionary["player2"] = username;
                                                            foundDictionary["state"] = "progress";
                                                        }
                                                    }

                                                    currentGameId[client.RemoteEndPoint.ToString()] =
                                                        foundDictionary["id"];
                                                }

                                                sendMessage(200, JsonConvert.SerializeObject(foundDictionary),
                                                    writer, stream);
                                            }
                                        }
                                        else
                                        {
                                            sendMessage(400, "Wrong username.", writer, stream);
                                        }
                                    }
                                }
                                else
                                {
                                    await sendMessage(400, "Please input parameters [player].", writer, stream);
                                }
                                
                            }
                            else if (path.StartsWith("/mymove"))
                            {
                                //Console.WriteLine(JsonConvert.SerializeObject(parameters));
                                if (parameters.ContainsKey("player") && parameters.ContainsKey("id") && parameters.ContainsKey("move"))
                                {
                                    string username = parameters["player"];
                                    string id = parameters["id"];
                                    string move = parameters["move"];
                                    Dictionary<string, string> foundDictionary;
                                    lock (_multiLock)
                                    {
                                        foundDictionary = _pairDictionary.Find(d =>
                                            d.ContainsKey("player1") &&
                                            (d["player1"] == username || d["player2"] == username) && d["id"] == id);
                                    

                                        if (foundDictionary != null)
                                        {
                                            if (foundDictionary["state"] == "progress")
                                            {
                                                
                                                Console.WriteLine(move);
                                                if (foundDictionary["player1"] == username)
                                                {
                                                    foundDictionary["lastMovePlayer1"] = move;
                                                }
                                                else if (foundDictionary["player2"] == username)
                                                {
                                                    foundDictionary["lastMovePlayer2"] = move;
                                                }
                                                
                                                sendMessage(200, "OK!", writer, stream);
                                                
                                            }
                                            else
                                            {
                                                sendMessage(400, "Game not start!", writer, stream);
                                            }
                                        }
                                        else
                                        {
                                            sendMessage(404, "Wrong username or ID", writer, stream);
                                        }
                                    }
                                }
                                else
                                {
                                    await sendMessage(400, "Please input parameters [player, id, move].", writer, stream);
                                }
                            }
                            else if (path.StartsWith("/theirmove"))
                            {
                                if (parameters.ContainsKey("player") && parameters.ContainsKey("id"))
                                {
                                    
                                    string username = parameters["player"];
                                    string id = parameters["id"];
                                    string move="";
                                    Dictionary<string, string> foundDictionary;
                                    lock (_multiLock)
                                    {
                                        foundDictionary = _pairDictionary.Find(d =>
                                            d.ContainsKey("player1") &&
                                            (d["player1"] == username || d["player2"] == username) && d["id"] == id);
                                    
                                        if (foundDictionary != null)
                                        {
                                            if (foundDictionary["state"] == "progress")
                                            {
                                                
                                                if (foundDictionary["player1"] == username)
                                                {
                                                    move = foundDictionary["lastMovePlayer2"];
                                                }else if(foundDictionary["player2"] == username)
                                                {
                                                    move = foundDictionary["lastMovePlayer1"];
                                                }
                                                    
                                                
                                                sendMessage(200, move, writer, stream);
                                            }
                                            else
                                            {
                                                sendMessage(400, "Game not start!", writer, stream);
                                            }
                                        }
                                        else
                                        {
                                            sendMessage(404, "Wrong username or ID", writer, stream);
                                        }
                                    }
                                }
                                else
                                {
                                    await sendMessage(400, "Please input parameters [player, id]", writer, stream);
                                }

                            }
                            else if (path.StartsWith("/quit"))
                            {
                                if (parameters.ContainsKey("player") && parameters.ContainsKey("id"))
                                {
                                    string username = parameters["player"];
                                    string id = parameters["id"];
                                    Dictionary<string, string> foundDictionary;
                                    lock (_multiLock)
                                    {
                                        foundDictionary = _pairDictionary.Find(d =>
                                            d.ContainsKey("player1") &&
                                            (d["player1"] == username || d["player2"] == username) && d["id"] == id);

                                        if (foundDictionary != null)
                                        {

                                            if (foundDictionary["player1"] == username)
                                            {
                                                if (foundDictionary["player2"] != "")
                                                {
                                                    _waitForPair.Add(foundDictionary["player2"]);
                                                    Guid myGuid = Guid.NewGuid();
                                                    Dictionary<string, string> dic = new Dictionary<string, string>
                                                    {
                                                        { "id", myGuid.ToString() },
                                                        { "player1", foundDictionary["player2"] }, { "player2", "" },
                                                        { "state", "wait" }, { "lastMovePlayer1", "" },
                                                        { "lastMovePlayer2", "" }
                                                    };
                                                    _pairDictionary.Add(dic);
                                                    currentGameId[client.RemoteEndPoint.ToString()] = myGuid.ToString();
                                                }

                                                _usedWords.Remove(foundDictionary["player1"]);
                                            }
                                            else if (foundDictionary["player2"] == username)
                                            {
                                                if (foundDictionary["player1"] != "")
                                                {
                                                    _waitForPair.Add(foundDictionary["player1"]);
                                                    Guid myGuid = Guid.NewGuid();
                                                    Dictionary<string, string> dic = new Dictionary<string, string>
                                                    {
                                                        { "id", myGuid.ToString() },
                                                        { "player1", foundDictionary["player1"] }, { "player2", "" },
                                                        { "state", "wait" }, { "lastMovePlayer1", "" },
                                                        { "lastMovePlayer2", "" }
                                                    };
                                                    _pairDictionary.Add(dic);
                                                    currentGameId[client.RemoteEndPoint.ToString()] = myGuid.ToString();
                                                }

                                                _usedWords.Remove(foundDictionary["player2"]);
                                            }

                                            _pairDictionary.Remove(foundDictionary);

                                            Console.WriteLine($"Client disconnected: {client.RemoteEndPoint}");
                                            Console.WriteLine($"Game ID:{id} has been end!");
                                            Console.WriteLine($"Player:{username} has been end!");
                                            sendMessage(200, $"Game ID:{id} has been end!" +
                                                                   $"Player:{username} has been end!", writer, stream);

                                            break;
                                        }
                                        else
                                        {
                                            sendMessage(404, "Wrong username or ID!", writer, stream);
                                        }
                                    }
                                }
                                else
                                {
                                    await sendMessage(400, "Please input parameters [player, id]", writer, stream);
                                }
                            }
                            else
                            {
                                await sendMessage(400, "Invalid request.", writer, stream);
                            }
                            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} sent response to {client.RemoteEndPoint} for {path}");
                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
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
            int equalSignIndex = pair.IndexOf('=');
            if (equalSignIndex < 0)
            {
                continue; // Skip invalid pair
            }

            string key = Uri.UnescapeDataString(pair.Substring(0, equalSignIndex));
            string value = Uri.UnescapeDataString(pair.Substring(equalSignIndex + 1));

            parameters[key] = value;
        }

        return parameters;
    }


    public async Task<int> sendMessage(int code, string state, StreamWriter writer, NetworkStream stream)
    {
        byte[] responseBodyBytes = Encoding.UTF8.GetBytes(state);
        if (code == 200)
        {
            writer.WriteLine("HTTP/1.1 200 Ok");
            writer.WriteLine("Access-Control-Allow-Origin: *");
            writer.WriteLine("Access-Control-Allow-Methods: GET, POST, OPTIONS");
            writer.WriteLine("Access-Control-Allow-Headers: Content-Type, Accept");
            writer.WriteLine("Content-Type: text/plain; charset=utf-8");
            writer.WriteLine("Content-Length: " + responseBodyBytes.Length);
            writer.WriteLine();
            await writer.FlushAsync();

            await stream.WriteAsync(responseBodyBytes, 0, responseBodyBytes.Length);
        }else if (code == 400)
        {
            writer.WriteLine("HTTP/1.1 400 Bad Request");
            writer.WriteLine("Access-Control-Allow-Origin: *");
            writer.WriteLine("Access-Control-Allow-Methods: GET, POST, OPTIONS");
            writer.WriteLine("Access-Control-Allow-Headers: Content-Type, Accept");
            writer.WriteLine("Content-Type: text/plain; charset=utf-8");
            writer.WriteLine("Content-Length: " + responseBodyBytes.Length);
            writer.WriteLine();
            await writer.FlushAsync();

            await stream.WriteAsync(responseBodyBytes, 0, responseBodyBytes.Length);
        }else if (code == 404)
        {
            
            writer.WriteLine("HTTP/1.1 404 Not Found");
            writer.WriteLine("Access-Control-Allow-Origin: *");
            writer.WriteLine("Access-Control-Allow-Methods: GET, POST, OPTIONS");
            writer.WriteLine("Access-Control-Allow-Headers: Content-Type, Accept");
            writer.WriteLine("Content-Type: text/plain; charset=utf-8");
            writer.WriteLine("Content-Length: " + responseBodyBytes.Length);
            writer.WriteLine();
            await writer.FlushAsync();

            await stream.WriteAsync(responseBodyBytes, 0, responseBodyBytes.Length); 
        }
       

        
        return 1;
    }
}

}