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
  correlationId?: string

  /** AGV Serial Number */
  agvId?: string | null

  /** Current State */
  currentState?: string | null
}

export interface IAgvData {
  /** @format uuid */
  correlationId?: string
  model?: string | null
  type?: string | null
  ipV4Address?: string | null

  /** @format double */
  maxVelocityStraight?: number

  /** @format double */
  batteryCapacity?: number

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
  sensors?: ISensor[] | null
}

export interface IAgvGraph {
  /** @format uuid */
  correlationId?: string

  /** @format uuid */
  relatedMapId?: string
  relatedMapName?: string | null
  nodes?: INode[] | null
  edges?: IEdge[] | null
}

export interface IAgvStatus {
  /** @format uuid */
  correlationId?: string
  orderId?: string | null
  busy?: boolean
  automatic?: boolean

  /** @format int32 */
  batteryCharge?: number
  charging?: boolean
  disturbed?: boolean
  lastNodeId?: string | null
}

export interface IEdge {
  from?: string | null
  to?: string | null

  /** @format float */
  metric?: number
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
  AgvStateChanged = 'AgvStateChanged'
}

export interface INode {
  id?: string | null
  enabled?: boolean
  type?: INodeType

  /** @format float */
  x?: number

  /** @format float */
  y?: number

  /** @format float */
  z?: number
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
  currentState?: string | null

  /** @format uuid */
  assignedAgv?: string | null
  orderDetails?: IOrderDetails

  /** @format uuid */
  correlationId?: string
}

export interface IOrderDetails {
  /** @format uuid */
  correlationId?: string
  orderId?: string | null
  source?: string | null
  destination?: string | null
  leNo?: string | null
  creator?: IHostOrderCreator

  /** @format date-time */
  creationDate?: string
}

export interface ISensor {
  sensorIdentifier?: string | null
}
