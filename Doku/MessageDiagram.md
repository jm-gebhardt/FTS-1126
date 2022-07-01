::: mermaid
sequenceDiagram
    participant WCSCommunication
    participant TransportOrder
    participant AgvManagement
    participant DeviceCommunication
    participant (AGV)

    Note over TransportOrder,AgvManagement: AgvManagement is currently<br>part of TransportOrder

    Note left of (AGV): TAR: AgvId == SerialNumber

    (AGV)->>DeviceCommunication: IState
    activate DeviceCommunication
    DeviceCommunication->>AgvManagement: IAgvStatusReceived
    deactivate DeviceCommunication
    activate AgvManagement
    AgvManagement->>DeviceCommunication: IAgvRequestGraph
    deactivate AgvManagement
    activate DeviceCommunication
    DeviceCommunication->>(AGV): IRequestGraph
    deactivate DeviceCommunication
    activate (AGV)
    (AGV)->>DeviceCommunication: IGraph
    deactivate (AGV)
    activate DeviceCommunication
    DeviceCommunication->>AgvManagement: IAgvGraphReceived
    deactivate DeviceCommunication

    Note over WCSCommunication,DeviceCommunication: Handle HostOrder

    WCSCommunication->>TransportOrder: IHandleHostOrder

    TransportOrder->>AgvManagement: IAssignAgv
    
    AgvManagement->>TransportOrder: IAgvAssignSucceeded

    TransportOrder->>DeviceCommunication: IExecuteTransportOrder
    
    DeviceCommunication->>TransportOrder: ITransportOrderStarted
    
    TransportOrder->>WCSCommunication: IHostOrderStarted
    
    DeviceCommunication->>TransportOrder: ITransportOrderPickCompleted
    
    alt success
        DeviceCommunication->>TransportOrder: ITransportOrderCompleted
        TransportOrder->>WCSCommunication: IHostOrderHandlingSucceeded
    else failed
        DeviceCommunication->>TransportOrder: ITransportOrderFailed
        TransportOrder->>WCSCommunication: IHostOrderHandlingFailed
    end
        
    Note over WCSCommunication,DeviceCommunication : Cancel HostOrder
    
    WCSCommunication->>TransportOrder: ICancelHostOrder
    TransportOrder->>DeviceCommunication: ICancelTransportOrder

    alt success
        DeviceCommunication->>TransportOrder: ITransportOrderCancelSucceeded 
        TransportOrder->>WCSCommunication: IHostOrderCancelSucceeded
    else failed
        DeviceCommunication->>TransportOrder: ITransportOrderCancelFailed
        TransportOrder->>WCSCommunication: IHostOrderCancelFailed
    end
    
    Note over WCSCommunication,DeviceCommunication : Update HostOrder
    
    WCSCommunication->>TransportOrder: IUpdateHostOrder
    
    TransportOrder->>DeviceCommunication: IUpdateTransportOrder
    alt success
        DeviceCommunication->>TransportOrder: ITransportOrderUpdateSucceeded
        TransportOrder->>WCSCommunication: IHostOrderUpdateSucceeded
    else failed
        DeviceCommunication->>TransportOrder: ITransportOrderUpdateFailed
        TransportOrder->>WCSCommunication: IHostOrderUpdateFailed
    end
:::