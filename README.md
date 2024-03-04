# AGV Master Control Module
This is a master control module for [automated guided vehicles](https://en.wikipedia.org/wiki/Automated_guided_vehicle) (AGV).
It is designed to take HostOrders from Gebhardt StoreWare and send TransportOrders to an AGV.
Orders are managed by the TransportOrder Service e.g. to maximize AGV productivity.
This software is organizes as MicroServices communicating using MassTransit and RabbitMQ.  
All outside communication with an ERP/StoreWare or AGVs is done using separate services.

This projects is using .NET 8 and requires a running [RabbitMQ](https://www.rabbitmq.com/) instance. Install guide can be found in the [wiki](https://dev.azure.com/GebhardtGroup/StoreWare/_wiki/wikis/StoreWare.wiki/134/RabbitMQ)


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
CONNECTIONSTRINGS__WCS="Server=127.0.0.1,1433;Database=master;User Id=MyDbUser;Password=MyStrongPassword;Encrypt=False"
```

### TransportOrder
```
CONNECTIONSTRINGS__AMC="Server=127.0.0.1,1433;Database=master;User Id=MyDbUser;Password=MyStrongPassword;Encrypt=False"
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

#### REST API
The TransportOrder module runs a REST API in order to monitor the service from outside.
You can access the UI with this URL
```
ASPNETCORE_URLS/swagger (typically http://127.0.0.1:5010/swagger)
```

If you are running the service in docker, you need to expose the swagger URL to the outside via caddy.
```
http://localhost/transportorder/swagger/
```

## Run Simulation
Ensure that `RABBITMQ__HOST` is set
The simulation mode offers two types of graph generators, `--gridgraph` and `--fixedgraph`.
In a simulation run with the `--gridgraph` graph generator the graph uses only direct
edges and generates a graph based on the `--sources, --destinations and --gridrows` options:

```
dotnet run --project WCSCommunication simulate --interval=10000 --sources=S1,S2 --destinations='D1,D2,D3
dotnet run --project TransportOrder production --autoAccept
dotnet run --project DeviceCommunication simulate --sources=S1,S2 --destinations=D1,D2,D3 --agvs=3 --nodetraveltime=5000 --gridrows=4 --gridgraph
```
A simulation run with the `-fixedgraph` graph generator uses a pre-defined fixed graph that includes all supported edge types:

```
dotnet run --project WCSCommunication simulate --interval=10000  --sources=P-Drill,P-Grinder,P-Milling --destinations=D-Paint,D-Press,D-Saw
dotnet run --project TransportOrder production --autoAccept
dotnet run --project DeviceCommunication simulate  --agvs=3 --nodetraveltime=5000 --fixedgraph
```
## Run UnitTests
```
dotnet test Source/Agv/CompileAgv.sln
```

## Run Integration Tests
Integration tests are using docker to run. For running the integrations tests make sure to update the container images.
The docker-compose file is located in a different repository: [AgvDevComb](https://dev.azure.com/GebhardtGroup/StoreWare/_git/AgvDevComb)
This repository is a submodule of this repository.
It is required to run the integration  tests
```
cd <projectFolder>
docker compose build
```

Integration tests are located in `Source/Agv.IntegrationsTest`
```
cd Source/Agv.IntegrationsTest
```

The following environment is required:  
Powershell:
```
$Env:COMPOSE_FILE_IntegrationTest = Join-Path $pwd "/../../docker-compose.yml"
$Env:COMPOSE_FILE_VirtualAGV = Join-Path $pwd "/../Agv.Karis/docker-compose.yml"
$Env:MQTT_SAMPLES = Join-Path $pwd "/MqttMessages"
$Env:DockerRegistryPassword = "ThisIsTheHighSecurityPasswordWhatYouCanGetFromYourPO"
```
Bash:
```
export COMPOSE_FILE_IntegrationTest=$(pwd)/../../docker-compose.yml
export COMPOSE_FILE_VirtualAGV=$(pwd)/../Agv.Karis/docker-compose.yml
export MQTT_SAMPLES=$(pwd)/MqttMessages
export DockerRegistryPassword=ThisIsTheHighSecurityPasswordWhatYouCanGetFromYourPO 
```
TAR provides the Karis AGV's image through their own Registry. The password of this Docker Registry can be obtained by the Product Owner.

Run Tests 
```
dotnet test
```

It may fail to download the duplicated NuGet packages from the Gebhardt gallary.
For this case temporarily delete  
`Source/Agv.IntegrationsTest/NuGet.Config`  
in your local source repository for downloading the NuGet packages from it's origin.  
Make sure the keep `NuGet.Config` in the published Git repository!

### Database Migrations
Please refer to: https://dev.azure.com/GebhardtGroup/StoreWare/_wiki/wikis/StoreWare.wiki/541/Entity-Framework#

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

# Settings
The Settings construct builds on Microsoft's IConfiguration.
Using the settings:
```
var settings = settingsProvider.GetSettings();
```
There are several layers that serve as a source for the settings.
If a layer has no settings stored, it is the turn of the layer below:

- DatabaseSettingsProvider
- DefaultSettingsProvider (hardcoded default values)

In general, the default values of all settings classes are provided by the DefaultSettingsProvider. In the following case it's the "myValue":
```
public record TestConfiguration : SettingsBase<TestConfiguration>
    {
        public string TestProperty { get; set; } = "myValue";
    }
```

If you want to override this value by the database (of the layer above), create an entry in the "ConfigurationSetting" table with
Key= TestConfiguration:TestProperty
Value = "differentValue"
this will be the value provided by the SettingsProvider, because the database layer overrides the default layer.
In general, the default value is created in the database.
The settingsProvider is automatically registered and you can have it injected via DI:
```ISettingsProvider<TestConfiguration>```

## Introduction of a new setting
1. Introduce the new settings class, which derives from SettingsBase
2. Get ISettingsProvider<T> injected into the constructor by DI (it's automatically registered)
3. Use GetSettings() method to get the current settings: ```private string Settings => settingsProvider.GetSettings();```

## Options
### Default seeding
By default, all settings are written to the configuration database table with the default values.
If this is not desired, it can be disabled for the whole class or on property level by using the attribute, e.g.:
```[AutoSeedToDatabase(false)]```
### SectionName
By default, the name of the setting class is used as key, e.g. as key in the database, you can overwrite this in the settings class:
```public override string SectionName => "Test";```


## Background Map Image Setup

### Storing and Deploying the PNG

- The PNG image is stored within the project's resources and is deployed during the build process (`Resources\BackgroundImages\*.png`).
- This image can be replaced post-deployment to update the map background.

### File Naming Convention

To properly align the map graphic with the graph, the scaling factor, x-offset, and y-offset need to be manually determined and then encoded into the filename. The structure of the filename should follow this pattern:

`mapName_scaleUp_xOffset_yOffset.png`

#### Example

- Filename: `stations_1.2_-10_-22.png`
- Name of the map: `stations`
- Upscale factor: `1.2` (greater than the graph scale)
- x-offset: `-10` (units in the graph coordinate system)
- y-offset: `-22` (units in the graph coordinate system)

### Fallback Mechanism

- In cases where these values are not defined in the filename, such as with a file named `stations.png`, default values are used: upscale factor (`1`), x-offset (`0`), and y-offset (`0`).
- If no image file is found for the specified map name, the background image is generated automatically.

# Provisioning
For provisioning, you need a fully running system.
You need some environment variables to be set, e.g. RabbitMQ connection.
The data is loaded from the data folder, but the path can be specified by IConfiguration.
You find an example structure in the Provisioning/examples/ folder.

### Agvs
To provision an Agv, you must store the desired data under data\{AgvName}\:
- AgvStatusReceived.json
- AgvGraphReceived.json
- AgvData.json
  Each file is optional. If it is found, it is seeded, otherwise it is skipped.
  For more Agvs, just create another folder with the same structure.

### Graph and Maps
For the Map and Graph, you have to place the following files under data\:
- GraphAndMap.json
- Map image as PNG

#### Map Alignment
To properly align the map graphic with the graph, the scaling factor, x-offset, and y-offset need to be manually determined and then encoded into the filename.
he structure of the filename should follow this pattern:
`mapName_scaleUp_xOffset_yOffset.png`

**Example**
- Filename: `stations_1.2_-10_-22.png`
- Name of the map: `stations`
- Upscale factor: `1.2` (greater than the graph scale)
- x-offset: `-10` (units in the graph coordinate system)
- y-offset: `-22` (units in the graph coordinate system)
