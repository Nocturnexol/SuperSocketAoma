using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using BS.DB.Comm;
using SuperSocket.SocketBase;
using SuperSocket.SocketEngine;
using SuperSocketAoma.Db;
using SuperSocketAoma.Model;
using SuperSocketAoma.SuperSocket;
using CloseReason = System.Windows.Forms.CloseReason;
using ThreadState = System.Threading.ThreadState;

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
            tbData.Text = new AnalysisAlert().GetExample().Parse();
            var t = new Thread(() =>
            {
                while(true)
                {
                    try
                    {
                        Exception ex;
                        while (BsPackage.ErrorQueue.Count > 0 && BsPackage.ErrorQueue.TryDequeue(out ex))
                        {
                            LogManager.Error(ex.Message, ex);
                        }
                        if (BsPackage.ErrorQueue.Count < 10)
                        {
                            Thread.Sleep(20);
                        }
                        LogManager.Info($"当前队列数量：{BsPackage.PacketQueue.Count}");
                        Thread.Sleep(30 * 1000);
                    }
                    catch (Exception exception)
                    {
                        LogManager.Error(exception.Message, exception);
                        Thread.CurrentThread.Abort();
                    }
                }
            });
            if (t.ThreadState != ThreadState.Running) t.Start();
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

                OracleWriter.Start();
                BsPackage.StartConsuming();
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
