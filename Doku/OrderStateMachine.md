::: mermaid
stateDiagram-v2
    [*] --> OrderReceived: HandleHostOrder
    OrderReceived --> AgvRequested: RequestOrderAgvAssign
    AgvRequested --> TransportOrderSend: AgvAssignmentSucceeded
    state AgvAssignmentErrorCode <<choice>>
    AgvRequested --> AgvAssignmentErrorCode: AgvAssignmentFailed
    AgvAssignmentErrorCode --> OrderReceived: (NoIdleAgv)
    AgvAssignmentErrorCode --> OrderFailed: (AgvRegistryEmpty)
    AgvAssignmentErrorCode --> OrderFailed: (NoRouteFound)
    TransportOrderSend --> OrderFailed: TransportOrderFailed
    TransportOrderSend --> TransportOrderStarted: TransportOrderStarted
    TransportOrderStarted --> TransportOrderPicked: TransportOrderPickCompleted
    TransportOrderPicked --> OrderFailed: TransportOrderFailed
    TransportOrderPicked --> [*]: TransportOrderCompleted
:::