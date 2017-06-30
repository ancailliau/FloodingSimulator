using System;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using NLog;

namespace UCLouvain.FloodingSimulator
{
    public class OptimizationClient : IDisposable
    {
        const string kaos_cm_selection_queue_name = "kaos_cm_selection_queue";

        IConnection connection;
        IModel channel;

        FloodingSimulator _simulator;
        private static Logger logger = LogManager.GetCurrentClassLogger();
    
        public OptimizationClient(FloodingSimulator simulator)
        {
            Setup();
            _simulator = simulator;
        }

        public void Dispose()
        {
            channel.Dispose();
            connection.Dispose();
        }

        void Setup ()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            
            channel.QueueDeclare(queue: kaos_cm_selection_queue_name,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
        }
        
        public void Run ()
        {
            var ts = new ThreadStart(Listen);
            var t = new Thread(ts);
            t.Start();
        }
        
        void Listen ()
        {
            while (!stop)
            {
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    
                    var methods = new JavaScriptSerializer().Deserialize<IEnumerable<string>>(message);
                    
                    try {
                        foreach (var m in methods)
                        {
                            var m_info = typeof(FloodingSimulator).GetMethod(m);
                            if (m_info != null) {
                                m_info.Invoke(_simulator, new object[] {});
                                logger.Info("Method '"+m+"' called");
                                
                            } else {
                                logger.Warn("Method '"+m+"' not found in simulator");
                            }
                        }
                        
                    } catch (Exception e) {
                        logger.Error(e.Message);
                        logger.Error(e.StackTrace);
                    }
                    
                };
                
                channel.BasicConsume(queue: kaos_cm_selection_queue_name,
                                     noAck: true,
                                     consumer: consumer);
                                     
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }            
        }

        bool stop = false;

        internal void Stop()
        {
            stop = true;
            Dispose();
        }
    }
}
