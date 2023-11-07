# Introduction
This project provides a mechanism to generate orders using physical sensors. Leveraging the SmartLink module from FlexLog, you can connect a light barrier. Based on the configuration set, the light barrier must maintain stability for a specified duration before an ActionGroup is published via the RabbitMQ bus, thereby generating a QueueActionGroup.

# Configuration

This project offers two distinct methods for configuration.

## 1. Shortcut Configuration

This approach provides a shortcut for the current use case. Below is an example of how to configure via arguments:

`--url http://10.10.120.1/v1 --pin S1_Pin4 --nodes "S1,D2" --waitingTime 5`

By using the above configuration, a single module will be added. The action taken in this context will be the generation of an ActionGroup, which includes both a pick and drop action.

## 2. YAML File Configuration

For those seeking a higher degree of flexibility, configuration via a YAML file of the `Config` type is also available. Within this YAML file, parameters can be set freely. An example of such a YAML configuration can be found below:

```yaml
Modules:
    - SmartLinkUrl: "http://10.10.120.1/v1"
      IoActionConfigs:
          - Id: "S1_Pin4"
            After: "00:00:05"
            State: true
            ActionType: "PublishQueueActionGroup"
            Parameters:
                - Type: "Pick"
                  NodeId: "S1"
                - Type: "Move"
                  NodeId: "N002-000"
                - Type: "Move"
                  NodeId: "N003-002"
                - Type: "Move"
                  NodeId: "N002-000"
                - Type: "Move"
                  NodeId: "N003-002"
                - Type: "Move"
                  NodeId: "N002-000"
                - Type: "Drop"
                  NodeId: "D2"
```
## 3. Running in Docker

You can run the project using docker-compose. Ensure you run it alongside the AgvDevComb compose. An .env.template file is provided at the root of the repository.
`docker compose build` and `docker compose up`.