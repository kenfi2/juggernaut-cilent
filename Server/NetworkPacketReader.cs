using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juggernaut_client.Server
{
    public class NetworkPacketReader
    {
        private Cryptography.XTEA m_xTEA = null;
        private CommunicationStream m_inputStream = null;

        private bool m_compressed = false;

        public Cryptography.XTEA XTEA
        {
            get => m_xTEA;
            set => m_xTEA = value;
        }

        public bool Compressed
        {
            get => m_compressed;
        }

        public NetworkPacketReader(CommunicationStream stream)
        {
            m_inputStream = stream;
        }

        public bool BytesAvailable(int bytes)
        {
            return m_inputStream.BytesAvailable >= bytes;
        }

        public bool PreparePacket(Action action = null)
        {
            var recvChecksum = m_inputStream.ReadUnsignedInt();

            int payloadOffset = (int)m_inputStream.Position;
            uint checksum = Cryptography.Adler32Checksum.CalculateAdler32Checksum(m_inputStream, payloadOffset, (int)m_inputStream.Length - payloadOffset);
            if (recvChecksum != checksum)
                return false;

            m_inputStream.Position = payloadOffset;
            m_compressed = false;

            if (m_xTEA != null)
            {
                int length = (int)m_inputStream.Length - payloadOffset;
                if (m_xTEA.Decrypt(m_inputStream, payloadOffset, length) == 0)
                    return false;
            }

            action?.Invoke();
            return true;
        }
    }
}
