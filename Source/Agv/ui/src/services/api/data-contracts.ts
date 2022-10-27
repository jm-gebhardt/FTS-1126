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
 * Controller for host order data
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

export interface IAgvData {
  /** @format uuid */
  correlationId: string
  model: string
  type: string
  ipV4Address?: string | null
  /** @format double */
  maxVelocityStraight: number
  /** @format double */
  batteryCapacity: number
  /** @format double */
  length?: number | null
  /** @format double */
  width?: number | null
  /** @format double */
  acceleration?: number | null
  /** @format double */
  maxVelocityTurn?: number | null
  /** @format double */
  maxVelocityTurnOnSpot?: number | null
  /** @format int64 */
  payload?: number | null
  sensors: ISensor[]
}

export interface IAgvPosition {
  positionInitialized: boolean
  /** @format float */
  x: number
  /** @format float */
  y: number
  /** @format float */
  z: number
  /** @format float */
  theta: number
  /** @format float */
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
  /** AGV position */
  agvPosition: IAgvPosition
}

export interface IAgvStatus {
  /** @format uuid */
  correlationId: string
  orderId?: string | null
  busy: boolean
  automatic: boolean
  /** @format int32 */
  batteryCharge: number
  charging: boolean
  disturbed: boolean
  lastNodeId?: string | null
}

/**
 * Model for drawing an edge in the UI
 */
export interface IEdge {
  /** Node where the edge starts. */
  from: INode
  /** Node where the edge ends. */
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

export interface INode {
  id: string
  enabled: boolean
  type: INodeType
  /** @format float */
  x: number
  /** @format float */
  y: number
  /** @format float */
  z: number
}

export enum INodeType {
  Charge = 'Charge',
  WayPoint = 'WayPoint',
  Turn = 'Turn',
  Station = 'Station',
  Park = 'Park'
}

/**
* Order Model
            
This is a simply copy of the Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.IOrder to overcome a json serialization problems
<see href="https://github.com/dotnet/runtime/issues/41749" />
*/
export interface IOrder {
  currentState: string
  /** @format uuid */
  assignedAgv?: string | null
  orderDetails: IOrderDetails
  /** @format uuid */
  correlationId: string
  failedReason?: string | null
}

export interface IOrderDetails {
  /** @format uuid */
  correlationId: string
  orderId: string
  source: string
  destination: string
  leNo: string
  creator: IHostOrderCreator
  /** @format date-time */
  creationDate: string
}

export interface ISensor {
  sensorIdentifier?: string | null
}
