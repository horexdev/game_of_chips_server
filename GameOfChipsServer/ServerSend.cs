namespace GameOfChipsServer;

public static class ServerSend
{
    public static void Welcome(int clientId)
    {
        using var packet = new Packet((int)ServerPacketType.Welcome);
        
        packet.Write(clientId);
        
        Send(clientId, packet);
    }

    public static void SendListOfPlayersGlobal(int clientId, List<int> ids)
    {
        using var packet = new Packet((int)ServerPacketType.SendListOfPlayersGlobal);

        if (ids.Count == 0)
            return;
        
        packet.Write(ids.Count);
        foreach (var id in ids)
        {
            var client = Server.GetClient(id);

            packet.Write(id);
            packet.Write(client.Username);
        }
        
        Send(clientId, packet);
    }

    private static void Send(int clientId, Packet packet)
    {
        packet.WriteLength();
        
        Server.GetClient(clientId).SendData(packet);
    }

    private static void SendToAll(Packet packet)
    {
        packet.WriteLength();

        for (var i = 1; i <= 2; i++)
            Server.GetClient(i).SendData(packet);
    }
    
    private static void SendToAll(int exceptClient, Packet packet)
    {
        packet.WriteLength();

        for (var i = 1; i <= 2; i++)
            if (i != exceptClient)
                Server.GetClient(i).SendData(packet);
    }
}