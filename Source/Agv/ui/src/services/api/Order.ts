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

export class Order<
  SecurityDataType = unknown
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags TransportOrder
   * @name OrderList
   * @summary Retrieve active transport orders
   * @request GET:/orders
   */
  orderList = (
    query?: { PageNumber?: number; PageSize?: number },
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
