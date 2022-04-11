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
    public partial class Form1 : Form
    {
        int m_movX, m_movY;
        bool m_isMoving = false;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private void minimize_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }
        private void topPanel_MouseUp(object sender, MouseEventArgs e)
        {
            m_isMoving = false;
        }

        private void topPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_isMoving)
                this.SetDesktopLocation(MousePosition.X - m_movX, MousePosition.Y - m_movY);
        }

        private void PasswordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ExecuteLogin();
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
                ProtocolLogin login = new ProtocolLogin();

                try
                {
                    login.EmailAddress = _email;
                    login.Password = _password;

                    login.Connect("127.0.0.1", 7171);
                } catch
                {
                }
            }
        }

        public void Write(string msg)
        {
            console.Text += msg + '\n';
        }

        private void topPanel_MouseDown(object sender, MouseEventArgs e)
        {
            m_isMoving = true;
            m_movX = e.X;
            m_movY = e.Y;
        }
    }
}
