﻿using System;
using System.Diagnostics;
using System.Windows.Forms;
using SuperSocket.SocketBase;
using SuperSocket.SocketEngine;
using SuperSocketAoma.Db;
using SuperSocketAoma.SuperSocket;
using CloseReason = System.Windows.Forms.CloseReason;

namespace SuperSocketAoma
{
    public partial class MainForm : Form
    {
        private bool _startupFlag;
        public static OracleWriter OracleWriter;
        //private readonly ILog _logger=LogManager.GetLogger(typeof(MainForm));
        public MainForm()
        {
            InitializeComponent();
            OracleWriter = new OracleWriter();
        }

        private void Main_Load(object sender, EventArgs e)
        {
        }
        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            //注意判断关闭事件Reason来源于窗体按钮，否则用菜单退出时无法退出!
            if (e.CloseReason != CloseReason.UserClosing) return;
            e.Cancel = true; //取消"关闭窗口"事件
            Visible = false;
        }

        private void 显示窗口ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Visible = true;
            WindowState = FormWindowState.Normal;
            Focus();
        }

        private void 打开目录ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(Application.StartupPath);
        }

        private void 退出程序ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Visible = true;
            WindowState = FormWindowState.Normal;
            Focus();
        }

        private void close_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void startUp_Click(object sender, EventArgs e)
        {
            if (!_startupFlag)
            {
                Program.Bootstrap = BootstrapFactory.CreateBootstrap();
                if (!Program.Bootstrap.Initialize())
                {
                    MessageBox.Show(@"初始化失败!", @"提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var result = Program.Bootstrap.Start();

                if (result == StartResult.Failed)
                {
                    MessageBox.Show(@"服务启动失败!", @"提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                MessageBox.Show(@"服务启动成功!", @"提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Common.Extensions.AddLog("服务已启动");
                info.Text = @"服务已启动";

                BsPackage.StartConsuming();
                OracleWriter.Start();
            }
            else
            {
                Program.Bootstrap.Stop();
                Program.Bootstrap = null;
                info.Text = @"服务已停止";
                Common.Extensions.AddLog("服务已停止");
                BsPackage.StopConsuming();
                OracleWriter.Stop();
            }

            _startupFlag = !_startupFlag;
        }

        private void clear_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            log.Clear();
        }

        

    }
}
