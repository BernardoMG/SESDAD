using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reference_DLL;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Threading;
using System.Net.Sockets;

namespace Subscriber
{
    class Subscriber
    {
        public RemoteSubscriberService subscriberService;
        public ProcessConfig subscriberData;
        public BrokerInterface broker;
        public TcpChannel channel;
        public List<string> subscriptions;
        public List<string> topics;
        public List<publisherParameters> eventsToDeliver;
        private int deliverCounter;
        private bool freeze;
        public List<publisherParameters> freezeList;
        private List<subscribeParameters> commandsList;
        private List<subscribeParameters> removeCommandList;
        private int commandListOrder;
        private int removeListOrder;
        public List<BrokerInterface> brokerReplicas;


        public Subscriber()
        {
            subscriberData = new ProcessConfig();
            channel = new TcpChannel();
            subscriptions = new List<string>();
            eventsToDeliver = new List<publisherParameters>();
            topics = new List<string>();
            deliverCounter = 0;
            commandListOrder = 0;
            removeListOrder = 0;
            commandsList = new List<subscribeParameters>();
            removeCommandList = new List<subscribeParameters>();
            subscriberService = new RemoteSubscriberService();
            freezeList = new List<publisherParameters>();
            brokerReplicas = new List<BrokerInterface>();
            ChannelServices.RegisterChannel(channel, false);
            RemotingServices.Marshal(subscriberService, "sub",
             typeof(RemoteSubscriberService));
            subscriberService.subscription += new DelParametersSub(AddSubscription);
            subscriberService.eventoDel += new DelParametersPub(deliverEvent);
            subscriberService.unsubscription += new DelParametersSub(RemoveSubscription);
            subscriberService.freeze += new DelGetSlaveUrl(setFreezeMode);
            subscriberService.unfreeze += new DelGetSlaveUrl(setUnfreeze);
        }

        public void setRemoteBrokerInterface(string url)
        {
            BrokerInterface brokerInt = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface),
               url);
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

        public void AddSubscription(subscribeParameters parameters)
        {
            commandsList.Add(parameters);
            ThreadStart tx = new ThreadStart(this.sendSubscription);
            Thread t_x = new Thread(tx);
            t_x.Start();
        }

        public void sendSubscription()
        {
            lock (this)
            {
                subscribeParameters subscriber = new subscribeParameters();
                if (commandsList.Count >= (commandListOrder + 1))
                {
                    subscriber = commandsList[commandListOrder];
                    commandListOrder++;

                    Console.WriteLine("New Subscription on: " + subscriber.topicName);
                    if (subscriber.topicName.Contains("*"))
                    {
                        string[] result = subscriber.topicName.Split(new Char[] { '*' });
                        topics.Add(result[0]);
                        subscriber.topicName = result[0];
                        subscriber.urlSubCreator = subscriberData.url;
                        try {
                            subscriber.urlBroker = broker.getData().url;
                            broker.UpdateSubscriptionList(subscriber);
                            broker.logMsgFromBrokerToMaster("New " + subscriber.type + " from " + subscriber.processName + " on " + subscriber.topicName);
                        }
                        catch (SocketException e)
                        {
                            Console.WriteLine("O Leader esta Morto");
                            broker = brokerReplicas[0];
                            brokerReplicas.RemoveAt(0);
                            broker.setBrokerLeader();
                            broker.setReplicas(brokerReplicas[1].getData().url);
                            Console.WriteLine("New Leader "+ broker.getData().name);
                        }
                        foreach (BrokerInterface brokerReplica in brokerReplicas)
                        {
                            brokerReplica.UpdateSubscriptionList(subscriber);
                        }
                    }
                    else
                    {
                        topics.Add(subscriber.topicName);
                        subscriber.urlSubCreator = subscriberData.url;
                        try
                        {
                            subscriber.urlBroker = broker.getData().url;
                            broker.UpdateSubscriptionList(subscriber);
                            broker.logMsgFromBrokerToMaster("New " + subscriber.type + " from " + subscriber.processName + " on " + subscriber.topicName);
                        }
                        catch (SocketException e)
                        {
                            Console.WriteLine("O Leader esta Morto");
                            broker = brokerReplicas[0];
                            brokerReplicas.Remove(broker);
                            broker.setBrokerLeader();
                            broker.setReplicas(brokerReplicas[1].getData().url);
                            Console.WriteLine("New Leader " + broker.getData().name);

                        }
                        foreach (BrokerInterface brokerReplica in brokerReplicas)
                        {
                            brokerReplica.UpdateSubscriptionList(subscriber);
                        }
                    }
                }
            }
        }

        public void RemoveSubscription(subscribeParameters parameters)
        {
            removeCommandList.Add(parameters);
            ThreadStart tz = new ThreadStart(this.sendRemoveSubscription);
            Thread t_z = new Thread(tz);
            t_z.Start();
        }

        public void sendRemoveSubscription()
        {
            lock (this)
            {
                subscribeParameters removeSubscription = new subscribeParameters();
                int positionToRemove = 0;
                if (removeCommandList.Count >= (removeListOrder + 1))
                {
                    removeSubscription = removeCommandList[removeListOrder];
                    removeListOrder++;
                    if (removeSubscription.topicName.Contains("*"))
                    {
                        string[] result = removeSubscription.topicName.Split(new Char[] { '*' });
                        removeSubscription.topicName = result[0];
                    }
                    for (int i = topics.Count - 1; i >= 0; i--)
                    {
                        if (topics[i].Equals(removeSubscription.topicName))
                        {
                            positionToRemove = i;
                            removeSubscription.urlSubCreator = subscriberData.url;
                            removeSubscription.urlBroker = broker.getData().url;
                            Console.WriteLine("Remove Subscription on: " + removeSubscription.topicName);
                            broker.RemoveSubscriptionList(removeSubscription);
                            foreach (BrokerInterface brokerReplica in brokerReplicas)
                            {
                                brokerReplica.RemoveSubscriptionList(removeSubscription);
                            }
                            broker.logMsgFromBrokerToMaster(removeSubscription.type + " from " + removeSubscription.processName + " on " + removeSubscription.topicName);
                        }
                    }
                    topics.RemoveAt(positionToRemove);
                }
            }
        }

        public void deliverEvent(publisherParameters evento)
        {
            if (!freeze)
            {                
                eventsToDeliver.Add(evento);                
                ThreadStart td = new ThreadStart(this.deliver);
                Thread t_d = new Thread(td);
                t_d.Start();
            }
            else
                freezeList.Add(evento);                
        }

        public void deliver()
        {
            lock (this)
            {
                publisherParameters publish;
                if (eventsToDeliver.Count >= (deliverCounter + 1))
                {
                    publish = eventsToDeliver[deliverCounter];
                    deliverCounter++;
                    broker.logMsgFromBrokerToMaster("SubEvent " + subscriberData.name + ", " + publish.processName + ", " + publish.topicName + ", " + publish.sequenceNumber);
                    Console.WriteLine("SubEvent " + subscriberData.name + ", " + publish.processName + ", " + publish.topicName + ", " + publish.sequenceNumber);
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
                deliverEvent(parameters);
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Active " + args[0] + " " + args[1] + " " + args[2]);
            Subscriber subscriber = new Subscriber();
            subscriber.subscriberData.name = args[0];
            subscriber.freeze = false;
            subscriber.subscriberData.url = args[1];
            string port = subscriber.subscriberData.Port;
            subscriber.setTCPChanel(port);
            subscriber.setRemoteBrokerInterface(args[2]);
            subscriber.broker.RegisterSubscriber(subscriber.subscriberData);
            subscriber.setRemoteBrokerReplicaInterface(args[3]);
            subscriber.setRemoteBrokerReplicaInterface(args[4]);
            foreach (BrokerInterface br in subscriber.brokerReplicas)
            {
                br.RegisterSubscriber(subscriber.subscriberData);
            }
            Console.ReadLine();
        }
    }

    public class RemoteSubscriberService : MarshalByRefObject, SubscriberInterface
    {
        public DelParametersSub subscription;
        public DelParametersPub eventoDel;
        public DelParametersSub unsubscription;
        public DelGetSlaveUrl freeze;
        public DelGetSlaveUrl unfreeze;

        public void deliverEvent(publisherParameters evento) { eventoDel(evento); }
        public void AddSubscription(subscribeParameters topicname) { subscription(topicname); }
        public void RemoveSubscription(subscribeParameters parameters) { unsubscription(parameters); }
        public void setFreezeMode() { freeze(); }
        public void setUnfreeze() { unfreeze(); }
    }
}
