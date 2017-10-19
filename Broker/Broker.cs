using System;
using System.Collections.Generic;
using Reference_DLL;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Threading;


namespace Projecto_DAD
{
    class Broker
    {
        public ProcessConfig brokerData;
        public PSlavesInterface remoteSlave;
        public PMServerInterface remotePM;
        public RemoteBrokerService remoteBroker;
        public TcpChannel channel;
        private List<PublisherInterface> publishers;
        private Dictionary<string, string> publishersRegistry;
        private List<SubscriberInterface> subscriber;
        private Dictionary<string, string> subscribersRegistry;
        private List<string> topicToBroadCast;
        private List<string> childs;
        private string fatherBrokerUrl;
        private Dictionary<string, List<string>> topicsSubscriptons;
        private List<publisherParameters> commandList;
        private string orderType;
        private string loggingLevel;
        private string routingPolicy;
        private Dictionary<string, int> deliverStatus;
        private Dictionary<string, List<publisherParameters>> waitingList;
        private List<string> logToDeliver;
        public string freezeMode;
        public List<publisherParameters> freezeList;
        public List<routingEvents> routingTable;
        public List<subscribeParameters> subFlood;
        public List<subscribeParameters> subFloodRemove;
        public int subGlobal;
        public string topico;
        public int rountingEventManager;
        public int orderManager;
        public bool SingleMachine;
        public int counter;
        public int removeCounter;
        public List<dummyStruct> requestDummy;
        public int dummyOrder;
        private int totalLocalOrder;
        private int globalSequenceNumber;
        private List<publisherParameters> waitingListForTotal;
        private int rountingEventManagerRoot;
        private List<publisherParameters> commandListForTotal;
        public bool isLeader;
        public List<BrokerInterface> replicas;
        public string LeaderUrl;


        public Broker()
        {
            publishers = new List<PublisherInterface>();
            publishersRegistry = new Dictionary<string, string>();
            subscriber = new List<SubscriberInterface>();
            subscribersRegistry = new Dictionary<string, string>();
            brokerData = new ProcessConfig();
            topicToBroadCast = new List<string>();
            topicsSubscriptons = new Dictionary<string, List<string>>();
            waitingList = new Dictionary<string, List<publisherParameters>>();
            deliverStatus = new Dictionary<string, int>();
            channel = new TcpChannel();
            remoteBroker = new RemoteBrokerService();
            childs = new List<string>();
            commandList = new List<publisherParameters>();
            logToDeliver = new List<string>();
            totalLocalOrder = 1;
            globalSequenceNumber = 1;
            waitingListForTotal = new List<publisherParameters>();
            commandListForTotal = new List<publisherParameters>();
            subGlobal = 0;
            rountingEventManager = 0;
            subFloodRemove = new List<subscribeParameters>();
            fatherBrokerUrl = "none";
            orderManager = 0;
            counter = 0;
            requestDummy = new List<dummyStruct>();
            removeCounter = 0;
            dummyOrder = 0;
            topico = "";
            replicas = new List<BrokerInterface>();
            LeaderUrl = "";
            isLeader = false;
            freezeMode = "Unfreeze";
            subFlood = new List<subscribeParameters>();
            routingTable = new List<routingEvents>();
            freezeList = new List<publisherParameters>();
            ChannelServices.RegisterChannel(channel, false);
            RemotingServices.Marshal(remoteBroker, "broker",
              typeof(RemoteBrokerService));
            remoteBroker.registerSubscriber += new DelStartProcess(RegisterSubscriber);
            remoteBroker.registerPublisher += new DelStartProcess(RegisterPublisher);
            remoteBroker.processData += new DelGetProcessData(getData);
            remoteBroker.addChildDel += new DelAddMsgToLog(addChild);
            remoteBroker.addFatherDel += new DelAddMsgToLog(addFather);
            remoteBroker.addSubscription += new DelParametersSub(UpdateSubscriptionList);
            remoteBroker.flooding += new DelSetServerInterface(floodingEvents);
            remoteBroker.pub += new DelParametersPub(queuEvent);
            remoteBroker.logMaster += new DelsSetTCPChanel(logMsgFromBrokerToMaster);
            remoteBroker.removeSubscription += new DelParametersSub(RemoveSubscription);
            remoteBroker.status += new DelIniciateRelation(getStatus);
            remoteBroker.freezeMode += new DelIniciateRelation(setFreezeMode);
            remoteBroker.unfreeze += new DelIniciateRelation(setUnfreeze);
            remoteBroker.subFlood += new DelParametersSub(subFilter);
            remoteBroker.subFloodRemove += new DelParametersSub(removeSubFilter);
            remoteBroker.crash += new DelFlooding(updateProcess);
            remoteBroker.global += new DelGetSlaveUrl(sendToRoot);
            remoteBroker.queuTotal += new DelParametersPub(queueEventForTotal);
            remoteBroker.setLeader += new DelSetLeader(setBrokerAsLeader);
            remoteBroker.addReplica += new DelAddReplica(setBrokerReplicas);
        }

        public void setRemoteInterface(string url)
        {
            if (url.Equals("8086"))
            {
                SingleMachine = true;
                PMServerInterface remotePMservice = (PMServerInterface)Activator.
                GetObject(typeof(PMServerInterface),
                "tcp://localhost:8086/RemotePMService");
                Console.WriteLine("Connected: " + "tcp://localhost:8086/RemotePMService");
                this.remotePM = remotePMservice;
                this.remotePM.MsgToMaster("Connection with broker: " + brokerData.name + " Estabilshed");
            }
            else
            {
                SingleMachine = false;
                PSlavesInterface slave = (PSlavesInterface)Activator.GetObject(typeof(PSlavesInterface),
                  "tcp://localhost:" + url + "/Slave");
                Console.WriteLine("Connected to remoteSlave: " + url);
                remoteSlave = slave;
            }
        }

        public void setBrokerAsLeader()
        {
            isLeader = true;
        }

        public void setBrokerReplicas(string repUrl)
        {
            BrokerInterface replica = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), repUrl);
            replicas.Add(replica);
            Console.WriteLine("Replica: " + replica.getData().name + " Added");
        }

        public void getStatus()
        {
            if (!brokerData.creatorPort.Equals("8086"))
            {
                remoteSlave.msgToSlave("Check for all active publisher's");
                foreach (string pub in publishersRegistry.Keys)
                {
                    remoteSlave.msgToSlave("Publisher: " + pub + " is active");
                }
                remoteSlave.msgToSlave("Check for all active subscriber's");
                foreach (string sub in subscribersRegistry.Keys)
                {
                    remoteSlave.msgToSlave("Subscriber: " + sub + " is active");

                }
                remoteSlave.msgToSlave("Check all subscription's");
                foreach (string topic in topicsSubscriptons.Keys)
                {
                    remoteSlave.msgToSlave("Topic: " + topic + " is subscribed");
                }
            }
            else
            {
                remotePM.MsgToMaster("Check for all active publisher's");
                foreach (string pub in publishersRegistry.Keys)
                {
                    remotePM.MsgToMaster("Publisher: " + pub + " is active");
                }
                remotePM.MsgToMaster("Check for all active subscriber's");
                foreach (string sub in subscribersRegistry.Keys)
                {
                    remotePM.MsgToMaster("Subscriber: " + sub + " is active");

                }
                remotePM.MsgToMaster("Check all subscription's");
                foreach (string topic in topicsSubscriptons.Keys)
                {
                    remotePM.MsgToMaster("Topic: " + topic + " is subscribed");
                }
            }
        }

        public void setTCPChanel(string port)
        {
            ChannelServices.UnregisterChannel(channel);
            int _port = Int32.Parse(port);
            channel = new TcpChannel(_port);
            ChannelServices.RegisterChannel(channel, false);
        }

        public ProcessConfig getData()
        {
            return brokerData;
        }

        public void updateProcess(string type, string name,string url)
        {
            switch (type)
            {
                case "subscriber":
                    subscribersRegistry.Remove(name);
                    string keyToRemove = "";
                    string urlToRemove = "";
                    KeyValuePair<string, List<string>> pairToRemove = new KeyValuePair<string, List<string>>();
                    foreach(KeyValuePair<string, List<string>> pair in topicsSubscriptons)
                    {
                        foreach(string subUrl in pair.Value)
                        {
                            if (subUrl.Equals(url))
                            {
                                pairToRemove = pair;
                                urlToRemove = subUrl;
                            }
                        }
                    }
                    pairToRemove.Value.Remove(urlToRemove);
                    if (pairToRemove.Value.Count == 0)
                    {
                        keyToRemove = pairToRemove.Key;
                        Console.WriteLine("The subscription on: " + pairToRemove.Key + " was removed because this sub was killed: " + urlToRemove);
                    }
                    if (keyToRemove!="")
                        topicsSubscriptons.Remove(keyToRemove);
                    break;
                case "publisher":
                    publishersRegistry.Remove(name);
                    break;
            }
        }

        public void RegisterSubscriber(ProcessConfig subscriberInfo)
        {         
            SubscriberInterface newSubscriber = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface),subscriberInfo.url);
            subscriber.Add(newSubscriber);
            subscribersRegistry.Add(subscriberInfo.name, subscriberInfo.url);
            Console.WriteLine("New Subscriber: " + subscriberInfo.url);
            if(!brokerData.creatorPort.Equals("8086"))
                remoteSlave.msgToSlave("New Subscriber: " + subscriberInfo.name+" " + subscriberInfo.url);
        }

        public void RegisterPublisher(ProcessConfig publisherInfo)
        {
            PublisherInterface newPublisher = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface),publisherInfo.url);
            publishers.Add(newPublisher);
            publishersRegistry.Add(publisherInfo.name, publisherInfo.url);
            Console.WriteLine("New Publisher: " + publisherInfo.url);
            if (!brokerData.creatorPort.Equals("8086"))
                remoteSlave.msgToSlave("New Publisher: " + publisherInfo.name + " " + publisherInfo.url);
        }

        public void addChild(string urlChildBroker)
        {
                childs.Add(urlChildBroker);
                Console.WriteLine("New Child: " + urlChildBroker);
        }   

        public void addFather(string urlFatherBroker)
        {
            fatherBrokerUrl = urlFatherBroker;
            Console.WriteLine("My Father: " + urlFatherBroker);
        }

        public void UpdateSubscriptionList(subscribeParameters parameters)
        {
            lock (this)
            {
                List<string> list;
                if (!topicsSubscriptons.TryGetValue(parameters.topicName, out list))
                {
                    list = new List<string>();
                    topicsSubscriptons.Add(parameters.topicName, list);
                    list.Add(parameters.urlSubCreator);
                }
                else
                    topicsSubscriptons[parameters.topicName].Add(parameters.urlSubCreator);
                Console.WriteLine("New Subscription arrived from: " + parameters.processName + " on: " + parameters.topicName);
                if (!SingleMachine)
                    remoteSlave.msgToSlave("New Subscription arrived from: " + parameters.processName + " on: " + parameters.topicName);
                if (routingPolicy.Equals("filter"))
                    subFilter(parameters);
            }
        }

        public void subFilter(subscribeParameters parameters)
        {            
            if (!brokerData.url.Equals(parameters.urlBroker))
            {
                UpdateRoutingTable(parameters);
            }
            if (isLeader)
            {
                if (!subscribersRegistry.ContainsKey(parameters.processName))
                {
                    foreach(BrokerInterface replica in replicas)
                    {
                        replica.UpdateSubscriptionList(parameters);
                    }
                }
                subFlood.Add(parameters);
                ThreadStart td = new ThreadStart(this.subscriptionFlood);
                Thread t_d = new Thread(td);
                t_d.Start();
            }   
        }

        public void UpdateRoutingTable(subscribeParameters parameters) {
            bool cantCreateNewEvent = false;
            foreach (routingEvents Event in routingTable)
            {
                if (Event.Topic.Equals(parameters.topicName) && Event.UrlSubscriberOnTopic.Equals(parameters.urlSubCreator))
                {
                    cantCreateNewEvent = true;
                }
            }
            if (!cantCreateNewEvent)
            {
                routingEvents newEventArrived = new routingEvents();
                newEventArrived.Topic = parameters.topicName;
                newEventArrived.UrlBrokerSender = parameters.urlBrokerSender;
                newEventArrived.UrlSubscriberOnTopic = parameters.urlSubCreator;
                routingTable.Add(newEventArrived);
                Console.WriteLine("RoutingTable Updated on: " + brokerData.name + " for topic: " + parameters.topicName + " sender: " + parameters.urlBrokerSender + " creator: " + parameters.urlSubCreator);
            }
        }

        public void subscriptionFlood()
        {
            lock (this)
            {
                subscribeParameters subscriber;
                string senderInPacket = "";
                if (subFlood.Count >= (counter + 1))
                {
                    subscriber = subFlood[counter];
                    counter++;
                    senderInPacket = subscriber.urlBrokerSender;
                    if (fatherBrokerUrl != "none" && fatherBrokerUrl != subscriber.urlBrokerSender)
                    {
                        subscriber.urlBrokerSender = brokerData.url;
                        BrokerInterface brokerFather = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), fatherBrokerUrl);
                        brokerFather.subFilter(subscriber);
                        subscriber.urlBrokerSender = senderInPacket;
                    }
                    if (childs != null)
                    {
                        foreach (string urlChild in childs)
                        {
                            if (urlChild != subscriber.urlBroker && urlChild != subscriber.urlBrokerSender)
                            {
                                subscriber.urlBrokerSender = brokerData.url;
                                BrokerInterface brokerChild = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), urlChild);
                                brokerChild.subFilter(subscriber);
                                subscriber.urlBrokerSender = senderInPacket;
                            }
                        }
                    }
                }
            }
        }

        public void RemoveSubscription(subscribeParameters parameters)
        {
            lock (this)
            {
                int position = 0;
                if (!SingleMachine)
                    remoteSlave.msgToSlave("Unsubscribe from: " + parameters.processName + " on topic: " + parameters.topicName);
                Console.WriteLine("Removed Subscription from: " + parameters.processName + " on: " + parameters.topicName);
                for (int i = 0; i < topicsSubscriptons[parameters.topicName].Count; i++)
                {
                    if (topicsSubscriptons[parameters.topicName][i].Equals(parameters.urlSubCreator))
                    {
                        position = i;
                    }
                }
                topicsSubscriptons[parameters.topicName].RemoveAt(position);
                if (topicsSubscriptons[parameters.topicName].Count == 0)
                    topicsSubscriptons.Remove(parameters.topicName);
                if (routingPolicy.Equals("filter"))
                    removeSubFilter(parameters);
            }
        }

        public void removeSubFilter(subscribeParameters parameters)
        {
            if (!brokerData.url.Equals(parameters.urlBroker))
            {
                removeEntryRoutingTable(parameters);
            }
            if (isLeader)
            {
                if (!subscribersRegistry.ContainsKey(parameters.processName))
                {
                    foreach (BrokerInterface replica in replicas)
                    {
                        replica.RemoveSubscriptionList(parameters);
                    }
                }
                subFloodRemove.Add(parameters);
                ThreadStart td = new ThreadStart(this.subscriptionFloodRemove);
                Thread t_d = new Thread(td);
                t_d.Start();
            }
        }

        public void removeEntryRoutingTable(subscribeParameters parameters)
        {
            routingEvents eventToRemove = new routingEvents(); 
            bool canRemoveEvent = false;
            foreach(routingEvents RoutingEventToRemove in routingTable)
            {
                if(RoutingEventToRemove.Topic.Equals(parameters.topicName) && RoutingEventToRemove.UrlSubscriberOnTopic.Equals(parameters.urlSubCreator))
                {
                    canRemoveEvent = true;
                    eventToRemove.Topic = RoutingEventToRemove.Topic;
                    eventToRemove.UrlBrokerSender = RoutingEventToRemove.UrlBrokerSender;
                    eventToRemove.UrlSubscriberOnTopic = RoutingEventToRemove.UrlSubscriberOnTopic;
                }
            }
            if (canRemoveEvent)
                routingTable.Remove(eventToRemove);
        }

        public void subscriptionFloodRemove()
        {
            lock (this)
            {
                subscribeParameters subscriber;
                string senderInPacket = "";
                if (subFloodRemove.Count >= (removeCounter + 1))
                {
                    subscriber = subFloodRemove[removeCounter];
                    removeCounter++;
                    senderInPacket = subscriber.urlBrokerSender;
                    if (fatherBrokerUrl != null)
                    {
                        if (!(fatherBrokerUrl.Equals("none")) && !(fatherBrokerUrl.Equals(subscriber.urlBrokerSender)))
                        {
                            subscriber.urlBrokerSender = brokerData.url;
                            BrokerInterface brokerFather = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), fatherBrokerUrl);
                            brokerFather.removeSubFilter(subscriber);
                            subscriber.urlBrokerSender = senderInPacket;
                        }
                    }
                    if (childs != null)
                    {
                        foreach (string urlChild in childs)
                        {
                            if (!(urlChild.Equals(subscriber.urlBrokerSender)))
                            {
                                subscriber.urlBrokerSender = brokerData.url;
                                BrokerInterface brokerChild = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), urlChild);
                                brokerChild.removeSubFilter(subscriber);
                                subscriber.urlBrokerSender = senderInPacket;
                            }
                        }
                    }
                }
            }
        }

        public void setFreezeMode()
        {
            freezeMode = "Freeze";
            Console.WriteLine("Freeze Mode On");
        }

        public void setUnfreeze() {
            freezeMode = "Unfreeze";
            Console.WriteLine("Freeze Mode Off");
            if (!orderType.Equals("TOTAL"))
            {
                foreach (publisherParameters parameters in freezeList)
                {
                    queuEvent(parameters);
                }
            }
            else
            {
                foreach (publisherParameters parameters in freezeList)
                {
                    queueEventForTotal(parameters);
                }
            }
        }

        public void queuEvent(publisherParameters parameters)
        {
            if (!deliverStatus.ContainsKey(parameters.processName))
                deliverStatus.Add(parameters.processName, 1);
            if (freezeMode.Equals("Unfreeze"))
            {
                if (routingPolicy.Equals("filter"))
                {
                    commandList.Add(parameters);
                    ThreadStart td = new ThreadStart(this.deliverEvents);
                    Thread t_d = new Thread(td);
                    t_d.Start();
                    ThreadStart t_f= new ThreadStart(this.filteringBase);
                    Thread t = new Thread(t_f);
                    t.Start();
                }
                else
                {
                    commandList.Add(parameters);
                    ThreadStart td = new ThreadStart(this.deliverEvents);
                    Thread t_d = new Thread(td);
                    t_d.Start();
                    ThreadStart ts = new ThreadStart(this.floodingEvents);
                    Thread t = new Thread(ts);
                    t.Start();                  
                }
            }
            else
                freezeList.Add(parameters);
        }

        public void queueEventForTotal(publisherParameters publisher)
        {
            if (freezeMode.Equals("Unfreeze"))
            {
                commandListForTotal.Add(publisher);
                ThreadStart td = new ThreadStart(this.sendToRoot);
                Thread t_d = new Thread(td);
                t_d.Start();
            }
            else
                freezeList.Add(publisher);
        }

        public void filteringBase()
        {
            lock (this)
            {
                publisherParameters publish;
                List<string> urls = new List<string>();
                string senderInPacket = "";
                dummyStruct dummy = new dummyStruct();
                bool send = false;
                if (commandList.Count >= (rountingEventManager + 1))
                {
                    publish = commandList[rountingEventManager];
                    rountingEventManager++;
                    urls = hasRoutingEntry(publish.topicName);
                    senderInPacket = publish.urlBrokerSender;
                    dummy.urlRoutingTable = urls;
                    dummy.publish = publish;
                    requestDummy.Add(dummy);
                    if (!SingleMachine)
                        remoteSlave.msgToSlave("New Event by: " + publish.processName + " on: " + publish.topicName);
                    if (urls.Count != 0)
                    {
                        foreach (string url in urls)
                        {
                            if (!url.Equals(publish.urlBrokerSender))
                            {
                                send = true;
                                BrokerInterface broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url);
                                publish.urlBrokerSender = brokerData.url;
                                broker.queuEvent(publish);
                                publish.urlBrokerSender = senderInPacket;
                            }
                        }
                    }
                    if(send)
                        Console.WriteLine("BroEvent: " + brokerData.name + ", " + publish.processName + ", " + publish.topicName + ", " + publish.sequenceNumber);
                    ThreadStart b = new ThreadStart(this.dummyGlobalUpdate);
                    Thread t_b = new Thread(b);
                    t_b.Start();
                }
            }
        }

        public void dummyGlobalUpdate()
        {
            lock (this)
            {
                dummyStruct dummy = new dummyStruct();
                string senderInPacket = "";
                if (requestDummy.Count >= (dummyOrder + 1)) {
                    dummy = requestDummy[dummyOrder];
                    dummyOrder++;
                    senderInPacket = dummy.publish.urlBrokerSender;
                    if (!dummy.urlRoutingTable.Contains(fatherBrokerUrl) && fatherBrokerUrl != "none" && !fatherBrokerUrl.Equals(dummy.publish.urlBrokerSender))
                    {
                        BrokerInterface brokerFather = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), fatherBrokerUrl);
                        dummy.publish.urlBrokerSender = brokerData.url;
                        brokerFather.queuEvent(dummy.publish);
                        dummy.publish.urlBrokerSender = senderInPacket;
                    }
                    if (childs != null)
                    {
                        foreach (string child in childs)
                        {
                            if (!dummy.urlRoutingTable.Contains(child) && !child.Equals(dummy.publish.urlBrokerSender))
                            {
                                BrokerInterface brokerChild = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), child);
                                dummy.publish.urlBrokerSender = brokerData.url;
                                brokerChild.queuEvent(dummy.publish);
                                dummy.publish.urlBrokerSender = senderInPacket;
                            }
                        }
                    }
                }
            }
        }

        public List<string> hasRoutingEntry(string topico1)
        {
            List<string> result = new List<string>();
            foreach(routingEvents RouteEvent in routingTable)
            {
                string TopicInRoutingTable = RouteEvent.Topic;
                char lastCharOfSubscription = TopicInRoutingTable[TopicInRoutingTable.Length - 1];
                if (lastCharOfSubscription == '/')
                {
                    if (topico1.Contains(TopicInRoutingTable) && !result.Contains(RouteEvent.UrlBrokerSender))
                    {
                        result.Add(RouteEvent.UrlBrokerSender);
                    }
                }
                else
                {
                    if (topico1.Equals(TopicInRoutingTable) && !result.Contains(RouteEvent.UrlBrokerSender))
                        result.Add(RouteEvent.UrlBrokerSender);
                }
            }
            return result;
        }

        public void logMsgFromBrokerToMaster(string log)
        {
            if (!brokerData.creatorPort.Equals("8086"))
            {
                logToDeliver.Add(log);
                ThreadStart td = new ThreadStart(this.deliverToSlave);
                Thread t_d = new Thread(td);
                t_d.Start();
            }
            else
                remotePM.MsgToMaster(log);
        }

        public void deliverToSlave()
        {
            string logs;
            lock (this)
            {
                logs = logToDeliver[logToDeliver.Count - 1];
            }
            remoteSlave.addMsgToLogMaster(logs);
        }

        public void deliverEvents()
        {
            string result = "";
            switch (orderType)
            {
                case "NO":
                    lock (this)
                    {
                        publisherParameters request= new publisherParameters();
                        if (commandList.Count >= (orderManager + 1))
                        {
                            request = commandList[orderManager];
                            orderManager++;
                            result = existTopic(request);
                            if (!result.Equals(""))
                            {
                                foreach (string url in topicsSubscriptons[result])
                                {
                                    SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                                    sub.deliverEvent(request);
                                }
                            }
                        }
                    }
                    break;
                case "FIFO":
                    lock (this) {
                        publisherParameters request2 = new publisherParameters();
                        if (commandList.Count >= (orderManager + 1))
                        {
                            request2 = commandList[orderManager];
                            orderManager++;
                            result = existTopic(request2);
                            if (searchSequenceNumber(request2))
                            {
                                if (!result.Equals(""))
                                {
                                    foreach (string url in topicsSubscriptons[result])
                                    {
                                        SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                                        sub.deliverEvent(request2);
                                    }
                                }
                                deliverStatus[request2.processName]++;
                            }
                            else
                            {
                                List<publisherParameters> list;
                                if (!waitingList.TryGetValue(request2.processName, out list))
                                {
                                    list = new List<publisherParameters>();
                                    waitingList.Add(request2.processName, list);
                                    list.Add(request2);
                                }
                                else
                                    waitingList[request2.processName].Add(request2);
                            }
                            if (waitingList.Count != 0 && waitingList.ContainsKey(request2.processName))
                            {
                                completeRequestOnWait(request2.processName);
                            }
                        }
                    }                                                        
            break;
                case "TOTAL":
                    lock (this)
                    {
                        publisherParameters request3 = new publisherParameters();
                        if (commandList.Count >= (orderManager + 1))
                        {
                            request3 = commandList[orderManager];
                            orderManager++;
                            result = existTopic(request3);
                            if (searchSequenceNumber(request3))
                            {
                                if (!result.Equals(""))
                                {
                                    foreach (string url in topicsSubscriptons[result])
                                    {
                                        SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                                        sub.deliverEvent(request3);
                                    }
                                }
                                totalLocalOrder++;
                            }
                            else
                            {
                                waitingListForTotal.Add(request3);
                            }
                            if (waitingListForTotal.Count != 0)
                            {
                                completeRequestOnWait(request3.processName);
                            }
                        }
                    }
                    break;
            }
        }
        
        private void completeRequestOnWait(string pub)
        {
            string result = "";
            int tamanho = 0;
            if (orderType.Equals("FIFO"))
            {
                    tamanho = waitingList[pub].Count;         
                for (int i = 0; i < tamanho; i++)
                {
                    publisherParameters parameters = waitingList[pub][i];
                    if (parameters.sequenceNumber == deliverStatus[pub])
                    {
                        result = existTopic(parameters);
                        if (!result.Equals(""))
                        {
                            foreach (string url in topicsSubscriptons[result])
                            {
                                SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                                sub.deliverEvent(parameters);
                            }
                        }
                        deliverStatus[pub]++;                      
                    }
                }
            }
            else
            {
                foreach (publisherParameters publisher in waitingListForTotal)
                {
                    if (publisher.sequenceNumber == totalLocalOrder)
                    {
                        result = existTopic(publisher);
                        if (!result.Equals(""))
                        {
                            foreach (string url in topicsSubscriptons[result])
                            {
                                SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                                sub.deliverEvent(publisher);
                            }
                        }
                        totalLocalOrder++;
                    }
                }              
            }
        }

        private string existTopic(publisherParameters request2)
        {
            string result = "";
            foreach (string topic in topicsSubscriptons.Keys)
            {
                char lastCharOfSubscription = topic[topic.Length - 1];
                if (lastCharOfSubscription=='/')
                {
                    if (request2.topicName.Contains(topic))
                    {
                        result = topic;
                    }
                }
                else
                {
                    if (request2.topicName.Equals(topic))
                        result = topic;
                }
            }
            return result;
        }

        private bool searchSequenceNumber(publisherParameters request2)
        {
            if (orderType.Equals("FIFO"))
            {
                foreach (string pub in deliverStatus.Keys)
                {
                    if (pub.Equals(request2.processName) && request2.sequenceNumber == deliverStatus[pub])
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (totalLocalOrder == request2.sequenceNumber)
                    return true;
            }    
            return false;
        }

        public void floodingEvents()
        {
            lock (this)
            {
                publisherParameters publish;
                string senderInPacket = "";
                if (commandList.Count >= (rountingEventManager + 1))
                {
                    publish = commandList[rountingEventManager];
                    rountingEventManager++;
                    senderInPacket = publish.urlBrokerSender;
                    Console.WriteLine("BroEvent: " + brokerData.name + ", " + publish.processName + ", " + publish.topicName + ", " + publish.sequenceNumber);
                    if (!SingleMachine)
                        remoteSlave.msgToSlave("New Event by: " + publish.processName + " on: " + publish.topicName);
                    if (loggingLevel.Equals("full"))
                        logMsgFromBrokerToMaster("BroEvent: " + brokerData.name + ", " + publish.processName + ", " + publish.topicName + ", " + publish.sequenceNumber);
                    if (fatherBrokerUrl != "none" && fatherBrokerUrl != publish.urlBrokerSender)
                    {
                        publish.urlBrokerSender = brokerData.url;
                        BrokerInterface brokerFather = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), fatherBrokerUrl);
                        brokerFather.queuEvent(publish);
                        publish.urlBrokerSender = senderInPacket;
                    }
                    if (childs != null)
                    {
                        foreach (string urlChild in childs)
                        {
                            if (urlChild != publish.urlBrokerSender)
                            {
                                publish.urlBrokerSender = brokerData.url;
                                BrokerInterface brokerChild = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), urlChild);
                                brokerChild.queuEvent(publish);
                                publish.urlBrokerSender = senderInPacket;
                            }
                        }
                    }
                }
            }
        }

        public void sendToRoot()
        {
            lock (this)
            {
                publisherParameters publish = new publisherParameters();
                if (commandListForTotal.Count >= (rountingEventManagerRoot + 1))
                {
                    publish = commandListForTotal[rountingEventManagerRoot];
                    rountingEventManagerRoot++;
                    if (!fatherBrokerUrl.Equals("none"))
                    {
                        BrokerInterface brokerFather = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), fatherBrokerUrl);
                        brokerFather.queueEventForTotal(publish);
                    }
                    else
                    {
                        publish.sequenceNumber = globalSequenceNumber;
                        publish.urlBrokerSender = brokerData.url;
                        globalSequenceNumber++;
                        if (routingPolicy.Equals("filter"))
                        {
                            commandList.Add(publish);
                            ThreadStart td = new ThreadStart(this.deliverEvents);
                            Thread t_d = new Thread(td);
                            t_d.Start();
                            ThreadStart t_f = new ThreadStart(this.filteringBase);
                            Thread t = new Thread(t_f);
                            t.Start();
                        }
                        else
                        {
                            commandList.Add(publish);
                            ThreadStart td = new ThreadStart(this.deliverEvents);
                            Thread t_d = new Thread(td);
                            t_d.Start();
                            ThreadStart ts = new ThreadStart(this.floodingEvents);
                            Thread t = new Thread(ts);
                            t.Start();
                        }
                    }
                }
            }
        }

    static void Main(string[] args)
        {
            Console.WriteLine("Active " + args[0] + " " + args[1] + " " + args[2]);
            Broker broker = new Broker();
            broker.brokerData.name = args[0];
            broker.brokerData.url = args[1];
            broker.orderType = args[3];
            broker.loggingLevel = args[4];
            broker.routingPolicy = args[5];
            broker.brokerData.creatorPort = args[2];
            string port = broker.brokerData.Port;
            broker.setTCPChanel(port);
            broker.setRemoteInterface(args[2]);
            if (!args[2].Equals("8086")) 
                broker.remoteSlave.RegisterBroker(broker.brokerData);
            if (args.Length == 7)
            {
                broker.LeaderUrl = args[6];
                Console.WriteLine("I'm a replica of leader: " + broker.LeaderUrl);
            }
            else
            {
                Console.WriteLine("I am the leader");

            }
            Console.ReadLine();
        }
    }

    public class RemoteBrokerService : MarshalByRefObject, BrokerInterface
    {
        public DelStartProcess registerSubscriber;
        public DelStartProcess registerPublisher;
        public DelGetProcessData processData;
        public DelAddMsgToLog addChildDel;
        public DelAddMsgToLog addFatherDel;
        public DelParametersSub addSubscription;
        public DelSetServerInterface flooding;
        public DelParametersPub pub;
        public DelsSetTCPChanel logMaster;
        public DelParametersSub removeSubscription;
        public DelIniciateRelation status;
        public DelIniciateRelation freezeMode;
        public DelIniciateRelation unfreeze;
        public DelParametersSub subFlood;
        public DelParametersSub subFloodRemove;
        public DelFlooding crash;
        public DelGetSlaveUrl global;
        public DelParametersPub queuTotal;
        public DelSetLeader setLeader;
        public DelAddReplica addReplica;

        public void setBrokerLeader() { setLeader(); }
        public void setReplicas(string replica) { addReplica(replica); }
        public RemoteBrokerService(){}
        public void RegisterSubscriber(ProcessConfig config) { registerSubscriber(config); }
        public void RegisterPublisher(ProcessConfig config) { registerPublisher(config); }
        public ProcessConfig getData() { return processData(); }
        public void addChild(string url) { addChildDel(url); }
        public void addFather(string url) { addFatherDel(url); }
        public void UpdateSubscriptionList(subscribeParameters parameters) { addSubscription(parameters); }
        public void RemoveSubscriptionList(subscribeParameters parameters) { removeSubscription(parameters); }
        public void floodingEvents() { flooding(); }
        public void queuEvent(publisherParameters parameters2) { pub(parameters2); }
        public void logMsgFromBrokerToMaster(string log) { logMaster(log); }
        public void getStatus() { status(); }
        public void setFreezeMode() { freezeMode(); }
        public void setUnfreeze() { unfreeze(); }
        public void subFilter(subscribeParameters parameters) { subFlood(parameters); }
        public void removeSubFilter(subscribeParameters parameters) { subFloodRemove(parameters); }
        public void updateProcess(string type, string name, string url) { crash(type, name, url); }
        public void sendToRoot() { global(); }
        public void queueEventForTotal(publisherParameters publisher) { queuTotal(publisher); }
    }
}