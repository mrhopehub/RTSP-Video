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
        private int playFlag = 0;
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
            playFlag = 1;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button2.Enabled = false;
            pictureBox1.BackColor = Color.LightGray;
            panel1.BackColor = Color.LightBlue;
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
            playFlag = 0;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (rtspPlayer.DrawHandle == panel1.Handle)
            {
                pictureBox1.BackColor = Color.LightBlue;
                panel1.BackColor = Color.LightGray;
                rtspPlayer.DrawHandle = pictureBox1.Handle;
            }
            else
            {
                pictureBox1.BackColor = Color.LightGray;
                panel1.BackColor = Color.LightBlue;
                rtspPlayer.DrawHandle = panel1.Handle;
            }

            //重启
            if (playFlag == 1)
            {
                rtspPlayer.Stop();
                rtspPlayer.Play();
            }
        }
    }
}
