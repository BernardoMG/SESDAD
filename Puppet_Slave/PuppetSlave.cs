using System;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Collections.Generic;
using System.Diagnostics;
using Reference_DLL;

namespace Projecto_DAD
{
    public class PuppetSlave
    {
        public SlaveRegisterForm form;
        public RemotePSlaveService remoteSlaveService;
        public PMServerInterface remotePMservice;
        public string urlFather;
        public PSlavesInterface father;
        public TcpChannel channel;
        public SiteNode ownData;
        public int slavePort;
        private BrokerInterface remoteBroker;
        private Dictionary<string, string> brokersRegistry;
        private Dictionary<string, string> allProcess;
        private List<Process> allSlaveProcesses;

        public PuppetSlave()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            brokersRegistry = new Dictionary<string, string>();
            allProcess = new Dictionary<string, string>();
            ownData = new SiteNode();        
            form = new SlaveRegisterForm();
            channel = new TcpChannel();
            urlFather = "";
            remoteSlaveService = new RemotePSlaveService();
            allSlaveProcesses = new List<Process>();
            RemotingServices.Marshal(remoteSlaveService, "Slave",
             typeof(RemotePSlaveService));
            ChannelServices.RegisterChannel(channel, false);       
            form.addMsgToLog += new DelAddMsgToLog(addMsgToLog);
            form.setServer += new DelSetServerInterface(setServerInterface);
            form.registerSlave += new DelRegisterSlave(saveSlaveData);
            form.setTCP += new DelsSetTCPChanel(setTCPChanel);
            remoteSlaveService.sendMsgToLog += new DelAddMsgToLog(addMsgToLog);
            remoteSlaveService.getData += new DelGetSlaveData(getData);
            remoteSlaveService.start += new DelStartProcess(startProcess);
            remoteSlaveService.registerBroker += new DelStartProcess(registerBroker);
            remoteSlaveService.handShake += new DelRelation(fatherHandShake);
            remoteSlaveService.myUrlBroker += new DelSomeInfo(getBrokerUrl);
            remoteSlaveService.addFather += new DelRelation(childHandShake);
            remoteSlaveService.myFatherUrl += new DelSomeInfo(getMyFatherUrl);
            remoteSlaveService.searchProcess += new DelGetInfo(verifyProcessName);
            remoteSlaveService.deliveryMaster += new DelAddMsgToLog(addMsgToLogMaster);
            remoteSlaveService.status += new DelIniciateRelation(getStatus);
            remoteSlaveService.removeProcess += new DelFlooding(removeProcessFromList);
        }

        [STAThread]
        static void Main(String[] args)
        {
            PuppetSlave PSlave = new PuppetSlave();
            Application.Run(PSlave.form);
        }

        public Dictionary<string, string> getBrokersRegistry() { return this.brokersRegistry; }

        public void addMsgToLog(string message)
        {
            form.Invoke(new DelAddMsgToLog(form.sendMessageToLog), message);
        }

        public void addMsgToLogMaster(string log)
        {
            remotePMservice.addLog(log);
        }

        public void saveSlaveData(SiteNode args)
        {
            ownData.name = args.name;
            ownData.father = args.father;
            ownData.port = args.port;
            remotePMservice.RegisterSlaves(args);         
        }

        public string getMyFatherUrl()
        {
            urlFather = remotePMservice.GetMySlaveFatherUrl(ownData.father);
            return urlFather;
        }

        public string verifyProcessName(string nameProcess)
        {
            string url="";
            foreach(string name in allProcess.Keys)
            {
                if (name.Equals(nameProcess))
                {
                    url = allProcess[name];
                    return url;
                }
            }
            return url;
        }

        public void getStatus()
        {
            if (remoteBroker != null)
            {
                addMsgToLog("Broker " + remoteBroker.getData().name + " is active");
                remoteBroker.getStatus();
            }
            else
                addMsgToLog("This node doesn't have a broker");
        }

        public void fatherHandShake(string urlBrokerFather, string urlBrokerChild)
        {
            BrokerInterface childBroker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface),
                                 urlBrokerFather);
            childBroker.addChild(urlBrokerChild);
        }

        public void childHandShake(string urlBrokerChild, string urlBrokerFather)
        {
            BrokerInterface fatherBroker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface),
                  urlBrokerChild);
            fatherBroker.addFather(urlBrokerFather);
        }

        public string getBrokerUrl()
        {
            string aux = "";
            foreach (string s in brokersRegistry.Values) { 
                 aux = s;
            }
           return aux;
        }

        public SiteNode getData()
        {
            return this.ownData;
        }

        public void setTCPChanel(string port)
        {
            ChannelServices.UnregisterChannel(channel);
            int _port = Int32.Parse(port);
            channel = new TcpChannel(_port);
            ChannelServices.RegisterChannel(channel, false);
        }

        public void startProcess(ProcessConfig config)
        {
            string path;
            switch (config.procType)
            {
                case "broker":
                    string args = config.name + " " + config.url + " " + ownData.port+" "+config.orderType+" "+config.loggingLevelType+" "+config.routingPolicy;
                    brokersRegistry.Add(config.name, config.url);
                    allProcess.Add(config.name, config.url);
                    BuildPaths.PPath = "\\Broker\\bin\\Debug\\Broker.exe";
                    path = BuildPaths.PPath;

                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = path;
                    startInfo.Arguments = args;
                    Process brokerProcess = Process.Start(startInfo);
                    allSlaveProcesses.Add(brokerProcess);
                    break;
                case "publisher":
                    foreach (string b in brokersRegistry.Values)
                    {
                        string args2 = config.name + " " + config.url + " " + b + " " + config.orderType;
                        allProcess.Add(config.name, config.url);
                        path = BuildPaths.getPath();
                        BuildPaths.PPath = "\\Publisher\\bin\\Debug\\Publisher.exe";
                        path = BuildPaths.PPath;

                        ProcessStartInfo publisherStartInfo = new ProcessStartInfo();
                        publisherStartInfo.FileName = path;
                        publisherStartInfo.Arguments = args2;
                        Process publisherProcess = Process.Start(publisherStartInfo);
                        allSlaveProcesses.Add(publisherProcess);
                    }
                    break;
                case "subscriber":
                    foreach (string  b in brokersRegistry.Values)
                    {
                        string args3 = config.name + " " + config.url + " " + b;
                        allProcess.Add(config.name, config.url);
                        path = BuildPaths.getPath();
                        BuildPaths.PPath = "\\Subscriber\\bin\\Debug\\Subscriber.exe";
                        path = BuildPaths.PPath;
                        ProcessStartInfo subscriberStartInfo = new ProcessStartInfo();
                        subscriberStartInfo.FileName = path;
                        subscriberStartInfo.Arguments = args3;
                        Process subscriberProcess = Process.Start(subscriberStartInfo);
                        allSlaveProcesses.Add(subscriberProcess);
                    }
                    break;
            }
        }

        public void registerBroker(ProcessConfig config)
        {
            BrokerInterface newBroker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface),
                    config.url);
            addMsgToLog("Connected ..." + "tcp://localhost:" + config.Port + "/" + config.name);
            remoteBroker=newBroker;
        }

        public void setServerInterface()
        {
            PMServerInterface remotePMservice = (PMServerInterface)Activator.
                GetObject(typeof(PMServerInterface),
                "tcp://localhost:8086/RemotePMService");
            addMsgToLog("Connected: " + "tcp://localhost:8086/RemotePMService");
            this.remotePMservice = remotePMservice;
            this.remotePMservice.MsgToMaster("Connection Estabilshed");
        }

        public void removeProcessFromList(string processName, string type, string processUrl)
        {
            Process processToRemove = new Process();
            bool found = false;
            foreach (Process process in allSlaveProcesses)
            {
                string[] processStartInfo = process.StartInfo.Arguments.Split(' ');
                if (processStartInfo[0].Equals(processName))
                {
                    remoteBroker.updateProcess(type, processName, processUrl);
                    process.Kill();
                    addMsgToLog("A Process Was Killed: " + processStartInfo[0]);
                    processToRemove = process;
                    found = true;
                }
            }
            if(found)
                allSlaveProcesses.Remove(processToRemove);
        }
    }

    public class RemotePSlaveService : MarshalByRefObject, PSlavesInterface
    {
        public DelAddMsgToLog sendMsgToLog;
        public DelGetSlaveData getData;
        public DelStartProcess start;
        public DelStartProcess registerBroker;
        public DelRelation handShake;
        public DelSomeInfo myUrlBroker;
        public DelRelation addFather;
        public DelSomeInfo myFatherUrl;
        public DelGetInfo searchProcess;
        public DelAddMsgToLog deliveryMaster;
        public DelIniciateRelation status;
        public DelFlooding removeProcess;

        public RemotePSlaveService() { }
        public void RegisterBroker(ProcessConfig config){ registerBroker(config); }
        public void msgToSlave(string message){ sendMsgToLog(message); }
        public SiteNode getSlaveData(){ return getData(); }
        public void startProcess(ProcessConfig conf) { start(conf); }
        public string getBrokerUrl() { return myUrlBroker(); }
        public string getMyFatherUrl() { return myFatherUrl(); }        
        public void fatherHandShake(string urlBrokerFather, string urlBrokerChild) { handShake(urlBrokerFather, urlBrokerChild); }
        public void childHandShake(string urlBrokerChild, string urlBrokerFather) { addFather(urlBrokerChild, urlBrokerFather); }
        public string verifyProcessName(string name) { return searchProcess(name); }
        public void addMsgToLogMaster(string log) { deliveryMaster(log); }
        public void getStatus() { status(); }
        public void removeProcessFromList(string processName, string type, string processUrl) { removeProcess(processName, type, processUrl); }
    }
}
