using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Timers;

namespace GameOfChipsServer;

public static class Server
{
    private static bool IsRunning { get; set; }
    
    private static Thread? MainThread { get; set; }
    
    private static int Port { get; set; }
    
    private static TcpListener? TcpListener { get; set; }

    private static Dictionary<int, Client> Clients { get; } = new();

    private static Queue<int> FreeIds { get; } = new();

    public const int BufferSize = 4096;

    private const int TicksPerSecond = 30;

    private const int MsPerTick = 1000;

    private const int MaxPlayers = 16;

    public const string Checksum = "67edb3d15f778b696f5878de79d1583f";

    public delegate void PacketHandler(int clientId, Packet packet);

    public static Dictionary<int, PacketHandler>? PacketHandlers { get; private set; }

    public static void Run(int port)
    {
        InitializeFreeIds();

        MainThread = new Thread(MainThreadStart);

        Port = port;

        TcpListener = new TcpListener(IPAddress.Any, Port);
        TcpListener.Start();

        TcpListener.BeginAcceptTcpClient(OnTcpClientConnect, null);
        
        Message($"Server started on {IPAddress.Any}:{Port}");

        PacketHandlers = new Dictionary<int, PacketHandler>
        {
            [(int)ClientPacketType.Welcome] = ServerHandle.WelcomeReceived,
            [(int)ClientPacketType.GetListOfPlayersGlobal] = ServerHandle.GetListOfPlayersGlobal,
            [(int)ClientPacketType.Disconnect] = ServerHandle.Disconnect,
        };

        IsRunning = true;
        
        MainThread.Start();
    }

    private static void MainThreadStart()
    {
        Message($"Main thread started. Running at {TicksPerSecond} ticks per second.");

        var nextLoop = DateTime.Now;

        while (IsRunning)
        {
            while (nextLoop < DateTime.Now)
            {
                ThreadManager.UpdateMain();

                nextLoop = nextLoop.AddMilliseconds(MsPerTick);
                
                if (nextLoop > DateTime.Now)
                    Thread.Sleep(nextLoop - DateTime.Now);
            }
        }
    }

    private static void OnTcpClientConnect(IAsyncResult result)
    {
        var tcpClient = TcpListener!.EndAcceptTcpClient(result);
        
        TcpListener.BeginAcceptTcpClient(OnTcpClientConnect, null);
        
        Message($"Incoming connection from {tcpClient.Client.RemoteEndPoint}..");

        var freeId = GetFreeId();
        if (freeId == -1)
        {
            Message($"{tcpClient.Client.RemoteEndPoint} failed to connect. Server is full!");
            
            tcpClient.Client.Disconnect(false);
            
            return;
        }
        
        var newClient = new Client();
        
        Clients.Add(freeId, newClient);
        
        newClient.Connect(freeId, tcpClient);
    }

    public static Client GetClient(int clientId)
    {
        return Clients[clientId];
    }

    public static bool IsClientExists(int clientId)
    {
        return Clients.ContainsKey(clientId);
    }

    public static void RemoveClient(int clientId)
    {
        Clients.Remove(clientId);
    }

    public static List<int> GetAllClientsIdsExceptTheCaller(int callerId)
    {
        return Clients.Keys.Where(id => id != callerId).ToList();
    }

    public static void FreeUpId(int id)
    {
        FreeIds.Enqueue(id);
    }
    
    private static int GetFreeId()
    {
        if (Clients.Count == MaxPlayers)
            return -1;

        if (FreeIds.Count == 0)
            return -1;

        return FreeIds.Dequeue();
    }
    
    private static void InitializeFreeIds()
    {
        for (var i = 1; i <= MaxPlayers; i++)
            FreeIds.Enqueue(i);
    }

    public static void Message(string message)
    {
        Console.WriteLine($"[{DateTime.Now}] {message}");
    }
}