using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace juggernaut_client.Server.Protocol
{
    enum LoginOpcode
    {
        InvalidAccountName = 0x01,
        InvalidPassword = 0x02,
        LoginSuccess = 0x03,
        DoLogin = 0x04,
        CreateAccount = 0x05,
        UsernameAlreadyExists = 0x06,
        EmailAlreadyRegistered = 0x07,
        AccountCannotBeCreated = 0x08,
        CreateAccountSuccess = 0x09,
    }
    public class ProtocolLogin : Protocol
    {
        protected bool m_tokenSuccess = false;
        protected bool m_expectingTermination = false;
        protected byte m_type = 0;

        public byte Type { get => m_type; set => m_type = value; }
        public string Username { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public override void OnConnectionEstablished()
        {
            if (m_type == (byte)LoginOpcode.DoLogin)
                SendLogin();
            else if (m_type == (byte)LoginOpcode.CreateAccount)
                CreateAccount();
            else
            {
                Disconnect();
                return;
            }

            m_connection.Receive();
        }

        public override void OnConnectionTerminated()
        {
            if (m_expectingTermination)
                return;

            OnConnectionSocketError(SocketError.ConnectionRefused, string.Empty);
        }

        protected override void OnCommunicationDataReady()
        {
            var mainForm = Program.MainForm as LoginWindow;

            LoginOpcode returnType = (LoginOpcode)m_inputStream.ReadUnsignedByte();
            switch (returnType)
            {
                case LoginOpcode.InvalidAccountName:
                    {
                        mainForm.ConsoleWrite("Invalid Account name");
                        break;
                    }
                case LoginOpcode.InvalidPassword:
                    {
                        mainForm.ConsoleWrite("Invalid Password");
                        break;
                    }
                case LoginOpcode.LoginSuccess:
                    {
                        mainForm.ConsoleWrite("Login Success!");
                        break;
                    }
                // Create Account
                case LoginOpcode.UsernameAlreadyExists:
                    {
                        mainForm.ConsoleWrite("Username already exists!");
                        break;
                    }
                case LoginOpcode.EmailAlreadyRegistered:
                    {
                        mainForm.ConsoleWrite("Email already is registered!");
                        break;
                    }
                case LoginOpcode.AccountCannotBeCreated:
                    {
                        mainForm.ConsoleWrite("Your account cannot be created.");
                        break;
                    }
                case LoginOpcode.CreateAccountSuccess:
                    {
                        mainForm.ConsoleWrite("Create Account succes!");
                        break;
                    }
            }
        }

        public override void OnConnectionError(string message, bool _ = false)
        {
            //OpenTibiaUnity.GameManager.InvokeOnMainThread(() => {
            //onLoginError.Execute(message);
            //});

            m_expectingTermination = true;
            Disconnect();
        }

        public override void OnConnectionSocketError(SocketError code, string message)
        {
            //OpenTibiaUnity.GameManager.InvokeOnMainThread(() => {
            //if (code == SocketError.ConnectionRefused || code == SocketError.HostUnreachable)
            //onInternalError.Execute("ERRORMSG_10061_LOGIN_HOSTUNREACHABLE");
            // else
            //onInternalError.Execute(string.Format("Error({0}): {1}", code, message));
            //});

            m_expectingTermination = true;
            Disconnect();
        }

        protected void SendLogin()
        {
            var message = m_packetWriter.PrepareStream();
            message.WriteUnsignedByte(0x01); // Protocol
            message.WriteUnsignedByte(Type);

            int payloadStart = (int)message.Position;
            m_xTEA.WriteKey(message); // encrypt

            message.WriteString(EmailAddress);
            message.WriteString(Password);

            //Cryptography.PublicRSA.EncryptMessage(message, payloadStart, Cryptography.PublicRSA.RSABlockSize);

            m_packetWriter.FinishMessage(OnPacketWriterFinished);
            m_packetReader.XTEA = m_xTEA;
            m_packetWriter.XTEA = m_xTEA;
        }

        protected void CreateAccount()
        {
            var message = m_packetWriter.PrepareStream();
            message.WriteUnsignedByte(0x01); // Protocol
            message.WriteUnsignedByte(Type);

            int payloadStart = (int)message.Position;
            m_xTEA.WriteKey(message); // encrypt

            message.WriteString(Username);
            message.WriteString(EmailAddress);
            message.WriteString(Password);

            //Cryptography.PublicRSA.EncryptMessage(message, payloadStart, Cryptography.PublicRSA.RSABlockSize);

            m_packetWriter.FinishMessage(OnPacketWriterFinished);
            m_packetReader.XTEA = m_xTEA;
            m_packetWriter.XTEA = m_xTEA;
        }
    }
}
