using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juggernaut_client.Server
{
    public sealed class NetworkPacketWriter
    {
        private Cryptography.XTEA m_xTEA = null;
        private CommunicationStream m_messageBuffer = new CommunicationStream();
        private CommunicationStream m_outputBuffer = new CommunicationStream();
        private uint m_sequenceNumber = 0;
        public Cryptography.XTEA XTEA
        {
            get => m_xTEA;
            set => m_xTEA = value;
        }

        public CommunicationStream OutputPacketBuffer
        {
            get => m_outputBuffer;
        }
        public CommunicationStream PrepareStream()
        {
            // todo; verify caller's client compatability

            // separate the body from the whole message
            // to make it easier to perform actions on the body

            m_messageBuffer = new CommunicationStream();
            m_outputBuffer = new CommunicationStream();
            return m_messageBuffer;
        }

        public void FinishMessage(Action action = null)
        {
            m_outputBuffer = new CommunicationStream();

            int pos = Connection.PacketLengthPos + Connection.PacketLengthSize + Connection.ChecksumSize;

            m_outputBuffer.Position = pos;
            int messageSize = (int)m_messageBuffer.Length;

            m_messageBuffer.Position = 0;
            if (m_xTEA != null)
            {
                m_outputBuffer.WriteUnsignedShort((ushort)messageSize);
                m_outputBuffer.Write(m_messageBuffer, 0, messageSize);
                m_xTEA.Encrypt(m_outputBuffer, pos, (int)m_outputBuffer.Length - pos);
            } else
            {
                m_outputBuffer.Write(m_messageBuffer, 0, messageSize);
            }

            uint checksum = Cryptography.Adler32Checksum.CalculateAdler32Checksum(m_outputBuffer, pos, (int)m_outputBuffer.Length - pos);
            m_outputBuffer.Position = Connection.ChecksumPos;
            m_outputBuffer.WriteUnsignedInt(checksum);

            m_outputBuffer.Position = Connection.PacketLengthPos;
            m_outputBuffer.WriteShort((short)(m_outputBuffer.Length - Connection.PacketLengthSize));
            m_outputBuffer.Position = 0;

            action?.Invoke();
        }

        public const int ModAdler = 65521;
        public static uint CalculateAdler32Checksum(CommunicationStream stream, int offset = 0, int length = 0)
        {
            if (stream == null)
                throw new ArgumentNullException("Adler32Checksum.CalculateAdler32Checksum: Invalid input.");

            if (offset >= stream.Length)
                throw new ArgumentOutOfRangeException("Adler32Checksum.CalculateAdler32Checksum: Invalid offset.");

            uint a = 1;
            uint b = 0;
            int i = 0;

            stream.Position = offset;
            while (stream.BytesAvailable > 0 && (length == 0 || i < length))
            {
                a = (a + stream.ReadUnsignedByte()) % ModAdler;
                b = (b + a) % ModAdler;
                i++;
            }

            a &= 65535;
            b &= 65535;
            return (b << 16) | a;
        }
    }
}
