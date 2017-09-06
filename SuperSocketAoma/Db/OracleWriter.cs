using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using BS.DB;
using Oracle.DataAccess.Client;
using SuperSocketAoma.Common;
using SuperSocketAoma.SuperSocket;

namespace SuperSocketAoma.Db
{
    public class OracleWriter
    {
        private bool _isRunning;
        private readonly ConcurrentQueue<AnalysisAlertData> _messageQueue = new ConcurrentQueue<AnalysisAlertData>();
        //private readonly OracleDbContext _context;
        private readonly OracleDBVisitor _dbVisitor;
        public OracleWriter()
        {
            //_context = new OracleDbContext();
            _dbVisitor =
                DBFactory.CreateOracleDBAccess(
                    ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString);
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
                //LogManager.Error(e.Message, e);
                BsPackage.ErrorQueue.Enqueue(e);
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

        private void Insert(List<AnalysisAlertData> msgs)
        {
            try
            {
                //_context.BulkInsert(msgs);
                //_context.BulkSaveChanges();
                var paramList = new List<OracleParameter>
                { 
                    new OracleParameter("guid", OracleDbType.Raw)
                    {
                        Direction = ParameterDirection.Input,
                        Value = msgs.Select(t => t.Guid).ToArray()
                    },
                    new OracleParameter("tId", OracleDbType.NVarchar2)
                    {
                        Direction = ParameterDirection.Input,
                        Value = msgs.Select(t => t.TerminalId).ToArray()
                    },
                    new OracleParameter("mId",OracleDbType.Int32)
                    {
                        Direction = ParameterDirection.Input,
                        Value = msgs.Select(t => t.MessageId).ToArray()
                    },
                    new OracleParameter("content",OracleDbType.NVarchar2)
                    {
                        Direction = ParameterDirection.Input,
                        Value = msgs.Select(t => t.Content).ToArray()
                    },
                    new OracleParameter("time",OracleDbType.Date)
                    {
                        Direction = ParameterDirection.Input,
                        Value = msgs.Select(t => t.DateTime).ToArray()
                    },
                    new OracleParameter("et",OracleDbType.Int32)
                    {
                        Direction = ParameterDirection.Input,
                        Value = msgs.Select(t => t.EventType).ToArray()
                    },
                    new OracleParameter("mf",OracleDbType.Int32)
                    {
                        Direction = ParameterDirection.Input,
                        Value = msgs.Select(t => t.Manufacturer).ToArray()
                    },
                    new OracleParameter("fnl",OracleDbType.Byte)
                    {
                        Direction = ParameterDirection.Input,
                        Value = msgs.Select(t => t.FileNameLength).ToArray()
                    },
                    new OracleParameter("fn",OracleDbType.NVarchar2)
                    {
                        Direction = ParameterDirection.Input,
                        Value = msgs.Select(t => t.FileName).ToArray()
                    }
                };

                _dbVisitor.BatchDataOperate("insert into AnalysisAlertData(Guid,TerminalId,MessageId,Content,DateTime,EventType,Manufacturer,FileNameLength,FileName) values(:guid,:tId,:mId,:content,:time,:et,:mf,:fnl,:fn)", msgs.Count, paramList);
            }
            catch (Exception e)
            {
                //LogManager.Error(e.Message,e);
                BsPackage.ErrorQueue.Enqueue(e);
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
