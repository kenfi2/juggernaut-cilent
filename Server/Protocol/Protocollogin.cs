using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace juggernaut_client.Server.Protocol
{
    public class ProtocolLogin : Protocol
    {
        public class LoginErrorEvent : ActionEvent<string> { }
        public class LoginTokenErrorEvent : ActionEvent<int> { }
        public class MessageOfTheDayEvent : ActionEvent<int, string> { }
        public class UpdateRequiredEvent : ActionEvent { }
        public class SessionKeyEvent : ActionEvent<string> { }

        protected bool m_tokenSuccess = false;
        protected bool m_expectingTermination = false;

        public string EmailAddress { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public ActionEvent<string> onInternalError = new ActionEvent<string>();
        public ActionEvent<string> onLoginError = new ActionEvent<string>();
        public ActionEvent<int> onLoginTokenError = new ActionEvent<int>();
        public ActionEvent<int, string> onMessageOfTheDay = new ActionEvent<int, string>();
        public ActionEvent onUpdateRequired = new ActionEvent();
        public ActionEvent<string> onSessionKey = new ActionEvent<string>();

        protected override void OnConnectionEstablished()
        {
            SendLogin();
            m_connection.Receive();
        }

        protected override void OnConnectionTerminated()
        {
            if (m_expectingTermination)
                return;

            OnConnectionSocketError(SocketError.ConnectionRefused, string.Empty);
        }

        protected override void OnCommunicationDataReady()
        {

        }

        protected override void OnConnectionError(string message, bool _ = false)
        {
            //OpenTibiaUnity.GameManager.InvokeOnMainThread(() => {
                onLoginError.Execute(message);
            //});

            m_expectingTermination = true;
            Disconnect();
        }

        protected override void OnConnectionSocketError(SocketError code, string message)
        {
            //OpenTibiaUnity.GameManager.InvokeOnMainThread(() => {
                if (code == SocketError.ConnectionRefused || code == SocketError.HostUnreachable)
                    onInternalError.Execute("ERRORMSG_10061_LOGIN_HOSTUNREACHABLE");
                else
                    onInternalError.Execute(string.Format("Error({0}): {1}", code, message));
            //});

            m_expectingTermination = true;
            Disconnect();
        }

        protected void SendLogin()
        {
            var message = m_packetWriter.PrepareStream();
            message.WriteUnsignedByte(0x01);

            int payloadStart = (int)message.Position;
            m_xTEA.WriteKey(message); // encrypt

            message.WriteString(EmailAddress);
            message.WriteString(Password);
            
            //Cryptography.PublicRSA.EncryptMessage(message, payloadStart, Cryptography.PublicRSA.RSABlockSize);

            m_packetWriter.FinishMessage();
            m_packetReader.XTEA = m_xTEA;
            m_packetWriter.XTEA = m_xTEA;
        }
    }
}
