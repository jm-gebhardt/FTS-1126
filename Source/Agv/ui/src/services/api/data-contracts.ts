/* eslint-disable */
/* tslint:disable */
/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

/**
 * AGV model
 */
export interface IAgv {
  /**
   * CorrelationId
   * @format uuid
   */
  correlationId: string
  /** AGV Serial Number */
  agvId: string
  /** Current State */
  currentState: string
}

/**
 * AGV Data
 */
export interface IAgvData {
  /**
   * Used to identify a message as correlated so that the CorrelationId can be returned
   * @format uuid
   */
  correlationId: string
  /** The model name of the AGV */
  model: string
  /** The type of the AGV */
  type: string
  /** The IP Address of the AGV */
  ipV4Address?: string | null
  /**
   * The maximum velocity of the AGV in straight direction [m/s]
   * @format double
   */
  maxVelocityStraight: number
  /**
   * The capacity of the AGVs battery in [Ah]
   * @format double
   */
  batteryCapacity: number
  /**
   * The length of the AGV in [m]
   * @format double
   */
  length?: number | null
  /**
   * The width of the AGV in [m]
   * @format double
   */
  width?: number | null
  /**
   * The maximum velocity of the AGV while turning on spot [m/s^2]
   * @format double
   */
  acceleration?: number | null
  /**
   * The maximum velocity of the AGV in while turning [m/s]
   * @format double
   */
  maxVelocityTurn?: number | null
  /**
   * The maximum velocity of the AGV while turning on spot [rad/s]
   * @format double
   */
  maxVelocityTurnOnSpot?: number | null
  /**
   * The max. payload of the AGV in [kg]
   * @format int64
   */
  payload?: number | null
  /** The list of sensors in the agv */
  sensors: ISensor[]
}

/**
 * AGV position information
 */
export interface IAgvPosition {
  /** Returns true, when the position is known and valid. */
  positionInitialized: boolean
  /**
   * Current X-position [m].
   * @format float
   */
  x: number
  /**
   * Current Y-position [m].
   * @format float
   */
  y: number
  /**
   * Current Z-position [m].
   * @format float
   */
  z: number
  /**
   * Current angle (rotation) [rad].
   * @format float
   */
  theta: number
  /**
   * Deviation from the responded position [m].
   * @format float
   */
  deviationRange?: number | null
}

/**
 * Model for the status of the AGV position
 */
export interface IAgvPositionStatus {
  /**
   * The correlationId of the new AGV position
   * @format uuid
   */
  correlationId: string
  /** Identifier of the AGV */
  agvId: string
  /** AGV position information */
  agvPosition: IAgvPosition
}

/**
 * AGV Status
 */
export interface IAgvStatus {
  /**
   * AGV correlation ID
   * @format uuid
   */
  correlationId: string
  /** Current or previous order ID. */
  orderId?: string | null
  /** AGV is busy (not idle). */
  busy: boolean
  /** AGV is in automatic mode. */
  automatic: boolean
  /**
   * Battery charge.
   * @format int32
   */
  batteryCharge: number
  /** AGV is charging. */
  charging: boolean
  /** AGV is disturbed. */
  disturbed: boolean
  /**
   * nodeID of last reached node or, if AGV is currently on a node, current node.
   * Empty string if no lastNodeId is available.
   */
  lastNodeId?: string | null
}

/**
 * Model for drawing an edge in the UI
 */
export interface IEdge {
  /** Node representation. */
  from: INode
  /** Node representation. */
  to: INode
}

/**
 * Model for drawing a graph in the UI
 */
export interface IGraph {
  /** The list of edges in the graph. */
  edges: IEdge[]
  /** The list of nodes in the graph */
  nodes: INode[]
  /**
   * CorrelationId
   * @format uuid
   */
  correlationId: string
}

/**
 * Defines the creator of a host order
 */
export enum IHostOrderCreator {
  Wms = 'Wms',
  Simulation = 'Simulation'
}

/**
 * SignalR hub methods
 */
export enum IHubMethods {
  OrderStateChanged = 'OrderStateChanged',
  AgvDiscovered = 'AgvDiscovered',
  AgvStateChanged = 'AgvStateChanged',
  AgvPositionChanged = 'AgvPositionChanged'
}

/**
 * Map Model
 */
export interface IMap {
  /**
   * Map Correlation Id
   * @format uuid
   */
  correlationId: string
  /** Map Id */
  mapId: string
  /**
   * The resolution of the maps image. Expressed as how many [m] per pixel are represented
   * @format double
   */
  resolution: number
  /**
   * The x offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.
   * @format double
   */
  xOffset: number
  /**
   * The y offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.
   * @format double
   */
  yOffset: number
  /** List of Agvs using this map */
  relatedAgvs: string[]
}

/**
 * Node representation.
 */
export interface INode {
  /** The unique ID of the node. */
  id: string
  /** Node enabled/disabled. */
  enabled: boolean
  /** Node type of the node. */
  type: INodeType
  /**
   * The x coordinate of the node.
   * @format float
   */
  x: number
  /**
   * The y coordinate of the node
   * @format float
   */
  y: number
  /**
   * The z coordinate of the node.
   * <br>
   * Set to default 0 when not needed.
   *
   * @format float
   */
  z: number
}

/**
* Node types.
<br>
These types are coming from the specification.
*/
export enum INodeType {
  WayPoint = 'WayPoint',
  Charge = 'Charge',
  Pick = 'Pick',
  Drop = 'Drop',
  PickDrop = 'PickDrop',
  Park = 'Park'
}

/**
* Order Model
            
This is a simply copy of the Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.IOrder to overcome a json serialization problems
<see href="https://github.com/dotnet/runtime/issues/41749" />
*/
export interface IOrder {
  /** Current State */
  currentState: string
  /**
   * AgvId
   * @format uuid
   */
  assignedAgv?: string | null
  /** Order object used by all messages that require the order details */
  orderDetails: IOrderDetails
  /**
   * Used to identify a message as correlated so that the CorrelationId can be returned
   * @format uuid
   */
  correlationId: string
  /** Reason why an order failed */
  failedReason?: string | null
}

/**
 * Order object used by all messages that require the order details
 */
export interface IOrderDetails {
  /**
   * Used to identify a message as correlated so that the CorrelationId can be returned
   * @format uuid
   */
  correlationId: string
  /** The unique ID of the order known by the host system */
  orderId: string
  /** The source where to pick up the order */
  source: string
  /** The destination where to drop the order */
  destination: string
  /** The LeNo of the orders handled box or transportation good */
  leNo: string
  /** The creator of the host order */
  creator: IHostOrderCreator
  /**
   * The date and time when the order was created
   * @format date-time
   */
  creationDate: string
}

/**
 * A sensor object skeleton
 */
export interface ISensor {
  /** The identifier of the sensor */
  sensorIdentifier?: string | null
}
