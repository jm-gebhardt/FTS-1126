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

import { IAgv, IAgvData, IGraph } from './data-contracts'
import { HttpClient, RequestParams } from './http-client'

export class Agvs<
  SecurityDataType = unknown
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Agv
   * @name AgvsList
   * @summary Retrieve paged list of agvs
   * @request GET:/agvs
   */
  agvsList = (params: RequestParams = {}) =>
    this.request<IAgv[], any>({
      path: `/agvs`,
      method: 'GET',
      format: 'json',
      ...params
    })
  /**
   * No description
   *
   * @tags Agv
   * @name GraphDetail
   * @summary Retrieve graph for agv
   * @request GET:/agvs/{correlationId}/graph
   */
  graphDetail = (correlationId: string, params: RequestParams = {}) =>
    this.request<IGraph, void>({
      path: `/agvs/${correlationId}/graph`,
      method: 'GET',
      format: 'json',
      ...params
    })
  /**
   * No description
   *
   * @tags Agv
   * @name DataDetail
   * @summary Retrieve data for agv
   * @request GET:/agvs/{correlationId}/data
   */
  dataDetail = (correlationId: string, params: RequestParams = {}) =>
    this.request<IAgvData, void>({
      path: `/agvs/${correlationId}/data`,
      method: 'GET',
      format: 'json',
      ...params
    })
  /**
   * No description
   *
   * @tags Agv
   * @name UnlockPartialUpdate
   * @summary Unlock AGV to allow it to be assigned to transport orders
   * @request PATCH:/agvs/{correlationId}/unlock
   */
  unlockPartialUpdate = (correlationId: string, params: RequestParams = {}) =>
    this.request<void, void>({
      path: `/agvs/${correlationId}/unlock`,
      method: 'PATCH',
      ...params
    })
  /**
   * No description
   *
   * @tags Agv
   * @name LockPartialUpdate
   * @summary Lock AGV to prevent it from being assigned to transport orders
   * @request PATCH:/agvs/{correlationId}/lock
   */
  lockPartialUpdate = (correlationId: string, params: RequestParams = {}) =>
    this.request<void, void>({
      path: `/agvs/${correlationId}/lock`,
      method: 'PATCH',
      ...params
    })
  /**
   * No description
   *
   * @tags Agv
   * @name EmitRequestDataPartialUpdate
   * @summary Emit RequestData telegram when receiving PATCH request
   * @request PATCH:/agvs/{correlationId}/emitRequestData
   */
  emitRequestDataPartialUpdate = (
    correlationId: string,
    params: RequestParams = {}
  ) =>
    this.request<void, void>({
      path: `/agvs/${correlationId}/emitRequestData`,
      method: 'PATCH',
      ...params
    })
  /**
   * No description
   *
   * @tags Agv
   * @name EmitRequestGraphPartialUpdate
   * @summary Emit RequestGraph telegram when receiving PATCH request
   * @request PATCH:/agvs/{correlationId}/emitRequestGraph
   */
  emitRequestGraphPartialUpdate = (
    correlationId: string,
    params: RequestParams = {}
  ) =>
    this.request<void, void>({
      path: `/agvs/${correlationId}/emitRequestGraph`,
      method: 'PATCH',
      ...params
    })
}
