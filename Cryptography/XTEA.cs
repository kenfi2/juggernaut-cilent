using System;
using System.IO;
using System.Text;

namespace juggernaut_client.Cryptography
{
    public class XTEA
    {
        public const int BlockSize = 2 * sizeof(uint);

        uint[] _key = new uint[4];

        public XTEA()
        {
            GenerateKey();
        }

        private void GenerateKey()
        {
            Random rdm = new Random();
            _key[0] = (uint)rdm.Next(int.MinValue, int.MaxValue);
            _key[1] = (uint)rdm.Next(int.MinValue, int.MaxValue);
            _key[2] = (uint)rdm.Next(int.MinValue, int.MaxValue);
            _key[3] = (uint)rdm.Next(int.MinValue, int.MaxValue);
        }

        public void WriteKey(Server.CommunicationStream message)
        {
            message.WriteUnsignedInt(_key[0]);
            message.WriteUnsignedInt(_key[1]);
            message.WriteUnsignedInt(_key[2]);
            message.WriteUnsignedInt(_key[3]);
        }

        public int Encrypt(Server.CommunicationStream message, int offset = 0, int length = int.MaxValue)
        {
            Random rdm = new Random();

            length = Math.Min(length, (int)message.Length - offset);
            message.Position = offset + length;

            int encryptedLength = (int)(Math.Floor((length + BlockSize - 1f) / BlockSize) * BlockSize);
            if (encryptedLength > length)
            {
                byte[] tmp = new byte[encryptedLength - length];
                for (int i = 0; i < tmp.Length; i++)
                    tmp[i] = (byte)rdm.Next(0, 255);

                message.Write(tmp, 0, tmp.Length);
                length = encryptedLength;
            }

            int s = offset;
            while (s < offset + length)
            {
                message.Position = s;
                uint v0 = message.ReadUnsignedInt();
                uint v1 = message.ReadUnsignedInt();
                uint delta = 0x61C88647;
                uint sum = 0;
                for (int r = 0; r < 32; r++)
                {
                    v0 += (v1 << 4 ^ v1 >> 5) + v1 ^ sum + _key[sum & 3];
                    sum -= delta;
                    v1 += (v0 << 4 ^ v0 >> 5) + v0 ^ sum + _key[sum >> 11 & 3];
                }

                message.Position -= BlockSize;
                message.WriteUnsignedInt(v0);
                message.WriteUnsignedInt(v1);

                s += BlockSize;
            }

            return length;
        }

        public int Decrypt(Server.CommunicationStream message, int offset = 0, int length = int.MaxValue)
        {
            length = Math.Min(length, (int)message.Length - offset);
            length -= length % BlockSize;
            int i = offset;
            while (i < offset + length)
            {
                message.Position = i;
                uint v0 = message.ReadUnsignedInt();
                uint v1 = message.ReadUnsignedInt();
                uint delta = 0x61C88647;
                uint sum = 0xC6EF3720;
                for (int r = 0; r < 32; r++)
                {
                    v1 -= (v0 << 4 ^ v0 >> 5) + v0 ^ sum + _key[sum >> 11 & 3];
                    sum += delta;
                    v0 -= (v1 << 4 ^ v1 >> 5) + v1 ^ sum + _key[sum & 3];
                }
                message.Position -= BlockSize;
                message.WriteUnsignedInt(v0);
                message.WriteUnsignedInt(v1);
                i += BlockSize;
            }

            message.Position = offset;
            int payloadLength = message.ReadUnsignedShort();
            int messageLength = payloadLength + Server.Connection.PacketLengthSize;

            int lengthDelta = messageLength - length;
            if (lengthDelta > 0 || -lengthDelta > length)
                return 0;

            message.SetLength(message.Length + lengthDelta);
            return length;
        }
    }
}
