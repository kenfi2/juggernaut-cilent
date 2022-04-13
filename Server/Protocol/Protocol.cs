using System;

namespace juggernaut_client.Server.Protocol
{
    public abstract class Protocol
    {
        protected Connection m_connection;

        protected NetworkPacketWriter m_packetWriter;
        protected CommunicationStream m_inputStream;
        protected NetworkPacketReader m_packetReader;
        protected Cryptography.XTEA m_xTEA;

        public Protocol()
        {
            CreateNewInputBuffer();

            m_packetWriter = new NetworkPacketWriter();
            m_xTEA = new Cryptography.XTEA();
        }

        private void CreateNewInputBuffer()
        {
            m_inputStream = new CommunicationStream();
            m_packetReader = new NetworkPacketReader(m_inputStream);
        }

        public virtual void Connect(string address, int port)
        {
            if (m_connection != null)
                throw new InvalidOperationException("Protocol.Connect: Trying to establish a connection before terminating.");

            m_connection = new Connection
            {
                Protocol = this
            };

            m_connection.Connect(address, port);
        }

        public virtual void Disconnect(bool dispatch = true)
        {
            if (!!m_connection)
            {
                // there is an public wrapper for disposing the used socket
                // there is no need to reuse the socket
                m_connection.Protocol = null;
                m_connection.Disconnect();
                m_connection = null;
            }
        }

        public virtual void OnConnectionEstablished() { }
        public virtual void OnConnectionTerminated() { }
        public virtual void OnConnectionError(string error, bool disconnecting = false) { }
        public virtual void OnConnectionSocketError(System.Net.Sockets.SocketError e, string error) { }
        public virtual void OnConnectionReceived(CommunicationStream stream)
        {
            try
            {
                if (!m_connection || !m_connection.Established || m_connection.Terminated)
                    return;

                m_inputStream.SetLength(0);
                m_inputStream.Position = Connection.PacketLengthPos;
                stream.CopyTo(m_inputStream);
                m_inputStream.Position = 0;

                if (!m_packetReader.PreparePacket(OnPacketReaderReady))
                    OnConnectionError("Protocol.OnConnectionReceived: Failed to prepare packet.");
            }
            catch (Exception e)
            {
                OnConnectionError($"Protocol.OnConnectionReceived: Failed to prepare packet ({e.Message}).");
            }
        }
        public virtual void OnConnectionSent(CommunicationStream stream) { }

        protected virtual void OnPacketReaderReady()
        {
            //OpenTibiaUnity.GameManager.InvokeOnMainThread(() => {
                if (!!m_connection && m_connection.Established)
                {
                    OnCommunicationDataReady();

                    if (!!m_connection && m_connection.Established && !m_connection.Terminated)
                        m_connection.Receive();
                }
            //});
        }

        protected virtual void OnCommunicationDataReady() { }

        protected virtual void OnPacketWriterFinished()
        {
            if (!!m_connection && m_connection.Established)
                m_connection.Send(m_packetWriter.OutputPacketBuffer);
        }

        public static bool operator !(Protocol instance)
        {
            return instance == null;
        }

        public static bool operator true(Protocol instance)
        {
            return !!instance;
        }

        public static bool operator false(Protocol instance)
        {
            return !instance;
        }
    }
}
