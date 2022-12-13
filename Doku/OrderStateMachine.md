::: mermaid
stateDiagram-v2
    [*] --> OrderReceived: HandleHostOrder
    OrderReceived --> OrderQueued: OrderQueuedEvent
    OrderReceived --> OrderReceived: TransportOrderReset
    OrderFailed --> OrderReceived: TransportOrderReset
    Final --> OrderReceived: TransportOrderReset

    OrderQueued --> TransportOrderSend: AgvAssignmentSucceeded
    TransportOrderSend --> TransportOrderStarted: TransportOrderStarted
    TransportOrderSend --> OrderFailed: TransportOrderFailed
    TransportOrderPicked --> OrderFailed: TransportOrderFailed
    TransportOrderStarted --> TransportOrderPicked: TransportOrderPickCompleted
    TransportOrderPicked --> Final: TransportOrderCompleted

    OrderReceived --> Cancelled: CancelHostOrder

    Final --> [*]
:::