# 7. Opcua communication stations

Date: 2025-11-12

## Status

Accepted

## Context

In the Fleetmanager we need to log statuses and errors of stations.
Gebhardt uses OPCUA to transport information about PLCs, conveyors etc statuses.
A reference for this is the [Gebhardt Visu OpCUA communication project](https://dev.azure.com/GebhardtGroup/StoreWare/_git/StoreWareDev?path=/Source/VisuOpcUa/CommunicationServer)
There currently, the [PLCcom Opc Ua Client sdk](https://www.indi-an.com/de/plccom/opc-ua-sdk/opcua-uebersicht/?gad_source=1&gad_campaignid=21741220789&gbraid=0AAAAADo_jeVLNWOqhzszA_eA6DqPmiatW&gclid=CjwKCAiAt8bIBhBpEiwAzH1w6Qw2c1vg2UblmJMNlAnMJzdXBT0SZzyvaAIwtfiRZahD6_NeMH4V3xoC6SQQAvD_BwE)
is used for client implementations, see, e.g. [here](https://dev.azure.com/GebhardtGroup/StoreWare/_git/StoreWareDev?path=/Source/VisuOpcUa/CommunicationServer/OpcUaSignalRBackgroundService.cs)
the current license can be found [here](https://dev.azure.com/GebhardtGroup/StoreWare/_git/StoreWareDev?path=/Source/VisuOpcUa/CommunicationServer/App.config&version=GBdevelop&line=19&lineEnd=20&lineStartColumn=1&lineEndColumn=67&lineStyle=plain&_a=contents)

There are standard data blocks (see pdf in Teams channel-> files-> OPCUA) implemented, for which certain parts are configured via excel tables.
Relevant data blocks are:
- DB_Visu_error_messages
- ?


Companion specs and nodes needed are generated via VisuOpcua from the excel files
located in [global definitions](https://dev.azure.com/GebhardtGroup/StoreWare/_git/StoreWareDev?path=/Source/VisuOpcUa/Database/ExcelConfigurationFiles/GlobalDefinitions)


For unit and integration testing of client implementations Microsoft's
[OPC PLC server](https://github.com/Azure-Samples/iot-edge-opc-plc) 
can be used, which provides both a docker image and a nuget package to allow
integration and unit testing and generates random data.
This however mostly allows technical testing of the client implementation.
The specs needed can only be provided as nodelists, not e.g. nodeset XML files.

## Decision

- New common project Common.OpcuaCommuniation
- To be independent of the actual OPCUA package (here PLCcom Opc Ua Client Sdk), provide an abstraction in common for 
Client etc
- Use PLCcom Opc Ua Client Sdk for actual implementation
- Use OPC PLC server from Microsoft for testing


## Consequences

The test server and the abstraction allows development independent of the team that provides the
final OPCUA server just using a nodelist configuration according to the Gebhardt specification.
However, until the PLC team have not provided the specs used for the stations, just a general
implementation similar to what the Visu-PLC interface spec and the visu opcua communication
code shows can be implemented.

