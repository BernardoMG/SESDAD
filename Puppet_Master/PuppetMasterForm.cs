using System;
using System.Windows.Forms;
using Reference_DLL;
using System.Collections.Generic;
using System.Threading;

namespace Projecto_DAD
{
    public partial class MasterForm : Form
    {
        public DelAddMsgToLog formDelegateAddToLog;
        public DelGetSlaveUrl delGetSlaveUrl;
        public DelAddMsgToLog verificaMaquina;
        public DelIniciateRelation addRelation;
        public DelAddMsgToLog sendCommand;
        public DelAddMsgToLog readScript;
        public List<string> scriptsNames;

        public MasterForm()
        {     
            InitializeComponent();
            scriptsNames = new List<string>();
        }

        public void sendMessageToLog1(string message)
        {
            this.LogBox.AppendText("\r\n" + message);
        }

        public void sendMessageToLog2(string message)
        { 
            this.slavesInfoLog.AppendText("\r\n" + message);
        }

        private void sendCommandToSlave(object sender, EventArgs e)
        {
            if (!messageBox.Text.Contains(".txt"))
                sendCommand(messageBox.Text);
            else
            {
                scriptsNames.Add(messageBox.Text);
                ThreadStart ts = new ThreadStart(this.executeScript);
                Thread t = new Thread(ts);
                t.Start();
            }            
        }

        private void executeScript()
        {
            string filename;
            lock (this)
            {
                filename = scriptsNames[scriptsNames.Count - 1];
            }
            readScript(filename);
        }
            
        private void runScriptButton_Click(object sender, EventArgs e)
        {
            string name = runScriptText.Text;
            verificaMaquina(name);
            addRelation();
        }

        private void ShowSlavesInfo_Click(object sender, EventArgs e)
        {
            delGetSlaveUrl();
        }
    }
}
