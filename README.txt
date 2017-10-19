Instructions for running Project_SESDAD:

1 - Execute Puppet_Master.exe.
2 - Launch N Puppet_Slave.exe, where N is the number of "Sites", from the console.
2.1 - A Form should pop-up for each Slave executed:
2.2 - Click the "Connect" button on each Slave to estabilsh a TCP Channel with the Puppet_Master, on predifined port: 8086.
2.3 - Fill the form for each slave according to the site information given on the config ("/Projecto_DAD/Puppet_Master/bin/Debug/config.txt") file and click on the "SetInfo" button.
3 - To read the configuration file, fill the "Config" text box, and click the "SetConfig" button. 
3 - To run a script, fill the "Script name" text box, on the Puppet_Slave form, and click the "Run Script" button.
4 - To enter a command, fill the "Command" text box with a command, and click the "Send" button.

Accomplished project goals:
 
   1) Commands:

    - Publish.
    - Subscribe.
    - Unsubscribe.
    - Wait.
    - Status.
    - Freeze ( only freezes the Broker, the rest will be implemented in the final solution).
    - Unfreeze ( only freezes the Broker, the rest will be implemented in the final solution).

   2) Ordering:
	
     - NO (ORDER).
     - FIFO.
     
   3) Routing Policies:
    
     - FLOOD.
     - FILTERING.
       
