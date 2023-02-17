::: mermaid
stateDiagram-v2
    [*] --> Idle: CreateAgv

    Idle --> BusyMoving: StartActionCommand

    BusyMoving --> Idle: ScheduleFinishAction
:::