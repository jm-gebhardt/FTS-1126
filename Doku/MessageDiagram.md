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
    DeviceCommunication->>AgvManagement: AgvPositionReceived
    DeviceCommunication->>AgvManagement: AgvStatusReceived
    deactivate DeviceCommunication
    AgvManagement->>AgvManagement: ReleaseAgv
    AgvManagement->>AgvManagement: AgvBecameIdle

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
    DeviceCommunication->>(AGV): RequestStationList
    deactivate DeviceCommunication
    activate (AGV)
    (AGV)->>DeviceCommunication: StationList
    deactivate (AGV)
    DeviceCommunication->>DeviceCommunication: InternalStationListReceived
    activate DeviceCommunication
    DeviceCommunication->>(AGV): RequestGraph
    deactivate DeviceCommunication
    activate (AGV)
    (AGV)->>DeviceCommunication: Graph
    deactivate (AGV)
    DeviceCommunication->>DeviceCommunication: InternalGraphReceived
    activate DeviceCommunication
    DeviceCommunication->>AgvManagement: AgvGraphReceived
    deactivate DeviceCommunication
    AgvManagement->>DeviceCommunication: RequestAgvMap
    activate DeviceCommunication
    DeviceCommunication->>(AGV): MqttRequestMap
    deactivate DeviceCommunication
    activate (AGV)
    (AGV)->>DeviceCommunication: MqttMap
    deactivate (AGV)
    activate DeviceCommunication
    DeviceCommunication->>AgvManagement: AgvMapReceived
    deactivate DeviceCommunication

    Note over WCSCommunication,DeviceCommunication: Handle HostOrder

    WCSCommunication->>TransportOrder: HandleHostOrder
    TransportOrder->>TransportOrder: RequestOrderAgvAssign
    TransportOrder->>AgvManagement: AssignAgv
    alt success
        AgvManagement->>TransportOrder: AgvAssignmentSucceeded

        TransportOrder->>DeviceCommunication: ExecuteTransportOrder
        activate DeviceCommunication
        DeviceCommunication->>(AGV): Order:Order
        deactivate DeviceCommunication
        activate (AGV)
        (AGV)->>DeviceCommunication: OrderEvent:Started
        activate DeviceCommunication
        DeviceCommunication->>TransportOrder: TransportOrderStarted

        TransportOrder->>WCSCommunication: HostOrderStarted
        (AGV)->>DeviceCommunication: OrderEvent:Picked
        DeviceCommunication->>TransportOrder: TransportOrderPickCompleted

        alt success
            (AGV)->>DeviceCommunication: OrderEvent:Completed
            DeviceCommunication->>TransportOrder: TransportOrderCompleted
            TransportOrder->>WCSCommunication: HostOrderHandlingSucceeded
        else failed
            (AGV)->>DeviceCommunication: OrderEvent:Failed
            DeviceCommunication->>TransportOrder: TransportOrderFailed
            alt No User input
                TransportOrder->>TransportOrder: Wait for user input
            else User input "NOK"
                TransportOrder->>WCSCommunication: HostOrderHandlingFailed
            end
        end
        deactivate (AGV)
        deactivate DeviceCommunication
    else failed
        AgvManagement->>TransportOrder: AgvAssignmentFailed
        TransportOrder->>WCSCommunication: HostOrderHandlingFailed
    end


    Note over WCSCommunication,DeviceCommunication : Cancel HostOrder

    WCSCommunication->>TransportOrder: CancelHostOrder
    TransportOrder->>DeviceCommunication: CancelTransportOrder
    activate DeviceCommunication
    DeviceCommunication->>(AGV): Order:Cancel
    deactivate DeviceCommunication
    activate (AGV)
    (AGV)->>DeviceCommunication: OrderEvent:Cancelled
    activate DeviceCommunication
    alt success
        DeviceCommunication->>TransportOrder: TransportOrderCancelSucceeded 
        TransportOrder->>WCSCommunication: HostOrderCancelSucceeded
    else failed
        (AGV)->>DeviceCommunication: OrderEvent:CancelFailed
        DeviceCommunication->>TransportOrder: TransportOrderCancelFailed
        TransportOrder->>WCSCommunication: HostOrderCancelFailed
    end
    deactivate (AGV)
    deactivate DeviceCommunication

    Note over WCSCommunication,DeviceCommunication : Update HostOrder

    WCSCommunication->>TransportOrder: UpdateHostOrder

    TransportOrder->>DeviceCommunication: UpdateTransportOrder
    activate DeviceCommunication
    DeviceCommunication->>(AGV): Order:OrderUpdate
    activate (AGV)
    (AGV)->>DeviceCommunication: OrderEvent:Updated
    alt success
        DeviceCommunication->>TransportOrder: TransportOrderUpdateSucceeded
        TransportOrder->>WCSCommunication: HostOrderUpdateSucceeded
    else failed
        (AGV)->>DeviceCommunication: OrderEvent:UpdateFailed
        DeviceCommunication->>TransportOrder: TransportOrderUpdateFailed
        TransportOrder->>WCSCommunication: HostOrderUpdateFailed
    end
    deactivate (AGV)
    deactivate DeviceCommunication
:::