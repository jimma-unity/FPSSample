using System;
using Unity.Networking.Transport;
using Unity.Collections;
using EventType = Unity.Networking.Transport.NetworkEvent.Type;

// JAPA - to remove
public static class NetworkConnectionExtensions
{
    public static int GetInternalId(this NetworkConnection conn)
    {
        // Get the type
        var type = typeof(NetworkConnection);

        // Get the property info for InternalId (it's a property, not a field)
        var prop = type.GetProperty("InternalId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        if (prop == null)
            throw new InvalidOperationException("Property 'InternalId' not found.");

        // Get the value
        return (int)prop.GetValue(conn);
    }
}

public class SocketTransport : INetworkTransport
{
    public SocketTransport(int port = 0, int maxConnections = 16)
    {
        m_IdToConnection = new NativeArray<NetworkConnection>(maxConnections, Allocator.Persistent);
        var settings = new NetworkSettings();
        settings.WithNetworkConfigParameters(disconnectTimeoutMS: ServerGameLoop.serverDisconnectTimeout.IntValue/*, receiveQueueCapacity: 10 * NetworkConfig.maxPackageSize, sendQueueCapacity: 10 * NetworkConfig.maxPackageSize*/);
        m_Socket = NetworkDriver.Create(settings);
        var endpoint = NetworkEndpoint.AnyIpv4.WithPort((ushort)port);
        m_Socket.Bind(endpoint);

        if (port != 0)
            m_Socket.Listen();
    }

    public int Connect(string ip, int port)
    {
        var connection = m_Socket.Connect(NetworkEndpoint.Parse(ip, (ushort)port));
        m_IdToConnection[connection.GetInternalId()] = connection;
        return connection.GetInternalId();
    }

    public void Disconnect(int connection)
    {
        m_Socket.Disconnect(m_IdToConnection[connection]);
        m_IdToConnection[connection] = default(NetworkConnection);
    }

    public void Update()
    {
        m_Socket.ScheduleUpdate().Complete();
    }

    public bool NextEvent(ref TransportEvent e)
    {
        NetworkConnection connection;

        connection = m_Socket.Accept();
        if (connection.IsCreated)
        {
            e.type = TransportEvent.Type.Connect;
            e.connectionId = connection.GetInternalId();
            m_IdToConnection[connection.GetInternalId()] = connection;
            return true;
        }

        DataStreamReader reader;
        var ev = m_Socket.PopEvent(out connection, out reader);

        if (ev == EventType.Empty)
            return false;

        int size = 0;
        if (reader.IsCreated)
        {
            GameDebug.Assert(m_Buffer.Length >= reader.Length);
            reader.ReadBytes(new Span<byte>(m_Buffer, 0, reader.Length));
            size = reader.Length;
        }
        
        switch (ev)
        {
            case EventType.Data:
                e.type = TransportEvent.Type.Data;
                e.data = m_Buffer;
                e.dataSize = size;
                e.connectionId = connection.GetInternalId();
                break;
            case EventType.Connect:
                e.type = TransportEvent.Type.Connect;
                e.connectionId = connection.GetInternalId();
                m_IdToConnection[connection.GetInternalId()] = connection;
                break;
            case EventType.Disconnect:
                e.type = TransportEvent.Type.Disconnect;
                e.connectionId = connection.GetInternalId();
                break;
            default:
                return false;
        }

        return true;
    }

    public void SendData(int connectionId, byte[] data, int sendSize)
    {
        DataStreamWriter sendStream;
        m_Socket.BeginSend(m_IdToConnection[connectionId], out sendStream, sendSize);
        sendStream.WriteBytes(new Span<byte>(data, 0, sendSize));
        m_Socket.EndSend(sendStream);
    }

    public string GetConnectionDescription(int connectionId)
    {
        return ""; // TODO enable this once RemoteEndPoint is implemented m_Socket.RemoteEndPoint(m_IdToConnection[connectionId]).GetIp();
    }

    public void Shutdown()
    {
        m_Socket.Dispose();
        m_IdToConnection.Dispose();
    }

    byte[] m_Buffer = new byte[1024 * 8];
    NetworkDriver m_Socket;
    NativeArray<NetworkConnection> m_IdToConnection;
}
