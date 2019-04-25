See An [Gurux](http://www.gurux.org/ "Gurux") for an overview.


Join the Gurux Community or follow [@Gurux](https://twitter.com/guruxorg "@Gurux") for project updates.

Open Source MQTT media component, made by Gurux Ltd, is a part of GXMedias set of media components, which programming interfaces help you implement communication by chosen connection type. Our media components also support the following connection types: serial, network, terminal.

For more info check out [Gurux](http://www.gurux.org/ "Gurux").

We are updating documentation on Gurux web page. 

If you have problems you can ask your questions in Gurux [Forum](http://www.gurux.org/forum).

Functionality
=========================== 
Purpose of Gurux MQTT library is allow GXDLMSDirector to access meters where it's not possible to client establish connection to the meter, example when dynamic IP addresses are used.
Gurux MQTT consists three different parts.

* Gurux.MQTT
Gurux.MQTT is media component, used to send data using MQTT broker and bridge to the meter.

* Gurux.Broker
Gurux.Broker is MQTT broker. You can use any MQTT broker you want to.

*Gurux.Bridge
Gurux.Bridge connects your meter to the MQTT broker. It sends received data from the meter to the Broker and vice versa.


Simple example
=========================== 

First you need to start Gurux.Broker. You can use other brokers as well, but in this example we use local broker. Start broker running "dotnet Gurux.Broker.dll"

Next start Gurux.Bridge. You do it running "dotnet Gurux.Bridge.dll -h broker_address". When you run it first time app creates connections.json-file and closes right away. 
Open connectios.json file. It looks something like:

{
  "BrokerAddress": "localhost",
  "BrokerPort": 1883,
  "Connections": [
    {
      "Name": "Test",
      "Settings": "<ip>localhost</ip><port>4061</port>",
      "Type": "Net" 
    },
    {
      "Settings": "<port>COM1</port>",
      "Type": "Serial",
      "UseOpticalHead": "true"
    }],
  "Name": "eab3395c-8b41-4e75-ac44-a18bce465e31"
}

*Broker Address
Broker Address is TCP/IP address of the broker. Default is localhost.
*BrokerPort
Broker port is TCP/IP port of the broker. Default is 1883.
*Bridge Name 
Address field is unique address of the bridge. Each bridge must have unique address. If you copy file to the other computer change address. 
*MediaName 
Media Name is optional field that you can use to describe your meter. If name is not given meters are defined by index number.
*Type
Type describes type of the meter. At the moment only serial and network connections are supported.
*Settings
Settings field defines used media settings.

After you have set the correct settings you can start the bridge again. You can see bridge and meter topics (address) in the command line output.

Next start GXDLMSDirector and create new device. Select MQTT as media type. Update broker address and port number and topic.
Select topic of the meter where you want to make a connection from the Bridge output and paste it to the topic field.

If your settings are correct, you can now read your meter.


Trouble shooting
=========================== 
If you have problems first check that topic of the Bridge is correct. You can also use command line parameter -t Verbose to show more information from the sent and received messages.
