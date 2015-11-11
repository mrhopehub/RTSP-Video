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
            textBox1.Enabled = false;
            button1.Enabled = false;
            button2.Enabled = true;
            rtspPlayer.Stop();
            rtspPlayer.Location = textBox1.Text;
            rtspPlayer.Play();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button2.Enabled = false;
            rtspPlayer = new SimpleRtspPlayer(textBox1.Text, panel1.Handle);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            rtspPlayer.Dispose();
            base.Dispose();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Enabled = true;
            button1.Enabled = true;
            button2.Enabled = false;
            rtspPlayer.Stop();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            rtspPlayer.Stop();
            if (rtspPlayer.DrawHandle == panel1.Handle)
            {
                rtspPlayer.DrawHandle = pictureBox1.Handle;
            }
            else
            {
                rtspPlayer.DrawHandle = panel1.Handle;
            }
            rtspPlayer.Play();
        }
    }
}
