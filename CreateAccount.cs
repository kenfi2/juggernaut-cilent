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
    public partial class CreateAccount : BaseForm
    {
        public CreateAccount()
        {
            InitializeComponent();

            var mainForm = Program.MainForm as LoginWindow;
            mainForm.ConsoleWrite("Create Account\n");
        }

        private void loginButton_Click(object sender, EventArgs e)
        {
            string _username = usernameTextBox.Text;
            string _email = emailTextBox.Text;
            string _password = PasswordTextBox.Text;

            if (_email != "" && _password != "")
            {
                try
                {
                    ProtocolLogin login = new ProtocolLogin
                    {
                        Type = (byte)LoginOpcode.CreateAccount,
                        Username = _username,
                        EmailAddress = _email,
                        Password = _password
                    };

                    login.Connect("127.0.0.1", 7171);

                    this.Close();
                }
                catch
                {
                }
            }
        }
    }
}
