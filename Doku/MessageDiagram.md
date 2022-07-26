::: mermaid
sequenceDiagram
    participant WCSCommunication
    participant TransportOrder
    participant AgvManagement
    participant DeviceCommunication
    participant (AGV)

    Note over TransportOrder,AgvManagement: AgvManagement is currently<br>part of TransportOrder

    Note left of (AGV): TAR: AgvId == SerialNumber

    (AGV)->>DeviceCommunication: State
    activate DeviceCommunication
    DeviceCommunication->>AgvManagement: AgvStatusReceived
    deactivate DeviceCommunication
    AgvManagement->>AgvManagement: ReleaseAgv

    AgvManagement->>DeviceCommunication: RequestAgvData
    activate DeviceCommunication
    DeviceCommunication->>(AGV): RequestData
    deactivate DeviceCommunication
    activate (AGV)
    (AGV)->>DeviceCommunication: AgvData
    deactivate (AGV)
    activate DeviceCommunication
    DeviceCommunication->>AgvManagement: AgvDataReceived
    deactivate DeviceCommunication

    AgvManagement->>DeviceCommunication: RequestAgvGraph
    activate DeviceCommunication
    DeviceCommunication->>(AGV): MqttRequestStationList
    deactivate DeviceCommunication
    activate (AGV)
    (AGV)->>DeviceCommunication: MqttStationList
    deactivate (AGV)
    DeviceCommunication->>DeviceCommunication: StationListReceived
    activate DeviceCommunication
    DeviceCommunication->>(AGV): MqttRequestGraph
    deactivate DeviceCommunication
    activate (AGV)
    (AGV)->>DeviceCommunication: MqttGraph
    deactivate (AGV)
    DeviceCommunication->>DeviceCommunication: GraphReceived
    activate DeviceCommunication
    DeviceCommunication->>AgvManagement: AgvGraphReceived
    deactivate DeviceCommunication

    Note over WCSCommunication,DeviceCommunication: Handle HostOrder

    WCSCommunication->>TransportOrder: HandleHostOrder

    TransportOrder->>AgvManagement: AssignAgv

    AgvManagement->>TransportOrder: AgvAssignmentSucceeded

    TransportOrder->>DeviceCommunication: ExecuteTransportOrder

    DeviceCommunication->>TransportOrder: TransportOrderStarted

    TransportOrder->>WCSCommunication: HostOrderStarted

    DeviceCommunication->>TransportOrder: TransportOrderPickCompleted

    alt success
        DeviceCommunication->>TransportOrder: TransportOrderCompleted
        TransportOrder->>WCSCommunication: HostOrderHandlingSucceeded
    else failed
        DeviceCommunication->>TransportOrder: TransportOrderFailed
        alt No User input
            TransportOrder->>TransportOrder: Wait for user input
        else User input "NOK"
            TransportOrder->>WCSCommunication: HostOrderHandlingFailed
        end
    end

    Note over WCSCommunication,DeviceCommunication : Cancel HostOrder

    WCSCommunication->>TransportOrder: CancelHostOrder
    TransportOrder->>DeviceCommunication: CancelTransportOrder

    alt success
        DeviceCommunication->>TransportOrder: TransportOrderCancelSucceeded 
        TransportOrder->>WCSCommunication: HostOrderCancelSucceeded
    else failed
        DeviceCommunication->>TransportOrder: TransportOrderCancelFailed
        TransportOrder->>WCSCommunication: HostOrderCancelFailed
    end

    Note over WCSCommunication,DeviceCommunication : Update HostOrder

    WCSCommunication->>TransportOrder: UpdateHostOrder

    TransportOrder->>DeviceCommunication: UpdateTransportOrder
    alt success
        DeviceCommunication->>TransportOrder: TransportOrderUpdateSucceeded
        TransportOrder->>WCSCommunication: HostOrderUpdateSucceeded
    else failed
        DeviceCommunication->>TransportOrder: TransportOrderUpdateFailed
        TransportOrder->>WCSCommunication: HostOrderUpdateFailed
    end
:::