::: mermaid
stateDiagram-v2
    [*] --> Discovered: AgvStatusReceived
    Discovered --> Discovered: AgvStatusReceived
    Discovered --> Idle: ReleaseAgv
    Idle --> Idle: AgvStatusReceived
    Idle --> Reserved: ReserveAgv
    Reserved --> Reserved: AgvStatusReceived
:::