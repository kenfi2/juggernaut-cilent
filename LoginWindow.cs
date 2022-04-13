using juggernaut_client.Server.Protocol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace juggernaut_client
{
    public partial class LoginWindow : BaseForm
    {
        public LoginWindow()
        {
            InitializeComponent();
        }
        protected override bool onClickCloseButton(object sender, EventArgs e)
        {
            Application.Exit();
            return true;
        }

        private void PasswordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ExecuteLogin();
                e.SuppressKeyPress = true;
            }
        }

        private void loginButton_Click(object sender, EventArgs e)
        {
            ExecuteLogin();
        }

        private void ExecuteLogin()
        {
            string _email = emailTextBox.Text;
            string _password = PasswordTextBox.Text;

            if (_email != "" && _password != "")
            {
                try
                {
                    ProtocolLogin login = new ProtocolLogin
                    {
                        Type = (byte)LoginOpcode.DoLogin,
                        EmailAddress = _email,
                        Password = _password
                    };

                    login.Connect("127.0.0.1", 7171);
                } catch
                {
                }
            }
        }
        private void createAccount_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CreateAccount createAccount = new CreateAccount();
            createAccount.ShowDialog();
        }

        public void ConsoleWrite(string msg)
        {
            console.AppendText(msg);
            console.AppendText(Environment.NewLine);
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && textBox1.Text != "")
            {
                ConsoleWrite(textBox1.Text);
                textBox1.Text = "";
                e.SuppressKeyPress = true;
            }
        }
    }
}
