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

import { IAgv, IAgvData, IAgvGraph, IAgvStatus } from './data-contracts'
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
  agvsList = (
    query?: { pageNumber?: number; pageSize?: number },
    params: RequestParams = {}
  ) =>
    this.request<IAgv[], any>({
      path: `/agvs`,
      method: 'GET',
      query: query,
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
    this.request<IAgvGraph, void>({
      path: `/agvs/${correlationId}/graph`,
      method: 'GET',
      format: 'json',
      ...params
    })
  /**
   * No description
   *
   * @tags Agv
   * @name StatusDetail
   * @summary Retrieve status for agv
   * @request GET:/agvs/{correlationId}/status
   */
  statusDetail = (correlationId: string, params: RequestParams = {}) =>
    this.request<IAgvStatus, void>({
      path: `/agvs/${correlationId}/status`,
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
}
