using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MyLibVLC;
using System.Runtime.InteropServices;
using System.Threading;

namespace RTSP_Video
{
    public partial class Form1 : Form
    {
        SimpleRtspPlayer rtspPlayer;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            rtspPlayer.Play();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Enabled = false;
            rtspPlayer = new SimpleRtspPlayer(textBox1.Text, panel1.Handle);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            rtspPlayer.Dispose();
            base.Dispose();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            rtspPlayer.Stop();
        }
    }
}
