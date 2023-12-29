namespace GameOfChipsServer;

public static class ServerHandle
{
    public static void WelcomeReceived(int clientId, Packet packet)
    {
        var clientIdFromPacket = packet.ReadInt();
        var clientUsername = packet.ReadString();
        var checksum = packet.ReadString();

        var client = Server.GetClient(clientId);

        client.Username = clientUsername;
        
        Server.Message($"{client.GetIp()}[{clientId}/{clientUsername}] successfully connected to the server!");
        
        if (clientIdFromPacket != clientId)
            Server.Message($"{client.GetIp()}[{clientId}] has assumed the wrong client Id {clientIdFromPacket}");

        if (string.Equals(checksum, Server.Checksum)) 
            return;
        
        Server.Message($"{client.GetIp()}[{clientId}] tried to connect with another version and was kicked.");
        
        client.Disconnect();
    }

    public static void GetListOfPlayersGlobal(int clientId, Packet packet)
    {
        var client = Server.GetClient(clientId);

        Server.Message($"{client.GetIp()}[{clientId}] requested a global list of players.");
        
        var ids = Server.GetAllClientsIdsExceptTheCaller(clientId);
        
        ServerSend.SendListOfPlayersGlobal(clientId, ids);
    }

    public static void Disconnect(int clientId, Packet packet)
    {
        if (!Server.IsClientExists(clientId))
            return;
        
        var client = Server.GetClient(clientId);
        
        client.Disconnect();
    }
}