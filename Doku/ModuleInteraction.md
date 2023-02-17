::: mermaid
flowchart TD
    classDef Process fill:#9f6,stroke:#333,stroke-width:1px;
    classDef UI fill:#9f6,stroke:#333,stroke-width:1px;
    classDef AgvManagement fill:#dd99f6,stroke:#333,stroke-width:1px;

    WcsCommunication[/WcsCommunication/]
    UI[/UI/]
    AssignAgvConsumer[[AssignAgvConsumer]]
    AgvPositionReceivedConsumer[[AgvPositionReceivedConsumer]]
    DeviceCommunication[/DeviceCommunication/]
    AgvStateMachine
    ActionManager[[ActionManager]]

    class WcsCommunication,DeviceCommunication Process
    class UI UI
    class AssignAgvConsumer,AgvStateMachine,AgvPositionReceivedConsumer AgvManagement


    UI -- ForceReleaseAgv --> AgvStateMachine
    UI -- LockAgv --> AgvStateMachine
    UI -- RequestAgvData/Graph --> DeviceCommunication
    UI -- ResetTransportOrder --> OrderStateMachine
    UI -- ManuallyCompleteTransportOrder --> OrderStateMachine

    WcsCommunication -- HandleHostOrder --> OrderStateMachine
    WcsCommunication -- CancelHostOrder --> OrderStateMachine

    OrderStateMachine -- HostOrderStarted --> WcsCommunication
    OrderStateMachine -- HostOrderHandlingSucceeded/Failed --> WcsCommunication
    OrderStateMachine -- HostOrderCancelSucceeded/Failed --> WcsCommunication
    OrderStateMachine -- OrderQueuingCommand --> ActionManager

    AgvStateMachine -. ReleaseAgv .-> AgvStateMachine
    AgvStateMachine -- AgvBecameIdle --> AgvChargingService
    AgvStateMachine == AgvReservationSucceeded/Rejected ==> AssignAgvConsumer
    AgvStateMachine -- AgvPropertyChanged --> AgvPropertyChangedConsumer
    AgvStateMachine -- RequestAgvData/Graph/Map --> DeviceCommunication

    AgvChargingService -- AgvReady --> ActionManager

    ActionManager -- HostOrderHandlingFailed --> WcsCommunication
    ActionManager -- OrderQueuedEvent --> OrderStateMachine
    ActionManager -- TransportOrderStarted/PickCompleted/Failed/Completed --> OrderStateMachine
    ActionManager -- OrderAssignmentSucceeded --> OrderStateMachine
    ActionManager -- ReleaseAgv --> AgvStateMachine
    ActionManager -- ActionStartedEvent --> AgvStateMachine
    ActionManager == AssignAgv ==> AssignAgvConsumer
    ActionManager -- StartActionCommand --> DeviceCommunication

    AssignAgvConsumer == ReserveAgv ==> AgvStateMachine
    AssignAgvConsumer == AgvAssignmentSucceeded/Failed ==> ActionManager

    DeviceCommunication -- AgvData/Graph/Status/MapReceived --> AgvStateMachine
    DeviceCommunication -- AgvPositionReceived --> AgvPositionReceivedConsumer
    DeviceCommunication -- ActionStatusChangedEvent --> ActionManager
:::