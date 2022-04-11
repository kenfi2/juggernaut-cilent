using System;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

namespace juggernaut_client.Cryptography
{
    public class PublicRSA
    {
        public const string RSA_KEY = "109120132967399429278860960508995541528237502902798129123468757937266291492576446330739696001110603907230888610072655818825358503429057592827629436413108566029093628212635953836686562675849720620786279431090218017681061521755056710823876476444260558147179707119674283982419152118103759076030616683978566631413";

        public const int RSABlockSize = 128;

        private static readonly RsaEngine m_engine;
        private static readonly Random m_random;

        static PublicRSA()
        {
            var rsaEncryptKey = new RsaKeyParameters(false, new BigInteger(RSA_KEY), new BigInteger("65537"));
            m_engine = new RsaEngine();
            m_engine.Init(true, rsaEncryptKey);

            m_random = new Random();
        }

        public static void EncryptMessage(Server.CommunicationStream stream, int payloadStart, int blockSize)
        {
            blockSize = Math.Min(blockSize, (int)stream.Length - payloadStart);
            stream.Position = payloadStart + blockSize;

            int length = (int)(Math.Floor((blockSize + RSABlockSize - 1f) / RSABlockSize) * RSABlockSize);
            if (length > blockSize)
            {
                var tmp = new byte[length - blockSize];
                m_random.NextBytes(tmp);

                stream.Write(tmp, 0, tmp.Length);
                blockSize = length;
            }

            stream.Position = payloadStart;
            var bytes = ProcessBlock(stream, payloadStart, blockSize);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static byte[] ProcessBlock(Server.CommunicationStream stream, int offset, int length)
        {
            var oldPosition = stream.Position;
            stream.Position = 0;
            byte[] buffer = new byte[(int)stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            stream.Position = oldPosition;

            return m_engine.ProcessBlock(buffer, offset, length);
        }
    }
}
