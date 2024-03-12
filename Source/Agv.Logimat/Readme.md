# Fake sending messages

## Preparation
Create a text file in which each line is a new json message to be send to the same given topic. The file needs to contain  
at least 2 messages.
Example:
```txt
{"headerId":1,"actionStates":[],"agvPosition":{"deviationRange":0,"localizationScore":0.42,"mapDescription":"Logimat2024 map provisioned","mapId":"Logimat2024","positionInitialized":true,"theta":-1.5707,"x":2.03,"y":-0.55},"batteryState":{"batteryCharge":99.8677749633789,"batteryCurrent":1.2,"batteryHealth":98,"batteryTemperature":30.3,"batteryVoltage":48.8,"charging":false,"reach":1000},"distanceSinceLastNode":0,"driving":false,"edgeStates":[],"errors":[],"information":[],"lastNodeId":"","lastNodeSequenceId":0,"loads":[],"manufacturer":"Gebhardt","newBaseRequest":false,"nodeStates":[],"operatingMode":"AUTOMATIC","orderId":"","orderUpdateId":0,"paused":false,"safetyState":{"eStop":"NONE","fieldViolation":false},"serialNumber":"InstaPick-1","timestamp":"2024-02-22T12:22:36Z","velocity":{"omega":0,"vx":0,"vy":0},"version":"2.0.0","zoneSetId":""}
{"headerId":2,"actionStates":[],"agvPosition":{"deviationRange":0,"localizationScore":0.42,"mapDescription":"Logimat2024 map provisioned","mapId":"Logimat2024","positionInitialized":true,"theta":-1.5707,"x":2.03,"y":-1.15},"batteryState":{"batteryCharge":99.8677749633789,"batteryCurrent":1.2,"batteryHealth":98,"batteryTemperature":30.3,"batteryVoltage":48.8,"charging":false,"reach":1000},"distanceSinceLastNode":0,"driving":false,"edgeStates":[],"errors":[],"information":[],"lastNodeId":"","lastNodeSequenceId":0,"loads":[],"manufacturer":"Gebhardt","newBaseRequest":false,"nodeStates":[],"operatingMode":"AUTOMATIC","orderId":"","orderUpdateId":0,"paused":false,"safetyState":{"eStop":"NONE","fieldViolation":false},"serialNumber":"InstaPick-1","timestamp":"2024-02-22T12:22:36Z","velocity":{"omega":0,"vx":0,"vy":0},"version":"2.0.0","zoneSetId":""}
```
And add a service for the agv to be faked in `docker-compose.yml`. Or modify the volume configuration of an 
existing agv service to use the new file.

Build the docker image(s).

```bash
sudo docker compose build 
```

## Run

1) Start AgvDevComb in production mode
2) Start the logimat provisioning when all services are healthy
3) Move into this directory and start the agvs you want to fake when provisioning ran successfully

### All

```bash
sudo docker compose --env-file ../../../.env  up  -d
```

### Instapick-1

```bash
sudo docker compose --env-file ../../../.env  up -d instapick-1
```

### Instapick-2

```bash
sudo docker compose --env-file ../../../.env  up -d instapick-2
```

### Karis-AMR

```bash
sudo docker compose --env-file ../../../.env  up -d karis-amr
```

### Karis-FLOW

```bash
sudo docker compose --env-file ../../../.env  up -d karis-flow
```

### Omnipallet

```bash
sudo docker compose --env-file ../../../.env  up -d omnipallet
```

## Stop 

```bash
sudo docker compose --env-file ../../../.env  down --remove-orphans
```








