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
  currentState: IAgvState
  /**
   * Time of the last state transition
   * @format date-time
   */
  timeOfStateTransition: string
  /**
   * Time of the last lock state change
   * @format date-time
   */
  timeOfLockStateChanged: string
  /** Communication alive state */
  isOnline: boolean
  /** Locked state */
  isLocked: boolean
  /** Is agv disturbed. There is at least one error with level "FATAL" */
  isDisturbed: boolean
  /** AGV is in automatic mode */
  isAutomatic: boolean
  /**
   * Battery charge level (Soc)
   * @format double
   */
  batteryCharge: number
  /** Is Battery currently charging */
  isCharging: boolean
  /**
   * Lower battery level in [%] for signaling "battery bad" (defined by VDA5050)
   * @format int32
   */
  badBatteryLevel: number
  /**
   * Upper battery level in [%] for signaling "battery good" (defined by VDA5050)
   * @format int32
   */
  goodBatteryLevel: number
}

/**
 * Agv Data model
 */
export interface IAgvData {
  /**
   * The CorrelationId of the agv
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
   * The capacity of the AGVs battery in [Ah]
   * @format double
   */
  batteryCapacity: number
  /**
   * The maximum velocity of the AGV while turning on spot [m/s^2]
   * @format double
   */
  acceleration?: number | null
  /**
   * The maximum velocity of the AGV in straight direction [m/s]
   * @format double
   */
  maxVelocityStraight: number
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
   * The width of the AGV in [m]
   * @format double
   */
  width?: number | null
  /**
   * The length of the AGV in [m]
   * @format double
   */
  length?: number | null
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
 * Agv State
 */
export enum IAgvState {
  Initial = 'Initial',
  Discovered = 'Discovered',
  Idle = 'Idle',
  Busy = 'Busy',
  Charging = 'Charging'
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
 * SignalR hub methods
 */
export enum IHubMethods {
  AgvDiscovered = 'AgvDiscovered',
  AgvChanged = 'AgvChanged',
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
  /** Node state. */
  state: INodeState
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
  /**
   * The theta coordinate of the node.
   * @format float
   */
  theta: number
}

/**
 * Node state
 */
export enum INodeState {
  Free = 'Free',
  Reserved = 'Reserved',
  Occupied = 'Occupied',
  Locked = 'Locked'
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
 * A sensor object skeleton
 */
export interface ISensor {
  /** The identifier of the sensor */
  sensorIdentifier?: string | null
}
