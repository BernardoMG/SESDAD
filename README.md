# SESDAD

Simplified implementation of a reliable, distributed, message broker supporting the publish-subscribe paradigm. The publish-subscribe system involves 3 types of processes: `publishers`, `subscribers`, and `message brokers`. 

- Publishers are processes that produce events on one or more topics. Multiple publishers may publish events on the same topic. 
- Subscribers register their interest in receiving events of a given set of topics. 
- Brokers organize themselves in a overlay network, to route events from the publishers to the interested subscribers.

Communication among publishers and subscribers is `indirect`, via the network of brokers. Both publishers and subscribers connect to one broker (typically, the `nearest` broker in terms of network latency) and send/receive events to/from that broker. Brokers coordinate among each other to propagate events in the overlay network.

## How to run

- Execute Puppet_Master.exe
- Launch N Puppet_Slave.exe using the console, where N is the number of "Sites"
- A form should pop-up for each Slave executed:
	- Click on the `Connect` button on each Slave to establish a TCP Channel with the Puppet_Master on default port (`8086`)
	- Fill the form of each Slave according to the "Site" information given on the configuration file (`/Projecto_DAD/Puppet_Master/bin/Debug/configMulti.txt` or `configSingle.txt`) and click on the `SetInfo` button.
- To read the configuration file, fill the `Config` text box and click on the `SetConfig` button. 
- To run a script, fill the `Script name` text box on the Puppet_Slave form and click on the `Run Script` button.
- To enter a command, fill the `Command` text box with the command and click on the `Send` button.


