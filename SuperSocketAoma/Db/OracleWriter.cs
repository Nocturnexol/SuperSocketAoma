using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SuperSocketAoma.Common;

namespace SuperSocketAoma.Db
{
    public class OracleWriter
    {
        private bool _isRunning;
        private readonly ConcurrentQueue<AnalysisAlertData> _messageQueue = new ConcurrentQueue<AnalysisAlertData>();
        private readonly OracleDbContext _context;

        public OracleWriter()
        {
            _context = new OracleDbContext();
        }

        public void Enqueue(AnalysisAlertData data)
        {
            try
            {
                if (_isRunning)
                {
                    _messageQueue.Enqueue(data);
                }
            }
            catch (Exception e)
            {
                LogManager.Error(e.Message, e);
                throw;
            }
        }

        public void WriteToOracle()
        {
            while (_isRunning)
            {
                var msgList = new List<AnalysisAlertData>();
                AnalysisAlertData msg;
                while (_messageQueue.Count > 0 && _messageQueue.TryDequeue(out msg))
                {
                    msgList.Add(msg);
                }
                if (msgList.Any())
                    Insert(msgList);
                if (_messageQueue.Count < 10)
                {
                    Thread.Sleep(10);
                }
            }

            Thread.CurrentThread.Abort();
        }

        private int Insert(List<AnalysisAlertData> msgs)
        {
            try
            {
                _context.AnalysisAlert.AddRange(msgs);
                return _context.SaveChanges();
            }
            catch (Exception e)
            {
                LogManager.Error(e.Message,e);
                return 0;
            }
        }

        public void Start()
        {
            var thread = new Thread(WriteToOracle);
            _isRunning = true;
            thread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
        }

    }
}
