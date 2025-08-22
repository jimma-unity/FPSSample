using System;
using System.Threading;

using UnityEngine.TestTools;
using NUnit.Framework;

using System.Net.Sockets;

using Unity.Networking.Transport;
using Unity.Collections;
using EventType = Unity.Networking.Transport.NetworkEvent.Type;
using UnityEngine;

namespace TransportTests
{
    public class SocketTests
    {
        [Test]
        public void UdpC_BindToEndpoint_ReturnSocketHandle()
        {
            using (var socket = NetworkDriver.Create())
            {
                var endpoint = NetworkEndpoint.AnyIpv4;
                var socketError = socket.Bind(endpoint);

                Assert.AreEqual(socketError, (int)SocketError.Success);
            }
        }

        [Test]
        public void UdpC_BindMultipleToSameEndpoint_ReturnSocketError()
        {
            using(var first = NetworkDriver.Create())
            using(var second = NetworkDriver.Create())
            {
                var endpoint = NetworkEndpoint.AnyIpv4.WithPort(50001);

                Assert.Zero(first.Bind(endpoint));

                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"(?i)(?=.*address)(?=.*in)(?=.*use)"));
                
                Assert.NotZero(second.Bind(endpoint));
            }
        }

        [Test]
        public void UdpC_ListenThenConnect_ShouldFail()
        {
            using (var socket = NetworkDriver.Create())
            {
                var endpoint = NetworkEndpoint.AnyIpv4.WithPort(50007);
                socket.Bind(endpoint);

                socket.Listen();

                var endPoint = socket.Connect(endpoint);
                socket.ScheduleUpdate().Complete();
                Assert.AreNotEqual(NetworkConnection.State.Connected, endPoint.GetState(socket));
            }
        }

        [Test]
        public void UdpC_ConnectTest_ShouldConnect()
        {
            using (var server = NetworkDriver.Create())
            using (var client = NetworkDriver.Create())
            {
                ushort serverPort = 50009;

                server.Bind(NetworkEndpoint.LoopbackIpv4.WithPort(serverPort));
                client.Bind(NetworkEndpoint.LoopbackIpv4);

                server.Listen();

                var id = client.Connect(NetworkEndpoint.LoopbackIpv4.WithPort(serverPort));

                NetworkConnection serverConnection, clientConnection;
                int maxIterations = 100;

                ConnectTogether(server, client, maxIterations, out serverConnection, out clientConnection);
                Assert.AreEqual(id, serverConnection);
            }
        }

        [Test]
        public void UdpC_MultipleConnectTest_ShouldConnect()
        {
            using (var server = NetworkDriver.Create())
            using (var client0 = NetworkDriver.Create())
            using (var client1 = NetworkDriver.Create())
            using (var client2 = NetworkDriver.Create())
            using (var client3 = NetworkDriver.Create())
            {
                ushort serverPort = 50005;

                server.Bind(NetworkEndpoint.LoopbackIpv4.WithPort(serverPort));
                client0.Bind(NetworkEndpoint.LoopbackIpv4);
                client1.Bind(NetworkEndpoint.LoopbackIpv4);
                client2.Bind(NetworkEndpoint.LoopbackIpv4);
                client3.Bind(NetworkEndpoint.LoopbackIpv4);

                server.Listen();

                NetworkConnection serverConnection, clientConnection;
                int maxIterations = 100;


                var id = client0.Connect(NetworkEndpoint.LoopbackIpv4.WithPort(serverPort));
                ConnectTogether(server, client0, maxIterations, out serverConnection, out clientConnection);
                Assert.AreEqual(id, serverConnection);

                id = client1.Connect(NetworkEndpoint.LoopbackIpv4.WithPort(serverPort));
                ConnectTogether(server, client1, maxIterations, out serverConnection, out clientConnection);
                Assert.AreEqual(id, serverConnection);

                id = client2.Connect(NetworkEndpoint.LoopbackIpv4.WithPort(serverPort));
                ConnectTogether(server, client2, maxIterations, out serverConnection, out clientConnection);
                Assert.AreEqual(id, serverConnection);

                id = client3.Connect(NetworkEndpoint.LoopbackIpv4.WithPort(serverPort));
                ConnectTogether(server, client3, maxIterations, out serverConnection, out clientConnection);
                Assert.AreEqual(id, serverConnection);
            }
        }

        [Test]
        public void UdpC_ConnectSendTest_ShouldConnectAndReceiveData()
        {
            using (var server = NetworkDriver.Create())
            using (var client = NetworkDriver.Create())
            {
                ushort serverPort = 51008;

                server.Bind(NetworkEndpoint.LoopbackIpv4.WithPort(serverPort));
                client.Bind(NetworkEndpoint.LoopbackIpv4);

                server.Listen();

                client.Connect(NetworkEndpoint.LoopbackIpv4.WithPort(serverPort));

                NetworkConnection serverConnection, clientConnection;
                int maxIterations = 100;

                ConnectTogether(server, client, maxIterations, out serverConnection, out clientConnection);

                var message = new byte[]
                {
                    (byte) 'm',
                    (byte) 'e',
                    (byte) 's',
                    (byte) 's',
                    (byte) 'a',
                    (byte) 'g',
                    (byte) 'e'
                };

                SendReceive(client, server, clientConnection, serverConnection, message, maxIterations);
            }
        }

        [Test]
        public void UdpC_ReconnectAndResend_ShouldReconnectAndResend()
        {
            using (var server = NetworkDriver.Create())
            using (var client = NetworkDriver.Create())
            {
                ushort serverPort = 50007;

                server.Bind(NetworkEndpoint.LoopbackIpv4.WithPort(serverPort));
                client.Bind(NetworkEndpoint.LoopbackIpv4);

                server.Listen();

                NetworkConnection serverConnection, clientConnection;
                int maxIterations = 100;

                var id = client.Connect(NetworkEndpoint.LoopbackIpv4.WithPort(serverPort));
                ConnectTogether(server, client, maxIterations, out serverConnection, out clientConnection);

                client.Disconnect(id);

                client.ScheduleUpdate().Complete(); // If we don't do this the disconnection won't actually start and instead will timneout (default is 30seconds)
                
                var data = new byte[1472];
                var size = 1472;
                NetworkConnection from;

                Assert.AreEqual(EventType.Disconnect, PollEvent(EventType.Disconnect, maxIterations, server, ref data, out size, out from));

                id = client.Connect(NetworkEndpoint.LoopbackIpv4.WithPort(serverPort));
                ConnectTogether(server, client, maxIterations, out serverConnection, out clientConnection);

                var message = new byte[]
                {
                    (byte) 'm',
                    (byte) 'e',
                    (byte) 's',
                    (byte) 's',
                    (byte) 'a',
                    (byte) 'g',
                    (byte) 'e'
                };

                SendReceive(client, server, clientConnection, serverConnection, message, maxIterations);
            }
        }

        [Test]
        public void UdpC_Timeout_ShouldDisconnect()
        {
            int customTimeout = 1000;
            
            var settings = new NetworkSettings();
            settings.WithNetworkConfigParameters(disconnectTimeoutMS: customTimeout);

            using (var server = NetworkDriver.Create(settings))
            using (var client = NetworkDriver.Create(settings))
            {

                ushort serverPort = 50006;

                server.Bind(NetworkEndpoint.LoopbackIpv4.WithPort(serverPort));
                client.Bind(NetworkEndpoint.LoopbackIpv4);
                
                server.Listen();

                var id = client.Connect(NetworkEndpoint.LoopbackIpv4.WithPort(serverPort));

                NetworkConnection serverConnection, clientConnection;
                int maxIterations = 100;

                ConnectTogether(server, client, maxIterations, out serverConnection, out clientConnection);
                Assert.AreEqual(id, serverConnection);

                // Force timeout
                Thread.Sleep(customTimeout + 500);
                
                //var message = new DataStreamWriter(7, Allocator.Persistent);
                DataStreamWriter message;
                server.BeginSend(clientConnection, out message);
                message.WriteByte((byte) 'm');
                message.WriteByte((byte) 'e');
                message.WriteByte((byte) 's');
                message.WriteByte((byte) 's');
                message.WriteByte((byte) 'a');
                message.WriteByte((byte) 'g');
                message.WriteByte((byte) 'e');
                server.EndSend(message);

                var data = new byte[1472];
                int size = -1;
                NetworkConnection from;
                Assert.AreEqual(EventType.Disconnect, PollEvent(EventType.Disconnect, maxIterations, server, ref data, out size, out from));
                Assert.AreEqual(from, clientConnection);
            }
        }

        EventType PollEvent(EventType ev, int maxIterations, NetworkDriver socket, ref byte[] buffer, out int size, out NetworkConnection connection)
        {
            int iterator = 0;
            size = 0;
            connection = default(NetworkConnection);
            
            while (iterator++ < maxIterations)
            {
                DataStreamReader reader;
                EventType e;
                if ((e = socket.PopEvent(out connection, out reader)) == ev)
                {
                    if (reader.IsCreated)
                    {
                        reader.ReadBytes(new Span<byte>(buffer, 0, reader.Length));
                        size = reader.Length;
                    }
                    return e;
                }
                socket.ScheduleUpdate().Complete();
            }
            return EventType.Empty;
        }

        void SendReceive(NetworkDriver sender, NetworkDriver receiver, NetworkConnection from, NetworkConnection to, byte[] data, int maxIterations)
        {
            DataStreamWriter writer;
            sender.BeginSend(to, out writer);
            writer.WriteBytes(data);
            sender.EndSend(writer);
            
            sender.ScheduleUpdate().Complete();
            receiver.ScheduleUpdate().Complete();
            
            var buffer = new byte[1472];
            int size = 0;
            NetworkConnection connection;
            PollEvent(EventType.Data, maxIterations, receiver, ref buffer, out size, out connection);
            
            Assert.AreEqual(from, connection);
            Assert.AreEqual(data.Length, size);
            
            for (int i = 0; i < data.Length; i++)
                Assert.AreEqual(data[i], buffer[i]);
        }

        void ConnectTogether(NetworkDriver server, NetworkDriver client, int maxIterations, out NetworkConnection serverConnection, out NetworkConnection clientConnection)
        {
            int servers = 0, clients = 0, iterations = 0;
            serverConnection = default(NetworkConnection);
            clientConnection = default(NetworkConnection);

            DataStreamReader reader;

            NetworkConnection poppedConnection = default(NetworkConnection);
            while (clients != 1 || servers != 1)
            {
                Assert.Less(iterations++, maxIterations);

                server.ScheduleUpdate().Complete();

                var newConnection = server.Accept();
                if (newConnection != default(NetworkConnection))
                {
                    clients++;
                    clientConnection = newConnection;
                }

                if (client.PopEvent(out poppedConnection, out reader) == EventType.Connect)
                {
                    serverConnection = poppedConnection;
                    servers++;
                }

                client.ScheduleUpdate().Complete();
            }
            Assert.AreNotEqual(serverConnection, default(NetworkConnection));
            Assert.AreNotEqual(clientConnection, default(NetworkConnection));
        }

        [Test]
        public void UdpC_LongGoingTest()
        {
            using (UdpCClient server = new UdpCClient(12000))
            using (UdpCClient c0     = new UdpCClient(12001, 12000))
            using (UdpCClient c1     = new UdpCClient(12002, 12000))
            using (UdpCClient c2     = new UdpCClient(12003, 12000))
            using (UdpCClient c3     = new UdpCClient(12004, 12000))
            using (UdpCClient c4     = new UdpCClient(12005, 12000))
            using (UdpCClient c5     = new UdpCClient(12006, 12000))
            {
                long start = 0, now = 0;
                start = NetworkUtils.stopwatch.ElapsedMilliseconds;

                while (now - start < 30000)
                {
                    server.Update();
                    c0.Update();
                    c1.Update();
                    c2.Update();
                    c3.Update();
                    c4.Update();
                    c5.Update();
                    now = NetworkUtils.stopwatch.ElapsedMilliseconds;
                }

                GameDebug.Log(string.Format("con: {0}, disc {1}, data {2}", server.connectCounter,
                    server.disconnectCounter, server.dataCounter));
                GameDebug.Log(string.Format("con: {0}, disc {1}, data {2}", c0.connectCounter, c0.disconnectCounter,
                    c0.dataCounter));
                GameDebug.Log(string.Format("con: {0}, disc {1}, data {2}", c1.connectCounter, c1.disconnectCounter,
                    c1.dataCounter));
                GameDebug.Log(string.Format("con: {0}, disc {1}, data {2}", c2.connectCounter, c2.disconnectCounter,
                    c2.dataCounter));
                GameDebug.Log(string.Format("con: {0}, disc {1}, data {2}", c3.connectCounter, c3.disconnectCounter,
                    c3.dataCounter));
                GameDebug.Log(string.Format("con: {0}, disc {1}, data {2}", c4.connectCounter, c4.disconnectCounter,
                    c4.dataCounter));
                GameDebug.Log(string.Format("con: {0}, disc {1}, data {2}", c5.connectCounter, c5.disconnectCounter,
                    c5.dataCounter));
            }
        }
    }

    public class UdpCClient : IDisposable
    {
        NetworkDriver m_Socket;

        NetworkConnection conn = default(NetworkConnection);
        int serverPort;

        public int connectCounter;
        public int disconnectCounter;
        public int dataCounter;

        public UdpCClient(int port, int serverPort = -1)
        {
            m_Socket = NetworkDriver.Create();
            m_Socket.Bind(NetworkEndpoint.LoopbackIpv4.WithPort((ushort)port));
            if (serverPort == -1)
                m_Socket.Listen();

            this.serverPort = serverPort;
        }

        public void Update()
        {
            if (!m_Socket.Listening && !conn.IsCreated)
            {
                conn = m_Socket.Connect(NetworkEndpoint.LoopbackIpv4.WithPort((ushort)serverPort));
            }
            else if (!m_Socket.Listening && dataCounter == 0 && !conn.IsCreated)
            {
                m_Socket.BeginSend(conn, out var message);
                {
                    message.WriteByte((byte)'m');
                    message.WriteByte((byte)'e');
                    message.WriteByte((byte)'s');
                    message.WriteByte((byte)'s');
                    message.WriteByte((byte)'a');
                    message.WriteByte((byte)'g');
                    message.WriteByte((byte)'e');
                }
                m_Socket.EndSend(message);
            }
            else if (!m_Socket.Listening && conn.IsCreated &&
                     UnityEngine.Random.Range(0, 1000) < 10)
            {
                m_Socket.Disconnect(conn);
                conn = default(NetworkConnection);
            }

            NetworkConnection connection;
            DataStreamReader reader;
            var ev = m_Socket.PopEvent(out connection, out reader);
            if (ev == EventType.Empty)
                return;
            
            {
                DataStreamWriter writer;
                m_Socket.BeginSend(conn, out writer);
                unsafe
                {
                    reader.ReadBytes(writer.AsNativeArray());
                }
                switch (ev)
                {
                    case EventType.Connect:
                        connectCounter++;
                        break;
                    case EventType.Disconnect:
                        conn = default(NetworkConnection);
                        disconnectCounter++;
                        break;
                    case EventType.Data:
                        dataCounter++;
                        m_Socket.EndSend(writer);
                        break;
                }
            }
        }

        public void Dispose()
        {
            m_Socket.Dispose();
        }
    }
}
