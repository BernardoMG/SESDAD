using System;
using System.Collections.Generic;
using System.IO;

namespace Reference_DLL
{
    public interface PMServerInterface
    {
        void RegisterSlaves(SiteNode args);
        void MsgToSlaves(string message);  
        void MsgToMaster(string message);
        string GetMySlaveFatherUrl(string father);
        void addLog(string log);
    }

    public interface PSlavesInterface
    {
        void msgToSlave(string message);
        SiteNode getSlaveData();
        void startProcess(ProcessConfig config);
        void RegisterBroker(ProcessConfig config);
        string getBrokerUrl();
        void fatherHandShake(string urlBroker, string url);
        void childHandShake(string url, string url2);
        string getMyFatherUrl();
        string verifyProcessName(string name);
        void addMsgToLogMaster(string log);
        void getStatus();
        void removeProcessFromList(string processName, string type, string processUrl);
    }

    public interface BrokerInterface
    {
        void RegisterPublisher(ProcessConfig config);
        void RegisterSubscriber(ProcessConfig config);
        ProcessConfig getData();
        void addChild(string url);
        void addFather(string url);
        void UpdateSubscriptionList(subscribeParameters parameters);
        void floodingEvents();
        void queuEvent(publisherParameters parameters);
        void logMsgFromBrokerToMaster(string log);
        void RemoveSubscriptionList(subscribeParameters parameters);
        void getStatus();
        void setFreezeMode();
        void setUnfreeze();
        void subFilter(subscribeParameters parameters);
        void removeSubFilter(subscribeParameters parameters);
        void updateProcess(string type, string name, string url);
        void sendToRoot();
        void queueEventForTotal(publisherParameters publisher);
        void setReplicas(string replica);
        void setBrokerLeader();
    }

    public interface PublisherInterface
    {
        void setFreezeMode();
        void setUnfreeze();
        void addPublication(publisherParameters parameters);
    }

    public interface SubscriberInterface
    {
        void AddSubscription(subscribeParameters parameters);
        void deliverEvent(publisherParameters evento);
        void RemoveSubscription(subscribeParameters parameters);
        void setFreezeMode();
        void setUnfreeze();
    }

    [Serializable]
    public static class BuildPaths
    {
        public static string processPath;
        public static string PPath
        {
            get { return processPath; }
            set { processPath = getPath() + value; }    
        }
        public static string getPath()
        {
            string path_ini = Directory.GetCurrentDirectory();
            string path_1 = Directory.GetParent(path_ini).ToString();
            string path_2 = Directory.GetParent(path_1).ToString();
            processPath = Directory.GetParent(path_2).ToString();
            return processPath;
        }
    }

    [Serializable]
    public struct ProcessConfig
    {
        public string site;
        public string creatorPort;
        public string url;
        public string name;
        public string procType;
        public string port;
        public string orderType;
        public string loggingLevelType;
        public string routingPolicy;

        public string URL
        {
            get { return "tcp://localhost:" + this.port + "/" + this.name; }
            set { this.url = value; }
        }

        public string Port
        {
            get
            {
                string[] brokerURLSplit = url.Split(':');
                string[] brokerURLSplit2 = brokerURLSplit[2].Split('/');
                return brokerURLSplit2[0]; 
            }
        }
    }

    [Serializable]
    public struct dummyStruct
    {
        public List<string> urlRoutingTable;
        public List<string> urlDeadRoutingTable;
        public publisherParameters publish;
    }

    [Serializable]
    public class SiteNode
    {
        public string port;
        public string name;
        public string father;
        public BrokerInterface broker;
        public List<ProcessConfig> siteProcecess;

        public BrokerInterface siteBroker
        {
            set { this.broker = value; }
            get { return broker; }
        }
        public SiteNode()
        {
            siteProcecess = new List<ProcessConfig>();
        }
        public string Url
        {
            get { return "tcp://localhost:" + port + "/Slave" ; }
        }
    }

    [Serializable]
    public class commandParameters
    {
        public string type;
        public string processName;
        public string topicName;
    }

   [Serializable]
    public class subscribeParameters : commandParameters
    {
        public string urlSubCreator;
        public string urlBroker;
        public string urlBrokerSender;
    }

    [Serializable]
    public class publisherParameters : commandParameters
    {
        public string numberEvents;
        public string urlPublisher;
        public int sequenceNumber;
        public string intervaloTempo;
        public string urlBroker;
        public string urlBrokerSender;
    }

    [Serializable]
    public class routingEvents
    {
        public string UrlBrokerSender;
        public string UrlSubscriberOnTopic;
        public string Topic;
    }

    public delegate void DelAddMsgToLog(string msg);
    public delegate void DelRegisterSlave(SiteNode args);
    public delegate void DelSetServerInterface();
    public delegate void DelGetSlaveUrl();
    public delegate void DelsSetTCPChanel(string port);
    public delegate SiteNode DelGetSlaveData();
    public delegate ProcessConfig DelGetProcessData();
    public delegate void DelStartProcess(ProcessConfig conf);
    public delegate string DelGetInfo(string info);
    public delegate string DelSomeInfo();
    public delegate void DelIniciateRelation();
    public delegate void DelRelation(string msg, string msg2);
    public delegate void DelParametersSub(subscribeParameters parameters);
    public delegate void DelParametersPub(publisherParameters parameters);
    public delegate void DelLogMaster(publisherParameters parameters);
    public delegate void DelFlooding(string msg, string msg1, string msg2);
    public delegate int DelGlobalSequenceNumber();
    public delegate void DelSetLeader();
    public delegate void DelAddReplica(string replica);
}
