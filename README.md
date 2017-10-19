# SESDAD

Simplified implementation of a reliable, distributed, message broker supporting the publish-subscribe paradigm. The publish-subscribe system involves 3 types of processes: `publishers`, `subscribers`, and `message brokers`. 

- Publishers are processes that produce events on one or more topics. Multiple publishers may publish events on the same topic. 
- Subscribers register their interest in receiving events of a given set of topics. 
- Brokers organize themselves in a overlay network, to route events from the publishers to the interested subscribers.

Communication among publishers and subscribers is `indirect`, via the network of brokers. Both publishers and subscribers connect to one broker (typically, the `nearest` broker in terms of network latency) and send/receive events to/from that broker. Brokers coordinate among each other to propagate events in the overlay network.

* Event routing polices:
	- Flooding: In this approach events are broadcast across the tree.
	- Filtering based: In this approach, events are only forwarded only along paths leading to interested subscribers. To this end, brokers maintain information regarding which events should be forwarded to their neighbors.

* Ordering guarantees:
	- Total order: all events published with total order guarantees are delivered in the same order at all matching subscribers. More formally, if two subscribers s1,s2 deliver events e1,e2, s1 and s2 deliver e1 and e2 in the same order. This ordering property is established on all events published with total order guarantee, independently of the identity of the producer and of the topic of the event.
	- FIFO order: all events published with FIFO order guarantee by a publiser p are deliv- ered in the same order according to which p published them.
	- No ordering: as the name suggests, no guarantee is provided on the order of notification of events.

## How to run

- Execute Puppet_Master.exe
- Launch N Puppet_Slave.exe using the console, where N is the number of "Sites"
- A form should pop-up for each Slave executed:
	- Click on the `Connect` button on each Slave to establish a TCP Channel with the Puppet_Master on default port (`8086`)
	- Fill the form of each Slave according to the "Site" information given on the configuration file (`/Puppet_Master/bin/Debug/configMulti.txt` or `configSingle.txt`) and click on the `SetInfo` button.
- To read the configuration file, fill the `Config` text box and click on the `SetConfig` button. 
- To run a script, fill the `Script name` text box on the Puppet_Slave form and click on the `Run Script` button.
- To enter a command, fill the `Command` text box with the command and click on the `Send` button.


