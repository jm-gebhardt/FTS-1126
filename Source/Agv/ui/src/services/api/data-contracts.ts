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

export enum IHostOrderCreator {
  Wms = 'Wms',
  Simulation = 'Simulation'
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
