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

            m_packetWriter.onFinishMessage.Connect(OnPacketWriterFinished);
            m_packetReader.onPreparePacket.Connect(OnPacketReaderReady);

            m_connection = new Connection();
            AddConnectionListeners();

            m_connection.Connect(address, port);
        }

        public virtual void Disconnect(bool dispatch = true)
        {
            if (!!m_connection)
            {
                m_packetWriter.onFinishMessage.Disconnect(OnPacketWriterFinished);
                m_packetReader.onPreparePacket.Disconnect(OnPacketReaderReady);

                // there is an public wrapper for disposing the used socket
                // there is no need to reuse the socket
                RemoveConnectionListeners();
                m_connection.Disconnect();
                m_connection = null;
            }
        }

        protected void AddConnectionListeners()
        {
            if (!!m_connection)
            {
                m_connection.onConnectionEstablished.Connect(OnConnectionEstablished);
                m_connection.onConnectionTerminated.Connect(OnConnectionTerminated);
                m_connection.onConnectionError.Connect(OnConnectionError);
                m_connection.onConnectionSocketError.Connect(OnConnectionSocketError);
                m_connection.onConnectionReceived.Connect(OnConnectionReceived);
                m_connection.onConnectionSent.Connect(OnConnectionSent);
            }
        }

        protected void RemoveConnectionListeners()
        {
            if (!!m_connection)
            {
                m_connection.onConnectionEstablished.Disconnect(OnConnectionEstablished);
                m_connection.onConnectionTerminated.Disconnect(OnConnectionTerminated);
                m_connection.onConnectionError.Disconnect(OnConnectionError);
                m_connection.onConnectionSocketError.Disconnect(OnConnectionSocketError);
                m_connection.onConnectionReceived.Disconnect(OnConnectionReceived);
                m_connection.onConnectionSent.Disconnect(OnConnectionSent);
            }
        }

        protected virtual void OnConnectionEstablished() { }
        protected virtual void OnConnectionTerminated() { }
        protected virtual void OnConnectionError(string error, bool disconnecting = false) { }
        protected virtual void OnConnectionSocketError(System.Net.Sockets.SocketError e, string error) { }
        protected virtual void OnConnectionReceived(CommunicationStream stream)
        {
            try
            {
                if (!m_connection || !m_connection.Established || m_connection.Terminated)
                    return;

                m_inputStream.SetLength(0);
                m_inputStream.Position = Connection.PacketLengthPos;
                stream.CopyTo(m_inputStream);
                m_inputStream.Position = 0;

                if (!m_packetReader.PreparePacket())
                    OnConnectionError("Protocol.OnConnectionReceived: Failed to prepare packet.");
            }
            catch (Exception e)
            {
                OnConnectionError($"Protocol.OnConnectionReceived: Failed to prepare packet ({e.Message}).");
            }
        }
        protected virtual void OnConnectionSent(CommunicationStream stream) { }

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
