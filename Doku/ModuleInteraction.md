::: mermaid
flowchart TD
    classDef Process fill:#9f6,stroke:#333,stroke-width:1px;
    classDef UI fill:#9f6,stroke:#333,stroke-width:1px;
    classDef AgvManagement fill:#dd99f6,stroke:#333,stroke-width:1px;

    WcsCommunication[/WcsCommunication/]
    UI[/UI/]
    AssignAgvConsumer[[AssignAgvConsumer]]
    DeviceCommunication[/DeviceCommunication/]
    AgvStateMachine
    ActionManager
    ActionStateMachine

    class WcsCommunication,DeviceCommunication Process
    class UI UI
    class AssignAgvConsumer,AgvStateMachine AgvManagement

    WcsCommunication -- HandleHostOrder --> OrderStateMachine
    WcsCommunication -- CancelHostOrder --> OrderStateMachine
    UI -- ReleaseAgv --> AgvStateMachine
    OrderStateMachine -- HostOrderStarted --> WcsCommunication
    OrderStateMachine -- HostOrderCompleted --> WcsCommunication
    OrderStateMachine -- HostOrderHandlingFailed --> WcsCommunication
    OrderStateMachine -- OrderQueuingCommand --> ActionManager
    ActionManager -- OrderQueuedEvent --> OrderStateMachine

    ActionManager -- AssignActionRequest --> ActionStateMachine
    ActionStateMachine -- ActionAssignmentSucceededEvent --> ActionManager

    DeviceCommunication -- ActionStatusChangedEvent --> ActionStateMachine
    ActionStateMachine -- TransportOrderStarted/PickCompleted/Completed --> OrderStateMachine
    ActionManager -- AssignAgv --> AssignAgvConsumer
    AssignAgvConsumer -- ReserveAgv --> AgvStateMachine
    AssignAgvConsumer -- AgvAssignmentSucceeded/Failed --> ActionManager


    ActionStateMachine -- ReleaseAgv --> AgvStateMachine
    ActionManager -- StartActionCommand --> DeviceCommunication

    AgvStateMachine -- AgvBecameIdle --> ActionManager
    AgvStateMachine -- AgvReservationSucceeded/Failed --> AssignAgvConsumer
    AgvStateMachine -- RequestAgvData/Graph/Map --> DeviceCommunication
    AgvStateMachine -. ReleaseAgv .-> AgvStateMachine
:::