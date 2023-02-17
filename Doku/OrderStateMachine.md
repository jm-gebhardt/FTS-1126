::: mermaid
stateDiagram-v2
    [*] --> OrderReceived: HandleHostOrder
    OrderReceived --> OrderQueued: OrderQueuedEvent
    OrderReceived --> OrderReceived: ResetTransportOrder
    OrderReceived --> Cancelled: CancelHostOrder

    OrderQueued --> TransportOrderSend: OrderAssignmentSucceeded

    TransportOrderSend --> TransportOrderStarted: TransportOrderStarted
    TransportOrderSend --> OrderFailed: TransportOrderFailed

    TransportOrderPicked --> OrderFailed: TransportOrderFailed
    TransportOrderPicked --> Final: TransportOrderCompleted

    TransportOrderStarted --> Final: TransportOrderCompleted
    TransportOrderStarted --> TransportOrderPicked: TransportOrderPickCompleted

    OrderFailed --> OrderReceived: ResetTransportOrder

    Final --> OrderReceived: ResetTransportOrder
    Final --> [*]

    note right of Final: Finalize by ManuallyCompleteTransportOrder during any state
:::