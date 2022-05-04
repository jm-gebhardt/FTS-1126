# AGV Master Control Module

This is a master control module for [automated guided vehicles](https://en.wikipedia.org/wiki/Automated_guided_vehicle) (AGV).
It is designed to take HostOrders from Gebhardt StoreWare and send TransportOrders to an AGV.
Orders are managed by the TransportOrder Service e.g. to maximize AGV productivity.
This software is organizes as MicroServices communicating using MassTransit and RabbitMQ.  
All outside communication with an ERP/StoreWare or AGVs is done using separate services.

This projects is using .NET 6 and requires a running [RabbitMQ](https://www.rabbitmq.com/) instance. Install guide can be found in the [wiki](https://dev.azure.com/GebhardtGroup/StoreWare/_wiki/wikis/StoreWare.wiki/134/RabbitMQ)


## Structure
Each Service is build as a separate projects.
```
- Agv
    - Common: Containing all shared items such as Messages, Models, Utils
    - DeviceCommunication: The service is the proxy to the AGVs.
    - TransportOrder: Manage HostOrders and distribute resulting TransportOrders
    - WCSCommunication: Taking HostOrders from StoreWare and sending them to be managed by TransportOrder

```
## Build

```
dotnet build Source/Agv/CompileAgv.sln
```

## Run Simulation
In Bash
```export RABBITMQ__HOST=localhost```
or in Windows Command Line: ```set RABBITMQ__HOST=localhost```
```
dotnet run --project WCSCommunication simulate --interval=10 --sources='S1,S2', --destinations='D1,D2,D3'
dotnet run --project TransportOrder simulate --agvs=2
dotnet run --project DeviceCommunication simulate --interval=5,10
```

## Run UnitTests

```
dotnet test Source/Agv/CompileAgv.sln
```

## Windows Service
The Software ist designed to run as a Windows Service.

### Register Service (Registry):
```
admin> sc.exe create "Gebhardt.StoreWare.Agv.<service>" binpath= "<binary> [options..]"
```
### Remove Service
```
admin> sc.exe delete "Gebhardt.StoreWare.Agv.<service>"
```

Note: If MMC/Taskmgr/ProcExp are running, service removal may not have immediate effect showing and the following message will be shown "Der angegebene Dienst wurde zum Löschen markiert."