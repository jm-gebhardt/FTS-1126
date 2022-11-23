::: mermaid
stateDiagram-v2
    [*] --> RequestAgvGraphReceived: RequestAgvGraph

    RequestAgvGraphReceived --> RequestAgvGraphReceived: RequestAgvGraph
    RequestAgvGraphReceived --> StationListReceived: InternalStationListReceived

    StationListReceived --> RequestAgvGraphReceived: RequestAgvGraph
    StationListReceived --> [*]: InternalGraphReceived
:::