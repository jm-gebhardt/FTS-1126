::: mermaid
stateDiagram-v2
    [*] --> Discovered: AgvStatusReceived

    Busy --> Busy: ActionStartedEvent
    Reserved --> Busy: ActionStartedEvent

    PendingLock --> Locked: ReleaseAgv
    Ready --> Reserved: ReserveAgv

    Reserved --> PendingLock: LockAgv
    Busy --> PendingLock: LockAgv

    Idle --> Charging: AgvStartedCharging
    Charging --> Ready: AgvStoppedCharging

    Idle --> Ready: AgvReady

    Discovered --> Idle: ReleaseAgv
    Busy --> Idle: ReleaseAgv

    Discovered --> Idle: ForceReleaseAgv
    Locked --> Idle: ForceReleaseAgv

    Ready --> Locked: LockAgv
    Idle --> Locked: LockAgv
    Discovered --> Locked: LockAgv
:::