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

import { IOrder } from './data-contracts'
import { HttpClient, RequestParams } from './http-client'

export class Orders<
  SecurityDataType = unknown
> extends HttpClient<SecurityDataType> {
  /**
 * No description
 *
 * @tags TransportOrder
 * @name OrdersList
 * @summary Retrieve active transport orders
If correlation Id is provided, all other parameters are ignored
 * @request GET:/orders
 */
  ordersList = (
    query?: {
      /**
       * Page Number
       * @format int32
       */
      PageNumber?: number
      /**
       * Page Size
       * @format int32
       */
      PageSize?: number
      /**
       * Start date of range
       * @format date-time
       */
      Start?: string
      /**
       * End date of range
       * @format date-time
       */
      End?: string
    },
    params: RequestParams = {}
  ) =>
    this.request<IOrder[], any>({
      path: `/orders`,
      method: 'GET',
      query: query,
      format: 'json',
      ...params
    })
}
