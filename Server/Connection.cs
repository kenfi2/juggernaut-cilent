using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace juggernaut_client.Server
{
    public class AsyncStateHolder
    {
        public byte[] AsyncBuffer;
        public int State = 0;

        public int Required
        {
            get => AsyncBuffer.Length - State;
        }

        public bool Finished
        {
            get => State == AsyncBuffer.Length;
        }

        public byte[] Buffer
        {
            get => AsyncBuffer;
        }

        public AsyncStateHolder(byte[] buffer, int state = 0)
        {
            AsyncBuffer = buffer;
            State = state;
        }
    }
    public class Connection
    {
        public const int HeaderPos = 0;

        public const int PacketLengthPos = 0;
        public const int PacketLengthSize = sizeof(ushort);

        public const int ChecksumPos = PacketLengthPos + PacketLengthSize;
        public const int ChecksumSize = sizeof(uint);

        public const int SequenceNumberPos = PacketLengthPos + PacketLengthSize;
        public const int SequenceNumberSize = sizeof(uint);
        public const int ConnectionTimeout = 30 * 1000;

        protected Socket m_socket = null;
        protected string m_address = null;
        protected int m_port = 0;
        protected bool m_established = false;
        protected bool m_terminated = false;
        protected bool m_sending = false;
        protected bool m_receiving = false;
        protected Queue m_packetQueue = null;
        protected Protocol.Protocol m_protocol = null;

        public bool Established { get => m_established; }
        public bool Sending { get => m_sending; }
        public bool Receiving { get => m_receiving; }
        public bool Terminated { get => m_terminated; }
        public Protocol.Protocol Protocol { get => m_protocol; set => m_protocol = value; }

        public void Connect(string address, int port)
        {
            if (m_socket != null || m_established)
                throw new InvalidOperationException("Connection.Connect: Trying to connect over an established connection.");

            m_terminated = false;
            m_address = address;
            m_port = port;

            var dnsAddress = Dns.GetHostAddresses(m_address);
            if (dnsAddress == null || dnsAddress.Length == 0)
            {
                if (m_protocol)
                    m_protocol.OnConnectionSocketError(SocketError.AddressNotAvailable, "Invalid IP/Hostname given as a parameter.");
                return;
            }

            var endPoint = new IPEndPoint(dnsAddress[0], port);
            m_socket = new Socket(endPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                var asyncResult = m_socket.BeginConnect(endPoint, null, null);
                asyncResult.AsyncWaitHandle.WaitOne(ConnectionTimeout);
                OnConnectionConnected(asyncResult);
            }
            catch (SocketException e)
            {
                if (m_protocol)
                    m_protocol.OnConnectionSocketError(e.SocketErrorCode, e.Message);
            }
        }
        public void Disconnect()
        {
            if (m_terminated || !m_established)
                return;

            m_socket.Disconnect(false);
            HandleCommunicationTermination();
        }
        public void Send(CommunicationStream message)
        {
            if (m_terminated || !m_established)
                throw new InvalidOperationException("Connection.Send: Trying to send before connecting.");

            CommunicationStream clone;

            long position = message.Position;
            message.Position = 0;
            clone = new CommunicationStream(message);
            message.Position = position;

            lock (m_packetQueue)
            {
                if (m_sending)
                {
                    m_packetQueue.Enqueue(clone);
                    return;
                }

                InternalSend(clone);
            }
        }
        public void Receive()
        {
            if (m_terminated || !m_established)
                throw new InvalidOperationException("Connection.Send: Trying to receive before connecting.");

            if (m_receiving)
                return;

            m_receiving = true;
            InternalReceiveHeader();
        }
        public void InternalSend(CommunicationStream stream)
        {
            byte[] buffer = new byte[stream.Length - stream.Position];
            stream.Read(buffer, 0, buffer.Length);
            InternalSend(buffer);
        }
        public void InternalSend(byte[] buffer)
        {
            m_sending = true;

            var stateObject = new AsyncStateHolder(buffer);
            m_socket.BeginSend(stateObject.Buffer, stateObject.State, stateObject.Required, SocketFlags.None, OnConnectionSend, stateObject);
        }
        protected void InternalReceiveHeader()
        {
            // this function is guaranteed to be called only
            // when the connection is established
            var buffer = new byte[PacketLengthSize];
            var stateObject = new AsyncStateHolder(buffer);
            m_socket.BeginReceive(stateObject.Buffer, stateObject.State, stateObject.Required, SocketFlags.None, OnConnectionRecvHeader, stateObject);
        }

        protected void InernalReceiveBody(int size)
        {
            // this function is guaranteed to be called only
            // when the connection is established
            var buffer = new byte[size];
            var stateObject = new AsyncStateHolder(buffer);
            m_socket.BeginReceive(stateObject.Buffer, stateObject.State, stateObject.Required, SocketFlags.None, OnConnectionRecvBody, stateObject);
        }
        private void OnConnectionConnected(IAsyncResult asyncResult)
        {
            if (m_terminated)
                return;

            try
            {
                m_socket.EndConnect(asyncResult);
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
                HandleCommunicationTermination();
                return;
            }

            m_established = true;
            m_packetQueue = new Queue();

            if (m_protocol)
            {
                m_protocol.OnConnectionEstablished();
            }
        }
        private void OnConnectionSend(IAsyncResult asyncResult)
        {
            if (m_terminated)
                return;

            var stateObject = asyncResult.AsyncState as AsyncStateHolder;
            int total = 0;
            try
            {
                total = m_socket.EndSend(asyncResult);
            }
            catch (SocketException e)
            {
                if (!m_terminated)
                {
                    Console.WriteLine(e.Message);
                    HandleCommunicationTermination();
                }
                return;
            }

            if (total == 0)
            {
                HandleCommunicationTermination();
                return;
            }

            m_sending = false;

            stateObject.State += total;
            if (stateObject.Finished)
            {
                m_protocol.OnConnectionSent(new CommunicationStream(stateObject.AsyncBuffer));
                lock (m_packetQueue)
                {
                    if (m_packetQueue.Count > 0)
                    {
                        InternalSend(m_packetQueue.Dequeue() as CommunicationStream);
                        return;
                    }
                }

                return;
            }

            // send the packets left until the state object is finished
            m_socket.BeginSend(stateObject.Buffer, stateObject.State, stateObject.Required, SocketFlags.None, OnConnectionSend, stateObject);
        }
        private void OnConnectionRecvHeader(IAsyncResult asyncResult)
        {
            if (m_terminated)
                return;

            var stateObject = asyncResult.AsyncState as AsyncStateHolder;
            int total = 0;
            try
            {
                total = m_socket.EndReceive(asyncResult);
            }
            catch (SocketException e)
            {
                if (!m_terminated)
                {
                    Console.WriteLine(e.Message);
                    HandleCommunicationTermination();
                }
                return;
            }

            if (total == 0)
            {
                HandleCommunicationTermination();
                return;
            }

            stateObject.State += total;
            if (stateObject.Finished)
            {
                int size = BitConverter.ToUInt16(stateObject.Buffer, 0);
                InernalReceiveBody(size);
                return;
            }

            // keep receiving until the state object is finished
            m_socket.BeginReceive(stateObject.Buffer, stateObject.State, stateObject.Required, SocketFlags.None, OnConnectionRecvHeader, stateObject);
        }
        private void OnConnectionRecvBody(IAsyncResult asyncResult)
        {
            if (m_terminated)
                return;

            var stateObject = asyncResult.AsyncState as AsyncStateHolder;
            int total = 0;
            try
            {
                total = m_socket.EndReceive(asyncResult);
            }
            catch (SocketException e)
            {
                if (!m_terminated)
                {
                    Console.WriteLine(e.Message);
                    HandleCommunicationTermination();
                }
                return;
            }

            if (total == 0)
            {
                HandleCommunicationTermination();
                return;
            }

            stateObject.State += total;
            if (stateObject.Finished)
            {
                m_protocol.OnConnectionReceived(new CommunicationStream(stateObject.AsyncBuffer));

                m_receiving = false;
                return;
            }

            // keep receiving until the state object is finished
            m_socket.BeginReceive(stateObject.Buffer, stateObject.State, stateObject.Required, SocketFlags.None, OnConnectionRecvBody, stateObject);
        }
        private void HandleCommunicationTermination()
        {
            m_terminated = true;
            m_established = false;
            m_receiving = false;
            m_sending = false;

            m_socket.Dispose();
            m_socket = null;
            m_packetQueue = null;

            if (m_protocol)
            {
                m_protocol.OnConnectionTerminated();
            }
        }
        public static bool operator !(Connection instance)
        {
            return instance == null;
        }

        public static bool operator true(Connection instance)
        {
            return !!instance;
        }

        public static bool operator false(Connection instance)
        {
            return !instance;
        }
    }
}
