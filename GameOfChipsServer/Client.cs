using System.Net.Sockets;

namespace GameOfChipsServer;

public class Client
{
    private int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Ip { get; private set; } = null!;
    
    public bool Connected { get; private set; }

    private NetworkStream? _networkStream;

    private byte[]? _recieveBuffer;

    private TcpClient? _tcpClient;

    private Packet? _receivedData;

    public string? GetIp()
    {
        return _tcpClient!.Client.RemoteEndPoint?.ToString();
    }
    
    public void SendData(Packet packet)
    {
        try
        {
            if (_tcpClient == null)
                return;
            
            _networkStream!.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
        }
        catch (Exception e)
        {
            Server.Message($"({_tcpClient!.Client.RemoteEndPoint}) {e.Message}");
        }
    }
    
    public void Connect(int id, TcpClient tcpClient)
    {
        Id = id;
        
        _tcpClient = tcpClient;
        
        _tcpClient.ReceiveBufferSize = Server.BufferSize;
        _tcpClient.SendBufferSize = Server.BufferSize;

        _networkStream = _tcpClient.GetStream();

        _receivedData = new Packet();
        _recieveBuffer = new byte[Server.BufferSize];

        _networkStream.BeginRead(_recieveBuffer, 0, Server.BufferSize, Callback, null);

        Connected = true;

        Ip = _tcpClient!.Client.RemoteEndPoint!.ToString()!;
        
        ServerSend.Welcome(Id);
    }

    public void Disconnect()
    {
        if (!Connected)
            return;
        
        Server.Message($"{Ip}[{Id}] disconnected from the server.");
        
        _tcpClient!.Client.Disconnect(false);
        Connected = false;

        Server.FreeUpId(Id);
        Server.RemoveClient(Id);
    }

    private void Callback(IAsyncResult asyncResult)
    {
        try
        {
            var byteLength = _networkStream!.EndRead(asyncResult);
            if (byteLength <= 0)
            {
                Disconnect();
                
                return;
            }

            var data = new byte[byteLength];
            
            Array.Copy(_recieveBuffer!, data, byteLength);
            
            _receivedData!.Reset(HandleData(data));

            _networkStream!.BeginRead(_recieveBuffer!, 0, Server.BufferSize, Callback, null);
        }
        catch (Exception e)
        {
            Server.Message(e.Message);
            
            Disconnect();
        }
    }
    
    private bool HandleData(byte[] data)
    {
        var packetLength = 0;

        _receivedData!.SetBytes(data);

        if (_receivedData.UnreadLength() >= 4)
        {
            packetLength = _receivedData.ReadInt();
            if (packetLength <= 0)
                return true;
        }

        while (packetLength > 0 && packetLength <= _receivedData.UnreadLength())
        {
            var packetBytes = _receivedData.ReadBytes(packetLength);
            
            ThreadManager.ExecuteOnMainThread(() =>
            {
                using var packet = new Packet(packetBytes);

                var packetId = packet.ReadInt();
                
                Server.PacketHandlers![packetId](Id, packet);
            });

            packetLength = 0;
            if (_receivedData.UnreadLength() < 4) 
                continue;
                
            packetLength = _receivedData.ReadInt();
            if (packetLength <= 0)
                return true;
        }
        
        return packetLength <= 1;
    }
}