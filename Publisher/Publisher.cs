using System;
using System.Collections.Generic;
using Reference_DLL;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Threading;

namespace Publisher
{
    class Publisher
    {
        public RemotePublisherService publisherService;
        public ProcessConfig publisherData;
        public BrokerInterface broker;
        public TcpChannel channel;
        public Dictionary<string, List<string>> allEvents;
        public int sequenceNumber;
        public List<publisherParameters> commands;
        private bool freeze;
        public List<publisherParameters> freezeList;
        public string ordertype;
        public List<BrokerInterface> brokerReplicas;

        public Publisher()
        {
            publisherData = new ProcessConfig();
            channel = new TcpChannel();
            allEvents = new Dictionary<string, List<string>>();
            publisherService = new RemotePublisherService();
            sequenceNumber = 0;
            ordertype = "";
            freezeList = new List<publisherParameters>();
            commands = new List<publisherParameters>();
            brokerReplicas = new List<BrokerInterface>();
            ChannelServices.RegisterChannel(channel, false);
            RemotingServices.Marshal(publisherService, "pub",
             typeof(RemotePublisherService));
            publisherService.pub += new DelParametersPub(addPublication);
            publisherService.freeze += new DelGetSlaveUrl(setFreezeMode);
            publisherService.unfreeze += new DelGetSlaveUrl(setUnfreeze);
        }

        public void setRemoteBrokerInterface(string url)
        {
            BrokerInterface brokerInt = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface),url);
            Console.WriteLine("Connected to remoteBroker: " + url);
            broker = brokerInt;
        }

        public void setRemoteBrokerReplicaInterface(string url)
        {
            BrokerInterface brokerInt = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface),
               url);
            Console.WriteLine("Connected to remoteBroker: " + url);
            brokerReplicas.Add(brokerInt);
        }

        public void setTCPChanel(string port)
        {
            ChannelServices.UnregisterChannel(channel);
            int _port = Int32.Parse(port);
            channel = new TcpChannel(_port);
            ChannelServices.RegisterChannel(channel, false);
        }

        public void addPublication(publisherParameters parameters)
        {
            commands.Add(parameters);
            ThreadStart td = new ThreadStart(this.AddEvent);
            Thread t_d = new Thread(td);
            t_d.Start();
        }

        public void AddEvent()
        {            
                List<string> list;
                int sqNumber;
                publisherParameters publish;
                publish = commands[commands.Count - 1];
                int number = Int32.Parse(publish.numberEvents);
                for (int i = 0; i < number; i++)
                {
                    lock (this)
                    {
                        if (!ordertype.Equals("TOTAL"))
                        {
                            sequenceNumber += 1;
                            sqNumber = sequenceNumber;
                        }
                        else
                            sqNumber = 0;
                        string events = publish.processName + sqNumber;
                        if (!allEvents.TryGetValue(publish.topicName, out list))
                        {
                            list = new List<string>();
                            allEvents.Add(publish.topicName, list);
                            publish.sequenceNumber = sqNumber;
                            list.Add(events);
                            publish.urlBroker = broker.getData().url;
                            if (!freeze)
                            {
                                Console.WriteLine("PubEvent: " + publish.processName + ", " + publish.topicName + ", " + sqNumber);
                                if (!ordertype.Equals("TOTAL"))
                                    broker.queuEvent(publish);
                                else
                                    broker.queueEventForTotal(publish);
                                broker.logMsgFromBrokerToMaster("PubEvent: " + publish.processName + ", " + publish.topicName + ", " + sqNumber);
                            }
                            else
                            {
                                publisherParameters pub = new publisherParameters();
                                pub.processName = publish.processName;
                                pub.topicName = publish.topicName;
                                pub.numberEvents = publish.numberEvents;
                                pub.intervaloTempo = publish.intervaloTempo;
                                pub.type = publish.type;
                                pub.sequenceNumber = publish.sequenceNumber;
                                pub.urlBroker = publish.urlBroker;
                                freezeList.Add(pub);

                            }
                            int intervalo = Int32.Parse(publish.intervaloTempo);
                            Thread.Sleep(intervalo);
                        }
                        else
                        {
                            allEvents[publish.topicName].Add(events);
                            publish.sequenceNumber = sqNumber;
                            publish.urlBroker = broker.getData().url;
                            if (!freeze)
                            {
                                Console.WriteLine("PubEvent: " + publish.processName + ", " + publish.topicName + ", " + sqNumber);
                                if (!ordertype.Equals("TOTAL"))
                                    broker.queuEvent(publish);
                                else
                                    broker.queueEventForTotal(publish);
                                broker.logMsgFromBrokerToMaster("PubEvent: " + publish.processName + ", " + publish.topicName + ", " + sqNumber);
                            }
                            else
                            {
                                publisherParameters pub = new publisherParameters();
                                pub.processName = publish.processName;
                                pub.topicName = publish.topicName;
                                pub.numberEvents = publish.numberEvents;
                                pub.intervaloTempo = publish.intervaloTempo;
                                pub.type = publish.type;
                                pub.sequenceNumber = publish.sequenceNumber;
                                pub.urlBroker = publish.urlBroker;
                                freezeList.Add(pub);
                            }
                            int intervalo = Int32.Parse(publish.intervaloTempo);
                            Thread.Sleep(intervalo);
                        }
                    }   
                }           
        }

        public void setFreezeMode()
        {
            freeze = true;
            Console.WriteLine("Freeze Mode ON");
        }

        public void setUnfreeze()
        {
            freeze = false;
            Console.WriteLine("Freeze Mode OFF");
            foreach (publisherParameters parameters in freezeList)
            {
                Console.WriteLine("PubEvent: " + parameters.processName + ", " + parameters.topicName + ", " + parameters.sequenceNumber);
                broker.queuEvent(parameters);
                broker.logMsgFromBrokerToMaster("PubEvent: " + parameters.processName + ", " + parameters.topicName + ", " + parameters.sequenceNumber);
            }            
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Active " + args[0] + " " + args[1] + " " + args[2]);
            Publisher publisher = new Publisher();
            publisher.publisherData.name = args[0];
            publisher.publisherData.url = args[1];
            publisher.freeze = false;
            string port = publisher.publisherData.Port;
            publisher.setTCPChanel(port);
            publisher.setRemoteBrokerInterface(args[2]);
            publisher.ordertype = args[3];
            publisher.broker.RegisterPublisher(publisher.publisherData);
            publisher.setRemoteBrokerReplicaInterface(args[4]);
            publisher.setRemoteBrokerReplicaInterface(args[5]);
            foreach(BrokerInterface br in publisher.brokerReplicas)
            {
                br.RegisterSubscriber(publisher.publisherData);
            }
            Console.ReadLine();
        }
    }

    public class RemotePublisherService : MarshalByRefObject, PublisherInterface
    {
        public DelParametersPub pub;
        public DelGetSlaveUrl freeze;
        public DelGetSlaveUrl unfreeze;

        public void addPublication(publisherParameters parameters) { pub(parameters); }
        public void setFreezeMode() { freeze(); }
        public void setUnfreeze() { unfreeze(); }
    }
}
