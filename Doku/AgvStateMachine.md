::: mermaid
stateDiagram-v2
    [*] --> Discovered: AgvStatusReceived

    Discovered --> Idle: ReleaseAgv
    Reserved --> Idle: ReleaseAgv
    PendingLock --> Locked: ReleaseAgv
    
    Discovered --> Idle: ForceReleaseAgv
    Locked --> Idle: ForceReleaseAgv
    
    Idle --> Reserved: ReserveAgv
    
    Idle --> Locked: LockAgv
    Discovered --> Locked: LockAgv
    Reserved --> PendingLock: LockAgv

    Reserved --> Charging: AgvStartedCharging
    Idle --> Charging: AgvStartedCharging
    Charging --> Idle: AgvStoppedCharging
:::