::: mermaid
stateDiagram-v2
    [*] --> Idle: CreateAgvEvent

    Idle --> BusyMovingToSource: ExecuteTransportOrder

    BusyMovingToSource --> BusyMovingToDestination: SchedulePickOrde

    BusyMovingToDestination --> Idle: ScheduleFinishOrder
:::