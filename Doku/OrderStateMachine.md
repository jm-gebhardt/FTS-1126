::: mermaid
stateDiagram-v2
    [*] --> OrderReceived: HandleHostOrder
    OrderReceived --> AgvRequested: RequestOrderAgvAssign

    AgvRequested --> TransportOrderSend: AgvAssignmentSucceeded
    AgvRequested --> AgvAssignmentErrorCode: AgvAssignmentFailed

    state AgvAssignmentErrorCode <<choice>>
    AgvAssignmentErrorCode --> OrderReceived: (NoIdleAgv)
    AgvAssignmentErrorCode --> OrderFailed: (AgvRegistryEmpty)
    AgvAssignmentErrorCode --> OrderFailed: (NoRouteFound)

    TransportOrderSend --> OrderFailed: TransportOrderFailed
    TransportOrderSend --> TransportOrderStarted: TransportOrderStarted

    TransportOrderStarted --> TransportOrderPicked: TransportOrderPickCompleted

    TransportOrderPicked --> OrderFailed: TransportOrderFailed
    TransportOrderPicked --> Final: TransportOrderCompleted

    OrderFailed --> OrderReceived: TransportOrderReset
    Final --> OrderReceived: TransportOrderReset

    Final --> [*]
:::