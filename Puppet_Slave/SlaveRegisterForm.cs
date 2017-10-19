using System;
using System.Windows.Forms;
using Reference_DLL;


namespace Projecto_DAD
{
    public partial class SlaveRegisterForm : Form
    {
        public DelAddMsgToLog addMsgToLog;
        public DelSetServerInterface setServer;
        public DelRegisterSlave registerSlave;
        public DelsSetTCPChanel setTCP;

        public SlaveRegisterForm()
        {
            InitializeComponent();
        }

        public void sendMessageToLog(string s)
        {
            this.showActions.AppendText("\r\n" + s);
        }
      
        private void connect_Click(object sender, EventArgs e)
        { 
            setServer();
        }

        private void register_click(object sender, EventArgs e)
        {
            addMsgToLog("Registering Slaves::");
            setTCP(portBox.Text);
            SiteNode data = new SiteNode();
            data.port = portBox.Text;
            data.name = nameBox.Text;
            data.father = fatherBox.Text;
            registerSlave(data);
        }
    }
}
