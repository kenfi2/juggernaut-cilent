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
    public partial class BaseForm : Form
    {
        private bool m_isMoving = false;
        private Point m_movedPos = new Point();
        public BaseForm()
        {
            InitializeComponent();
        }

        protected virtual bool onClickCloseButton(object sender, EventArgs e) => false;
        private void closeButton_Click(object sender, EventArgs e)
        {
            if (!onClickCloseButton(sender, e))
            {
                this.Close();
            }
        }
        private void minimizeButton_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void topPanel_MouseDown(object sender, MouseEventArgs e)
        {
            m_isMoving = true;
            m_movedPos.X = e.X;
            m_movedPos.Y = e.Y;
        }

        private void topPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (!m_isMoving)
                return;

            this.SetDesktopLocation(MousePosition.X - m_movedPos.X, MousePosition.Y - m_movedPos.Y);
        }

        private void topPanel_MouseUp(object sender, MouseEventArgs e)
        {
            m_isMoving = false;
        }
    }
}
