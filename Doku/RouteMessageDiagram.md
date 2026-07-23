::: mermaid
sequenceDiagram
    participant TransportOrder
    participant RouteEventHandler
    participant OrderExecutionSaga
    participant AGV
    participant MqttStateConsumer
    participant RouteStepEventHandler

    Note over RouteEventHandler,OrderExecutionSaga: DeviceCommunication.Vda5050 - route/order command side
    Note over MqttStateConsumer,RouteStepEventHandler: DeviceCommunication.Vda5050 - state/event side

    Note over TransportOrder,AGV: Plan a route

    TransportOrder->>+RouteEventHandler: RoutePlanned
    RouteEventHandler->>RouteEventHandler: coordinator.PlanRouteAsync
    Note right of RouteEventHandler: NoRouteAction - route only has unreleased<br/>(horizon) steps, order withheld until steps are released
    deactivate RouteEventHandler

    Note over TransportOrder,AGV: Release steps

    TransportOrder->>+RouteEventHandler: RouteStepsReleased
    RouteEventHandler->>RouteEventHandler: coordinator.ReleaseRouteStepsAsync
    alt order not started yet
        RouteEventHandler->>OrderExecutionSaga: StartOrder
    else order already running
        RouteEventHandler->>OrderExecutionSaga: OrderUpdateRequested
    end
    OrderExecutionSaga->>AGV: MqttOrder
    deactivate RouteEventHandler

    Note over TransportOrder,AGV: Extend/replace the route

    TransportOrder->>+RouteEventHandler: RouteUpdated
    RouteEventHandler->>RouteEventHandler: coordinator.UpdateRouteAsync
    RouteEventHandler->>OrderExecutionSaga: OrderUpdateRequested
    OrderExecutionSaga->>AGV: MqttOrder
    deactivate RouteEventHandler

    Note over TransportOrder,AGV: AGV reports traversal progress

    AGV->>+MqttStateConsumer: MqttState
    MqttStateConsumer->>OrderExecutionSaga: OrderStateReceived
    MqttStateConsumer->>RouteStepEventHandler: OrderStateReceived
    deactivate MqttStateConsumer
    activate RouteStepEventHandler
    RouteStepEventHandler->>RouteStepEventHandler: coordinator.DetermineRouteStepProgress
    RouteStepEventHandler->>TransportOrder: RouteStepsProcessed
    opt order completed
        Note right of RouteStepEventHandler: Agv.CurrentOrder is cleared
    end
    deactivate RouteStepEventHandler

    Note over TransportOrder,AGV: Abort the route

    TransportOrder->>+RouteEventHandler: RouteAborted
    RouteEventHandler->>RouteEventHandler: coordinator.AbortRouteAsync
    RouteEventHandler->>OrderExecutionSaga: OrderCancellationRequested
    OrderExecutionSaga->>AGV: MqttInstantActions (cancelOrder)
    deactivate RouteEventHandler
:::
