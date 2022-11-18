# AGV Master Control Module
This is a master control module for [automated guided vehicles](https://en.wikipedia.org/wiki/Automated_guided_vehicle) (AGV).
It is designed to take HostOrders from Gebhardt StoreWare and send TransportOrders to an AGV.
Orders are managed by the TransportOrder Service e.g. to maximize AGV productivity.
This software is organizes as MicroServices communicating using MassTransit and RabbitMQ.  
All outside communication with an ERP/StoreWare or AGVs is done using separate services.

This projects is using .NET 6 and requires a running [RabbitMQ](https://www.rabbitmq.com/) instance. Install guide can be found in the [wiki](https://dev.azure.com/GebhardtGroup/StoreWare/_wiki/wikis/StoreWare.wiki/134/RabbitMQ)


## Structure
Each Service is build as a separate project.
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

## Expected Configuration Variables (ENVS)
Configurations may be passed in as environment variables or any other configuration provider loaded by [CreateDefaultBuilder](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.host.createdefaultbuilder?view=dotnet-plat-ext-6.0#microsoft-extensions-hosting-host-createdefaultbuilder)

* In Bash: `export RABBITMQ__HOST=localhost`
* In Windows Command Line: `set RABBITMQ__HOST=localhost`



### All Services
```
RABBITMQ__HOST=localhost
```

### WCSCommunication (Production only)
```
CONNECTIONSTRINGS__WCS="Server=127.0.0.1,1433;Database=master;User Id=MyDbUser;Password=MyStrongPassword;"
```

### TransportOrder
```
CONNECTIONSTRINGS__TO="Server=127.0.0.1,1433;Database=master;User Id=MyDbUser;Password=MyStrongPassword;"
```
Database persistence can be disabled using: `--enableInMemoryPersistence`


### Webserver
Each module is hosting a metric server via a Kestrel web host.
The default URL for the Kestral web host is `http:////127.0.0.1:5000`.
In order to run multiple processes, you need to define a seprate listening port for each proceess.
This can be done with the envirnment variable `ASPNETCORE_URLS`.

Typical values:
```
WCSCommunication:
ASPNETCORE_URLS=http://127.0.0.1:5010

TransportOrder:
(use default, ASPNETCORE_URLS not set)

DeviceCommunication:
ASPNETCORE_URLS=http://127.0.0.1:5020
```

#### WebAPI
The TransportOrder module runs a web API in order to monitor the service from outside.
You can access the UI with this URL
```
ASPNETCORE_URLS/swagger (typically http://127.0.0.1:5010/swagger)
```

If you are running the service in docker, you need to expose the swagger URL to the outside via caddy.
```
http://localhost/transportorder/swagger/
```

### WebUI
See the WebUI [README.md](Source/Agv/ui/README.md) for more information


## Provide certificates
Quite now the system is using self-signed certificates.
To create all required certificates and keys the following script need to be executed once:
```
.\Config\create_cert.bat
```
For a detailed description see [Transportsicherheit](https://dev.azure.com/GebhardtGroup/StoreWare/_wiki/wikis/StoreWare.wiki/167/Transportsicherheit-(TLS-Transport-Layer-Security)) in Wiki.


## Run Simulation
Ensure that `RABBITMQ__HOST` is set
```
dotnet run --project WCSCommunication simulate --interval=10000 --sources=S1,S2 --destinations='D1,D2,D3
dotnet run --project TransportOrder production --autoAccept
dotnet run --project DeviceCommunication simulate --sources=S1,S2 --destinations=D1,D2,D3 --agvs=3 --nodetraveltime=5000 --gridrows=4
```


## Run UnitTests
```
dotnet test Source/Agv/CompileAgv.sln
```

## Run Integration Tests
Integration tests are located in `Source/Agv.IntegrationsTest`
```
cd Source/Agv.IntegrationsTest
```
Powershell:
```
$Env:COMPOSE_FILE = Join-Path $pwd "/../../docker-compose.yml"
$Env:MQTT_SAMPLES = Join-Path $pwd "/MqttMessages"
```
Bash:
```
export COMPOSE_FILE=$(pwd)/../../docker-compose.yml
export MQTT_SAMPLES=$(pwd)/MqttMessages
```

Run Tests 
```
dotnet test
```

## Docker

The software is designed to run within docker optionally.
Orchestration is done using ```docker compose```.
You must create a certificate first by running `.\Config\create_cert.bat`

### Docker environment

Docker environment is set using the environment file ```.env```.
There is a template ```.env.template``` that can be used as a template. Copy this file to ```.env``` and set the environment variables provided within that file.

### Build images
```
docker compose build
```

### Run containers
```
docker compose up
```

You can now access the Web Service under: http://localhost

#### Check console logs
```
docker compose logs -f [service (wcscommunication|devicecommunication|transportorder|rabbitmq)]
```

### Database Migrations
First, ensure that a ConnectionString is available as an ENV variable.
#### Create Migrations
Export required connection string, e.g:
````
export CONNECTIONSTRINGS__TO='Server=127.0.0.1;Database=master;User Id=sa;Password=<password>'
````

Apply existing migrations
```
dotnet ef database update
```

Add new migration
```
dotnet ef migrations add <MigrationName> 
```


## Windows Service
The software is designed to run as a Windows Service.

### Register Service (Registry):
```
admin> sc.exe create "Gebhardt.StoreWare.Agv.<service>" binpath= "<binary> [options..]"
```
### Remove Service
```
admin> sc.exe delete "Gebhardt.StoreWare.Agv.<service>"
```

Note: If MMC/Taskmgr/ProcExp are running, service removal may not have immediate effect and the following message will be shown "Der angegebene Dienst wurde zum Löschen markiert."
