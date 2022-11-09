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

    class WcsCommunication,DeviceCommunication Process
    class UI UI
    class AssignAgvConsumer,AgvStateMachine AgvManagement

    WcsCommunication -- HandleHostOrder --> OrderStateMachine
    WcsCommunication -- CancelHostOrder --> OrderStateMachine
    UI -- ReleaseAgv --> AgvStateMachine
    OrderStateMachine -- HostOrderStarted --> WcsCommunication
    OrderStateMachine -- HostOrderCompleted --> WcsCommunication
    OrderStateMachine -- HostOrderHandlingFailed --> WcsCommunication
    OrderStateMachine -- AssignAgv --> AssignAgvConsumer
    OrderStateMachine -- ReleaseAgv --> AgvStateMachine
    OrderStateMachine -- ExecuteTransportOrder --> DeviceCommunication
    AssignAgvConsumer -- ReserveAgv --> AgvStateMachine
    AssignAgvConsumer -- AgvAssignmentSucceeded/Failed --> OrderStateMachine
    AgvStateMachine -- AgvBecameIdle --> AssignAgvConsumer
    AgvStateMachine -- AgvReservationSucceeded/Failed --> AssignAgvConsumer
    AgvStateMachine -- RequestAgvData/Graph/Map --> DeviceCommunication
    AgvStateMachine -. ReleaseAgv .-> AgvStateMachine
    DeviceCommunication -- TransportOrderStarted/Completed/Failed --> OrderStateMachine
    DeviceCommunication -- TransportOrderPickCompleted --> OrderStateMachine
:::