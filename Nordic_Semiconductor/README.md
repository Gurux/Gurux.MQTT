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

*Gurux.Bridge
Gurux.Bridge connects your meter to the MQTT broker. It sends received data from the meter to the Broker and vice versa.

Simple example
=========================== 

You can check this [video](https://youtu.be/x03-UK_-cic)

In this example we are using iot.eclipse.org as a broker. You can use other brokers as well.

First compile and start Gurux.Bridge for Nordic Semiconductor. You don't need to do anything. You can access the Bridge using device name as a topic.

Next start GXDLMSDirector and create new device. Select MQTT as media type. Update broker address and port number and topic.
Select topic of the meter where you want to make a connection from the Bridge output and paste it to the topic field.

If your settings are correct, you can now read your meter.


Trouble shooting
=========================== 
If you have problems first check that topic of the Bridge and broker address are correct.
