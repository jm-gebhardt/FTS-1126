::: mermaid
stateDiagram-v2
    [*] --> Discovered: AgvStatusReceived

    Discovered --> Idle: ReleaseAgv
    Busy --> Idle: ReleaseAgv
    PendingLock --> Locked: ReleaseAgv

    Reserved --> Busy: ActionStartedEvent
	Busy --> Busy: ActionStartedEvent
	
    Busy --> PendingLock: LockAgv
    
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