using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading;
using Reference_DLL;
using System.IO;
using System.Diagnostics;

namespace Projecto_DAD
{
    class PuppetMaster
    {
        public MasterForm form;
        public RemotePMService remotePMService;
        public TcpChannel channel;
        public Dictionary<string, string> slavesRegistry;
        private Dictionary<string, string> brokersRegistry;
        public List<PSlavesInterface> slaves;
        public List<string> messages;
        public int masterPort = 8086;
        public int generatedPort = 0;
        public List<publisherParameters> requests;
        public Dictionary<string, SiteNode> siteTree;
        public bool SingleMachine;
        private List<Process> processes;
        private Object thisLock = new Object();
        private int maxReplicatedPort;
        private int maxBrokerReplicas;
        private Dictionary<string, List<string>> brokerReplicasRegistry;


        public PuppetMaster()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new MasterForm();
            slaves = new List<PSlavesInterface>();
            slavesRegistry = new Dictionary<string, string>();
            brokersRegistry = new Dictionary<string, string>();
            messages = new List<string>();
            requests = new List<publisherParameters>();
            remotePMService = new RemotePMService();
            generatedPort = masterPort;
            channel = new TcpChannel(masterPort);
            siteTree = new Dictionary<string, SiteNode>();
            processes = new List<Process>();
            SingleMachine = false;
            maxReplicatedPort = 0;
            maxBrokerReplicas = 2;
            brokerReplicasRegistry = new Dictionary<string, List<string>>();
            ChannelServices.RegisterChannel(channel, false);
            RemotingServices.Marshal(remotePMService, "RemotePMService",
                typeof(RemotePMService));
            form.formDelegateAddToLog += new DelAddMsgToLog(addMsgToLog);
            form.delGetSlaveUrl += new DelGetSlaveUrl(showSlavesInfo);
            form.verificaMaquina += new DelAddMsgToLog(verificaExecucao);
            form.addRelation += new DelIniciateRelation(addRelation);
            form.sendCommand += new DelAddMsgToLog(verificaCommando);
            form.readScript += new DelAddMsgToLog(executeScript);
            remotePMService.sendMsgToSlaves += new DelAddMsgToLog(msgToSlaves);
            remotePMService.registerSlave += new DelRegisterSlave(saveSlaveInRegistry);
            remotePMService.registerSlave += new DelRegisterSlave(registerSlave);
            remotePMService.remoteDelegateAddMsgToLog += new DelAddMsgToLog(addMsgToLog);
            remotePMService.myFather += new DelGetInfo(GetMySlaveFatherUrl);
            remotePMService.logDelivery += new DelAddMsgToLog(addLog);
        }

        [STAThread]
        static void Main(string[] args)
        {
            PuppetMaster PMaster = new PuppetMaster();
            Application.Run(PMaster.form);
        }

        public void addMsgToLog(string message)
        {
            form.Invoke(new DelAddMsgToLog(form.sendMessageToLog1), message);
        }

        public void addLog(string log)
        {
            addMsgToLog(log);
        }

        public void addMsgToLog2(string message)
        {
            form.Invoke(new DelAddMsgToLog(form.sendMessageToLog2), message);
        }

        public void saveSlaveInRegistry(SiteNode args)
        {
            addMsgToLog("PS::Saving Slave "+args.name+" in Registry:");
            this.slavesRegistry.Add(args.port, args.Url);
        }

        public void registerSlave(SiteNode args)
        {
            PSlavesInterface newSlave = (PSlavesInterface)Activator.GetObject(typeof(PSlavesInterface),
                    "tcp://localhost:" + args.port + "/Slave");
            this.slaves.Add(newSlave);
            msgToSlaves("Slave " + args.name + " have been created in port: " + args.port);
            remotePMService.MsgToMaster("Slave " + args.name + " have been created in port: " + args.port);
        }

        public void showSlavesInfo()
        {
            foreach (String s in slavesRegistry.Values)
            {
                addMsgToLog2("Slave: " + s);
            }
            if (brokersRegistry.Count != 0)
            {
                foreach (String s in brokersRegistry.Values)
                {
                    addMsgToLog2("Broker: " + s);
                }
            }
            if (brokerReplicasRegistry.Count != 0)
            {
                foreach (KeyValuePair<string, List<string>> entry in brokerReplicasRegistry)
                {
                    foreach (String rep in entry.Value)
                    {
                        addMsgToLog2("Broker Replicas: " + rep);
                    }
                }
            }
        }

        public void msgToSlaves(string message)
        {
            messages.Add(message);
            ThreadStart ts = new ThreadStart(this.broadcastMessage);
            Thread t = new Thread(ts);
            t.Start();
        }

        private void broadcastMessage()
        {
            lock (this)
            {
                string MsgToBcast;
                MsgToBcast = messages[messages.Count - 1];
                for (int i = 0; i < slaves.Count; i++)
                {
                    try
                    {
                        ((PSlavesInterface)slaves[i]).msgToSlave(MsgToBcast);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed sending message to client. Removing client. " + e.Message);
                        slaves.RemoveAt(i);
                    }
                }
            }
        }

        public void verificaExecucao(string filename)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(filename);
            string line = file.ReadLine();
            if (line.Contains("SingleMachine"))
            {
                parseScriptSingle(filename);
                SingleMachine = true;
            }
            else
            {
                parseScript(filename);
                SingleMachine = false;
            }
        }

        public void verificaCommando(string command)
        {         
            if (SingleMachine)
            {
                sendCommandSingleMachine(command);
            }
            else
            {
                sendCommandToSlave(command);
            }
        }

        public void setMaxReplicatedBrokerPort(int port)
        {
            if (port > maxReplicatedPort)
                maxReplicatedPort = port;
        }
        // -------------------MULTIMACHINE--------------------------------------------------------------------//
        public void parseScript(string filename)
        {
            string line;
            string[] words;
            string orderType="FIFO";
            string loggingLevel = "light";
            string routingPolicy = "flooding";
            System.IO.StreamReader file = new System.IO.StreamReader(filename);
            if (slaves == null)
               addMsgToLog ("Slave Missing");
            else
            {
                while ((line = file.ReadLine()) != null && slaves != null)
                {
                    words = line.Split();
                    if (line.Contains("Ordering"))
                    {
                        switch (words[1])
                        {
                            case "NO":
                                orderType = "NO";
                                break;
                            case "FIFO":
                                orderType = "FIFO";
                                break;
                            case "TOTAL":
                                orderType = "TOTAL";
                                break;
                        }
                    }
                    if (line.Contains("LoggingLevel"))
                    {
                        switch (words[1])
                        {
                            case "light":
                                loggingLevel = "light";
                                break;
                            case "full":
                                loggingLevel = "full";
                                break;
                        }
                    }
                    if (line.Contains("RoutingPolicy"))
                    {
                        switch (words[1])
                        {
                            case "flodding":
                                routingPolicy = "flooding";
                                break;
                            case "filter":
                                routingPolicy = "filter";
                                break;
                        }
                    }
                    if (line.Contains("Site"))
                    {
                        bool search = false;
                        foreach (PSlavesInterface s in slaves)
                        {
                            string aux = s.getSlaveData().name;
                            if (words[1].Equals(aux))
                            { 
                                search = true;
                                continue;
                            }
                        }
                        if (!search)
                           addMsgToLog( "Slave " + words[1] + " not present");

                    }
                    if (line.Contains("Process"))
                    {
                        foreach (PSlavesInterface s in slaves)
                        {
                            if (words[5].Equals(s.getSlaveData().name))
                            {
                                ProcessConfig config = new ProcessConfig();
                                config.name = words[1];
                                config.url = words[7];
                                config.procType = words[3];
                                config.orderType = orderType;
                                config.loggingLevelType = loggingLevel;
                                config.routingPolicy = routingPolicy;                 
                                addMsgToLog("Iniciate Process: " + config.name + " URL: " + config.url);
                                s.startProcess(config);                            
                            }
                        }
                    }    
                }
            }
        }

        public void addRelation()
        {
            if (!SingleMachine)
            {
                foreach (PSlavesInterface slave in slaves)
                {
                    string urlFather = slave.getMyFatherUrl();
                    if (urlFather != "none")
                    {
                        string urlBrokerChild = slave.getBrokerUrl();
                        PSlavesInterface father = (PSlavesInterface)Activator.GetObject(typeof(PSlavesInterface),
                      urlFather);
                        string urlBrokerFather = father.getBrokerUrl();
                        father.fatherHandShake(urlBrokerFather, urlBrokerChild);
                        slave.childHandShake(urlBrokerChild, urlBrokerFather);
                    }
                }
            }
            else
            {
                addRelationSingleMachine();
            }
        }

        public string GetMySlaveFatherUrl(string father)
        {
            if (father == "none") { }
            else {
                foreach (PSlavesInterface slaves in slaves)
                {
                    if (slaves.getSlaveData().name == father)
                    {
                        return slaves.getSlaveData().Url;
                    }
                }
            }
            return "none";
        }

        public void addTask(publisherParameters request)
        {
            requests.Add(request);
            ThreadStart ts = new ThreadStart(this.deliverTaskToPub);
            Thread t = new Thread(ts);
            t.Start();
        }

        public void deliverTaskToPub()
        {
            lock (this)
            {
                publisherParameters pub;
                pub = requests[requests.Count - 1];
                PublisherInterface publisher = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), pub.urlPublisher);
                publisher.addPublication(pub);
            }
        }

        private string checkType(string url)
        {
            if (url.Contains("/sub"))
                return "subscriber";
            if (url.Contains("/pub"))
                return "publisher";
            else
                return "broker";
        }

        public void statusCommandMulti()
        {
            foreach (PSlavesInterface slave in slaves)
            {
                slave.getStatus();
            }
        }

        public void executeScript(string filename)
        {
            if (!SingleMachine)
            {
                string[] lines = File.ReadAllLines(filename);
                string url = "";
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] words = lines[i].Split();
                    if (words[0].Equals("Wait"))
                    {
                        int time = Int32.Parse(words[1]);
                        Thread.Sleep(time);
                    }
                    if (words[0].Equals("Status"))
                    {                     
                        ThreadStart tx = new ThreadStart(this.statusCommandMulti);
                        Thread tq = new Thread(tx);
                        tq.Start();                        
                    }
                    if (words[0].Equals("Freeze"))
                    {
                        string type = "";
                        foreach (PSlavesInterface slave in slaves)
                        {
                            url = slave.verifyProcessName(words[1]);
                            type = checkType(url);
                            if (url != "")
                            {
                                switch (type)
                                {
                                    case "broker":
                                        BrokerInterface broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url);
                                        broker.setFreezeMode();
                                        break;
                                    case "subscriber":
                                        SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                                        sub.setFreezeMode();
                                        break;
                                    case "publisher":
                                        PublisherInterface pub = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), url);
                                        pub.setFreezeMode();
                                        break;
                                }
                            }
                        }
                    }
                    if (words[0].Equals("Unfreeze"))
                    {
                        string type = "";
                        foreach (PSlavesInterface slave in slaves)
                        {
                            url = slave.verifyProcessName(words[1]);
                            type = checkType(url);
                            if (url != "")
                            {
                                switch (type)
                                {
                                    case "broker":
                                        BrokerInterface broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url);
                                        broker.setUnfreeze();
                                        break;
                                    case "subscriber":
                                        SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                                        sub.setUnfreeze();
                                        break;
                                    case "publisher":
                                        PublisherInterface pub = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), url);
                                        pub.setUnfreeze();
                                        break;                                    
                                }
                            }
                        }
                    }
                    if (words[0].Equals("Crash"))
                    {
                        string type = "";
                        foreach (PSlavesInterface slave in slaves)
                        {
                            url = slave.verifyProcessName(words[1]);
                            type = checkType(url);
                            if (url != "")
                            {

                                slave.removeProcessFromList(words[1], type, url);
                            }
                        }
                    }
                    foreach (PSlavesInterface slave in slaves)
                    {
                        if (words.Length > 2)
                        {
                            url = slave.verifyProcessName(words[1]);


                            if (url != "")
                            {
                                switch (words[2])
                                {
                                    case "Subscribe":
                                        SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                                        subscribeParameters parameters = new subscribeParameters();
                                        parameters.processName = words[1];
                                        parameters.topicName = words[3];
                                        parameters.type = words[2];
                                        sub.AddSubscription(parameters);
                                        break;
                                    case "Unsubscribe":
                                        SubscriberInterface sub2 = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                                        subscribeParameters parameters3 = new subscribeParameters();
                                        parameters3.processName = words[1];
                                        parameters3.topicName = words[3];
                                        parameters3.type = words[2];
                                        sub2.RemoveSubscription(parameters3);
                                        break;
                                    case "Publish":
                                        publisherParameters parameters2 = new publisherParameters();
                                        parameters2.urlPublisher = url;
                                        parameters2.processName = words[1];
                                        parameters2.numberEvents = words[3];
                                        parameters2.topicName = words[5];
                                        parameters2.intervaloTempo = words[7];
                                        parameters2.type = words[2];
                                        PublisherInterface pub = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), url);
                                        pub.addPublication(parameters2);
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            else
                executeScriptSingleMachine(filename);
        }

        public void sendCommandToSlave(string command)
        {
            string url = "";
            string[] words = command.Split();
            if (words[0].Equals("Wait"))
            {
                int time = Int32.Parse(words[1]);
                Thread.Sleep(time);
            }
            if (words[0].Equals("Status"))
            {
                ThreadStart tx = new ThreadStart(this.statusCommandMulti);
                Thread tq = new Thread(tx);
                tq.Start();
            }
            if (words[0].Equals("Freeze"))
            {
                string type = "";
                foreach (PSlavesInterface slave in slaves)
                {
                    url = slave.verifyProcessName(words[1]);
                    type = checkType(url);
                    if (url != "")
                    {
                        switch (type)
                        {
                            case "broker":
                                BrokerInterface broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url);
                                broker.setFreezeMode();
                                break;
                            case "subscriber":
                                SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                                sub.setFreezeMode();
                                break;
                            case "publisher":
                                PublisherInterface pub = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), url);
                                pub.setFreezeMode();
                                break;
                        }
                    }
                }
            }
            if (words[0].Equals("Unfreeze"))
            {
                string type = "";
                foreach (PSlavesInterface slave in slaves)
                {
                    url = slave.verifyProcessName(words[1]);
                    type = checkType(url);
                    if (url != "")
                    {
                        switch (type)
                        {
                            case "broker":
                                BrokerInterface broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url);
                                broker.setUnfreeze();
                                break;
                            case "subscriber":
                                SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                                sub.setUnfreeze();
                                break;
                            case "publisher":
                                PublisherInterface pub = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), url);
                                pub.setUnfreeze();
                                break;
                        }
                    }
                }
            }
            if (words[0].Equals("Crash"))
            {
                string type = "";
                foreach (PSlavesInterface slave in slaves)
                {
                    url = slave.verifyProcessName(words[1]);
                    type = checkType(url);
                    if (url != "")
                    {

                        slave.removeProcessFromList(words[1], type, url);
                    }
                }
            }
            foreach (PSlavesInterface slave in slaves)
            {
                if (words.Length > 2)
                {
                    url = slave.verifyProcessName(words[1]);

                }
                if (url != "")
                {
                    switch (words[2])
                    {
                        case "Subscribe":
                            SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                            subscribeParameters parameters = new subscribeParameters();
                            parameters.processName = words[1];
                            parameters.topicName = words[3];
                            parameters.type = words[2];
                            sub.AddSubscription(parameters);
                            break;
                        case "Unsubscribe":
                            SubscriberInterface sub2 = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                            subscribeParameters parameters3 = new subscribeParameters();
                            parameters3.processName = words[1];
                            parameters3.topicName = words[3];
                            parameters3.type = words[2];
                            sub2.RemoveSubscription(parameters3);
                            break;
                        case "Publish":
                            publisherParameters parameters2 = new publisherParameters();
                            parameters2.urlPublisher = url;
                            parameters2.processName = words[1];
                            parameters2.numberEvents = words[3];
                            parameters2.topicName = words[5];
                            parameters2.intervaloTempo = words[7];
                            parameters2.type = words[2];
                            PublisherInterface pub = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), url);
                            pub.addPublication(parameters2);
                            break;
                    }
                }
            }
        }

        // -------------------SINGLEMACHINE--------------------------------------------------------------------//
        public int generatePort()
        {
            return ++generatedPort;
        }

        public int getReplicaPort()
        {
            maxReplicatedPort = maxReplicatedPort + 1;
            return this.maxReplicatedPort;
        }

        public void startBrokerReplicas(ProcessConfig brokerLeader)
        {
            brokerReplicasRegistry.Add(brokerLeader.name, new List<string>());
            for (int i = 0; i < maxBrokerReplicas; i++)
            {
                ProcessConfig broReplicaConfig = new ProcessConfig();
                broReplicaConfig.name = brokerLeader.name + i;
                broReplicaConfig.port = getReplicaPort().ToString();
                broReplicaConfig.site = brokerLeader.site;
                broReplicaConfig.url = "tcp://localhost:" + broReplicaConfig.port + "/broker";
                broReplicaConfig.orderType = brokerLeader.orderType;
                broReplicaConfig.routingPolicy = brokerLeader.routingPolicy;
                broReplicaConfig.procType = brokerLeader.procType;
                broReplicaConfig.loggingLevelType = brokerLeader.loggingLevelType;
                addMsgToLog("Iniciatiating Broker's " + brokerLeader.name + " Replica: " + broReplicaConfig.name + " URL: " + broReplicaConfig.url + "Port:" + broReplicaConfig.port);
                BuildPaths.PPath = "\\Broker\\bin\\Debug\\Broker.exe";
                string BroPath = BuildPaths.PPath;
                string BroArgs =
                    broReplicaConfig.name + " " +
                    broReplicaConfig.url + " " +
                    masterPort + " " +
                    brokerLeader.orderType + " " +
                    brokerLeader.loggingLevelType + " " +
                    brokerLeader.routingPolicy + " " +
                    brokerLeader.url;
                ;
                brokerReplicasRegistry[brokerLeader.name].Add(broReplicaConfig.url);
                ProcessStartInfo BroRepStartInfo = new ProcessStartInfo();
                BroRepStartInfo.FileName = BroPath;
                BroRepStartInfo.Arguments = BroArgs;
                Process brokerReplica = Process.Start(BroRepStartInfo);
                BrokerInterface Leader = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brokerLeader.url);
                Leader.setReplicas(broReplicaConfig.url);
            }
        }

        public ProcessConfig setConfiguration(string filename)
        {
            string line;
            string[] words;
            ProcessConfig newProc = new ProcessConfig();
            StreamReader file = new StreamReader(filename);
            while ((line = file.ReadLine()) != null)
            {
                words = line.Split();
            }
            return newProc;
        }

        public void parseScriptSingle(string filename)
        {
            string line;
            string[] words;
            StreamReader file = new StreamReader(filename);
            ProcessConfig newProc = new ProcessConfig();
            while ((line = file.ReadLine()) != null)
            {
                words = line.Split();
                if (line.Contains("RoutingPolicy"))
                {
                    switch (words[1])
                    {
                        case "flooding":
                            newProc.routingPolicy = "flooding";
                            break;
                        case "filter":
                            newProc.routingPolicy = "filter";
                            break;
                    }
                }
                if (line.Contains("Ordering"))
                {
                    switch (words[1])
                    {
                        case "NO":
                            newProc.orderType = "NO";
                            break;
                        case "FIFO":
                            newProc.orderType = "FIFO";
                            break;
                        case "TOTAL":
                            newProc.orderType = "TOTAL";
                            break;
                    }
                }
                if (line.Contains("LoggingLevel"))
                {
                    switch (words[1])
                    {
                        case "light":
                            newProc.loggingLevelType = "light";
                            break;
                        case "full":
                            newProc.loggingLevelType = "full";
                            break;
                    }
                }
                if (line.Contains("Site"))
                {
                    SiteNode site = new SiteNode();
                    site.name = words[1];
                    site.father = words[3];
                    site.port = generatePort().ToString();
                    siteTree.Add(site.name, site);
                }
                if (line.Contains("Process"))
                {
                    foreach (KeyValuePair<string, SiteNode> entry in siteTree)
                    {
                        if (entry.Key.Equals(words[5])) //site is equal to site on process line
                        {
                            newProc.name = words[1];
                            newProc.procType = words[3];
                            newProc.site = words[5];
                            newProc.url = words[7];
                            entry.Value.siteProcecess.Add(newProc);
                        }
                    }
                    setMaxReplicatedBrokerPort(Int32.Parse(newProc.Port));
                }
            }
            startBrokerProcesses();
        }

        public void startBrokerProcesses()
        {
            foreach (KeyValuePair<string, SiteNode> entry in siteTree)
            {
                foreach (ProcessConfig process in entry.Value.siteProcecess)
                {
                    if (process.procType.Equals("broker"))
                    {
                        addMsgToLog("Iniciate Process: " + process.name + " URL: " + process.url);
                        BuildPaths.PPath = "\\Broker\\bin\\Debug\\Broker.exe";
                        string path = BuildPaths.PPath;
                        string args =
                            process.name + " " +
                            process.url + " " +
                            masterPort + " " +
                            process.orderType + " " +
                            process.loggingLevelType + " " +
                            process.routingPolicy;
                        brokersRegistry.Add(process.name, process.url);
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = path;
                        startInfo.Arguments = args;
                        Process brokerProcess = Process.Start(startInfo);
                        processes.Add(brokerProcess);
                        entry.Value.broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), process.url);
                        BrokerInterface broker=(BrokerInterface)Activator.GetObject(typeof(BrokerInterface), process.url);
                        broker.setBrokerLeader();
                        startBrokerReplicas(process);
                    }
                }
            }
            startPublisherProcesses();
            startSubsriberProcesses();
        }

        public void startSubsriberProcesses()
        {
            foreach (KeyValuePair<string, SiteNode> entry in siteTree)
            {
                foreach (ProcessConfig process in entry.Value.siteProcecess)
                {
                    if (entry.Key.Equals(process.site))
                    {
                        if (process.procType.Equals("subscriber"))
                        {
                            string[] repUrl = brokerReplicasRegistry[entry.Value.siteBroker.getData().name].ToArray();
                            BuildPaths.PPath = "\\Subscriber\\bin\\Debug\\Subscriber.exe";
                            string path = BuildPaths.PPath;
                            string args = process.name + " " +
                                          process.url + " " +
                                          entry.Value.siteBroker.getData().url + " " +
                                          repUrl[0] + " " +
                                          repUrl[1];
                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.FileName = path;
                            startInfo.Arguments = args;
                            Process sub = Process.Start(startInfo);
                            processes.Add(sub);
                        }
                    }
                }
            }
        }

        public void startPublisherProcesses()
        {
            foreach (KeyValuePair<string, SiteNode> entry in siteTree)
            {
                foreach (ProcessConfig process in entry.Value.siteProcecess)
                {
                    if (entry.Key.Equals(process.site))
                    {
                        if (process.procType.Equals("publisher"))
                        {
                            string[] repUrl = brokerReplicasRegistry[entry.Value.siteBroker.getData().name].ToArray();
                            BuildPaths.PPath = "\\Publisher\\bin\\Debug\\Publisher.exe";
                            string path = BuildPaths.PPath;
                            string args = process.name + " " +
                                          process.url + " " +
                                          entry.Value.siteBroker.getData().url + " " +
                                          process.orderType+ " "+
                                          repUrl[0] + " " +
                                          repUrl[1];
                            ;
                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.FileName = path;
                            startInfo.Arguments = args;
                            Process pub = Process.Start(startInfo);
                            processes.Add(pub);
                        }
                    }
                }
            }
        }

        public void addRelationSingleMachine()
        {
            BrokerInterface brokerFather = null;
            BrokerInterface brokerChild;
            foreach (SiteNode site in siteTree.Values)
            {
                string father = site.father;
                if (!father.Equals("none"))
                {
                    brokerChild = site.broker;
                    foreach (String name in siteTree.Keys)
                    {
                        if (name.Equals(father))
                        {
                            brokerFather = siteTree[name].broker;
                            brokerFather.addChild(brokerChild.getData().url);
                        }
                    }
                    if (brokerFather != null)
                    {
                        brokerChild.addFather(brokerFather.getData().url);
                    }
                }
            }
        }

        public void statusCommand()
        {
            foreach (KeyValuePair<string, SiteNode> node in siteTree)
            {
                BrokerInterface broker = node.Value.siteBroker;
                broker.getStatus();
            }
        }

        public void executeScriptSingleMachine(string filename)
        {
            string[] lines = File.ReadAllLines(filename);
            string url = "";
            for (int i = 0; i < lines.Length; i++)
            {
                string[] words = lines[i].Split();
                if (words[0].Equals("Wait"))
                {
                    int time = Int32.Parse(words[1]);
                    Thread.Sleep(time);
                }
                if (words[0].Equals("Status"))
                {
                    ThreadStart tx = new ThreadStart(this.statusCommand);
                    Thread tq = new Thread(tx);
                    tq.Start();                  
                }
                if (words[0].Equals("Freeze"))
                {
                    string type = "";
                    foreach (KeyValuePair<string, SiteNode> pair in siteTree)
                    {
                        foreach (ProcessConfig process in pair.Value.siteProcecess)
                        {
                            if (process.name.Equals(words[1]))
                            {
                                url = process.url;
                                type = process.procType;
                            }
                        }
                    }
                    if (url != "")
                    {
                        switch (type)
                        {
                            case "broker":
                                BrokerInterface broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url);
                                broker.setFreezeMode();
                                break;
                            case "subscriber":
                                SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                                sub.setFreezeMode();
                                break;
                            case "publisher":
                                PublisherInterface pub = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), url);
                                pub.setFreezeMode();
                                break;
                        }
                    }
                }
                if (words[0].Equals("Unfreeze"))
                {
                    string type = "";
                    foreach (KeyValuePair<string, SiteNode> pair in siteTree)
                    {
                        foreach (ProcessConfig process in pair.Value.siteProcecess)
                        {
                            if (process.name.Equals(words[1]))
                            {
                                url = process.url;
                                type = process.procType;
                            }
                        }
                    }
                    if (url != "")
                    {
                        switch (type)
                        {
                            case "broker":
                                BrokerInterface broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url);
                                broker.setUnfreeze();
                                break;
                            case "subscriber":
                                SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                                sub.setUnfreeze();
                                break;
                            case "publisher":
                                PublisherInterface pub = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), url);
                                pub.setUnfreeze();
                                break;
                        }
                    }
                }
                if (words[0].Equals("Crash"))
                {
                    ProcessConfig processToRemoveFromTreeNode = new ProcessConfig();
                    Process processToRemove = new Process();
                    foreach (SiteNode sitenode in siteTree.Values)
                    {
                        foreach (ProcessConfig process in sitenode.siteProcecess)
                        {
                            if (process.name.Equals(words[1]))
                            {
                                    sitenode.siteBroker.updateProcess(process.procType, words[1],process.url);
                                    processToRemoveFromTreeNode = process;
                            }
                        }
                        sitenode.siteProcecess.Remove(processToRemoveFromTreeNode);
                    }
                    foreach (Process process in processes)
                    {
                        string[] processStartInfo = process.StartInfo.Arguments.Split(' ');
                        if (processStartInfo[0].Equals(words[1])){
                            process.Kill();
                            addLog("A Process Was Killed: " + processStartInfo[0]);
                            processToRemove=process;
                        }
                    }
                    processes.Remove(processToRemove);

                }
                if (words.Length > 2)
                {
                    foreach (KeyValuePair<string, SiteNode> pair in siteTree)
                    {
                        foreach (ProcessConfig process in pair.Value.siteProcecess)
                        {
                            if (process.name.Equals(words[1]))
                            {
                                url = process.url;
                            }
                        }
                    }
                }
                if (url != "" && words.Length>2)
                {
                    switch (words[2])
                    {
                        case "Subscribe":
                            SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                            subscribeParameters parameters = new subscribeParameters();
                            parameters.processName = words[1];
                            parameters.topicName = words[3];
                            parameters.type = words[2];
                            //se houver tempo para p thread, criar buffer etccc
                            sub.AddSubscription(parameters);
                            break;
                        case "Unsubscribe":
                            SubscriberInterface sub2 = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                            subscribeParameters parameters3 = new subscribeParameters();
                            parameters3.processName = words[1];
                            parameters3.topicName = words[3];
                            parameters3.type = words[2];
                            sub2.RemoveSubscription(parameters3);
                            break;
                        case "Publish":
                            publisherParameters parameters2 = new publisherParameters();
                            parameters2.urlPublisher = url;
                            parameters2.processName = words[1];
                            parameters2.numberEvents = words[3];
                            parameters2.topicName = words[5];
                            parameters2.intervaloTempo = words[7];
                            parameters2.type = words[2];
                            PublisherInterface pub = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), url);
                            pub.addPublication(parameters2);
                            break;
                    }
                }            
            }
        }

        public void sendCommandSingleMachine(string command)
        {
            string url = "";
            string[] words = command.Split();
                if (words[0].Equals("Wait"))
                {
                    int time = Int32.Parse(words[1]);
                    Thread.Sleep(time);
                }
                if (words[0].Equals("Status"))
                {
                    ThreadStart tx = new ThreadStart(this.statusCommand);
                    Thread tq = new Thread(tx);
                    tq.Start();
                }
                if (words[0].Equals("Freeze"))
                {
                    string type = "";
                    foreach (KeyValuePair<string, SiteNode> pair in siteTree)
                    {
                        foreach (ProcessConfig process in pair.Value.siteProcecess)
                        {
                            if (process.name.Equals(words[1]))
                            {
                                url = process.url;
                                type = process.procType;
                            }
                        }
                    }
                    if (url != "")
                    {
                        switch (type)
                        {
                            case "broker":
                                BrokerInterface broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url);
                                broker.setFreezeMode();
                                break;
                            case "subscriber":
                                SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                                sub.setFreezeMode();
                                break;
                            case "publisher":
                                PublisherInterface pub = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), url);
                                pub.setFreezeMode();
                                break;
                        }
                    }
                }
                if (words[0].Equals("Unfreeze"))
                {
                    string type = "";
                    foreach (KeyValuePair<string, SiteNode> pair in siteTree)
                    {
                        foreach (ProcessConfig process in pair.Value.siteProcecess)
                        {
                            if (process.name.Equals(words[1]))
                            {
                                url = process.url;
                                type = process.procType;
                            }
                        }
                    }
                    if (url != "")
                    {
                        switch (type)
                        {
                            case "broker":
                                BrokerInterface broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url);
                                broker.setUnfreeze();
                                break;
                            case "subscriber":
                                SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                                sub.setUnfreeze();
                                break;
                            case "publisher":
                                PublisherInterface pub = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), url);
                                pub.setUnfreeze();
                                break;
                        }
                    }
                }
            if (words[0].Equals("Crash"))
            {
                ProcessConfig processToRemoveFromTreeNode = new ProcessConfig();
                Process processToRemove = new Process();
                foreach (SiteNode sitenode in siteTree.Values)
                {
                    foreach (ProcessConfig process in sitenode.siteProcecess)
                    {
                        if (process.name.Equals(words[1]))
                        {
                            sitenode.siteBroker.updateProcess(process.procType, words[1], process.url);
                            processToRemoveFromTreeNode = process;
                        }
                    }
                    sitenode.siteProcecess.Remove(processToRemoveFromTreeNode);
                }
                foreach (Process process in processes)
                {
                    string[] processStartInfo = process.StartInfo.Arguments.Split(' ');
                    if (processStartInfo[0].Equals(words[1]))
                    {
                        process.Kill();
                        addLog("A Process Was Killed: " + processStartInfo[0]);
                        processToRemove = process;
                    }
                }
                processes.Remove(processToRemove);

            }
            if (words.Length > 2)
                {
                    foreach (KeyValuePair<string, SiteNode> pair in siteTree)
                    {
                        foreach (ProcessConfig process in pair.Value.siteProcecess)
                        {
                            if (process.name.Equals(words[1]))
                            {
                                url = process.url;
                            }
                        }
                    }
                }
                if (url != "" && words.Length > 2)
                {
                    switch (words[2])
                    {
                        case "Subscribe":
                            SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                            subscribeParameters parameters = new subscribeParameters();
                            parameters.processName = words[1];
                            parameters.topicName = words[3];
                            parameters.type = words[2];
                            sub.AddSubscription(parameters);
                            break;
                        case "Unsubscribe":
                            SubscriberInterface sub2 = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), url);
                            subscribeParameters parameters3 = new subscribeParameters();
                            parameters3.processName = words[1];
                            parameters3.topicName = words[3];
                            parameters3.type = words[2];
                            sub2.RemoveSubscription(parameters3);
                            break;
                        case "Publish":
                            publisherParameters parameters2 = new publisherParameters();
                            parameters2.urlPublisher = url;
                            parameters2.processName = words[1];
                            parameters2.numberEvents = words[3];
                            parameters2.topicName = words[5];
                            parameters2.intervaloTempo = words[7];
                            parameters2.type = words[2];
                            PublisherInterface pub = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), url);
                            pub.addPublication(parameters2);
                            break;
                    }
                }
        }
        // ----------------------------------------------------------------------------------//
    }

    public class RemotePMService : MarshalByRefObject, PMServerInterface
    {
        public DelAddMsgToLog remoteDelegateAddMsgToLog;
        public DelAddMsgToLog sendMsgToSlaves;
        public DelRegisterSlave registerSlave;
        public DelGetInfo myFather;
        public DelAddMsgToLog logDelivery;

        public RemotePMService(){}
        public void RegisterSlaves(SiteNode args){ registerSlave(args); }
        public void MsgToMaster(string message){ remoteDelegateAddMsgToLog(message); }
        public void MsgToSlaves(string message){ sendMsgToSlaves(message); }
        public string GetMySlaveFatherUrl(string father) { return myFather(father); }
        public void addLog(string log) { logDelivery(log); }
    }
}
