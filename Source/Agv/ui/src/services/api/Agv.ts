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

import { HttpClient, RequestParams } from './http-client'

export class Agv<
  SecurityDataType = unknown
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Agv
   * @name GetAgv
   * @summary Retrieve active agvs
   * @request GET:/agv
   */
  getAgv = (params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/agv`,
      method: 'GET',
      ...params
    })
  /**
   * No description
   *
   * @tags Agv
   * @name GraphDetail
   * @summary Retrieve graph for agv
   * @request GET:/agv/{correlationId}/graph
   */
  graphDetail = (correlationId: string, params: RequestParams = {}) =>
    this.request<void, void>({
      path: `/agv/${correlationId}/graph`,
      method: 'GET',
      ...params
    })
  /**
   * No description
   *
   * @tags Agv
   * @name StatusDetail
   * @summary Retrieve status for agv
   * @request GET:/agv/{correlationId}/status
   */
  statusDetail = (correlationId: string, params: RequestParams = {}) =>
    this.request<void, void>({
      path: `/agv/${correlationId}/status`,
      method: 'GET',
      ...params
    })
  /**
   * No description
   *
   * @tags Agv
   * @name DataDetail
   * @summary Retrieve data for agv
   * @request GET:/agv/{correlationId}/data
   */
  dataDetail = (correlationId: string, params: RequestParams = {}) =>
    this.request<void, void>({
      path: `/agv/${correlationId}/data`,
      method: 'GET',
      ...params
    })
}
