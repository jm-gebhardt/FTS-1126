::: mermaid
stateDiagram-v2
    [*] --> ActionCreated: CreateActionRequest

    ActionCreated --> ActionSend: AssignActionRequest
    ActionSend --> ActionCompleted: ActionStatusChangedEvent(TransportOrderCompleted)
    ActionCompleted --> [*]
:::