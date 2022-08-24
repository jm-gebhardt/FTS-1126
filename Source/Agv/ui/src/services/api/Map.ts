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

import { IMap } from './data-contracts'
import { HttpClient, RequestParams } from './http-client'

export class Map<
  SecurityDataType = unknown
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Map
   * @name GetMap
   * @summary Get all available maps
   * @request GET:/map
   */
  getMap = (params: RequestParams = {}) =>
    this.request<IMap[], any>({
      path: `/map`,
      method: 'GET',
      format: 'json',
      ...params
    })
  /**
   * No description
   *
   * @tags Map
   * @name MapPngDetail
   * @summary Get Map Image
   * @request GET:/map/{correlationId}/map.png
   */
  mapPngDetail = (correlationId: string, params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/map/${correlationId}/map.png`,
      method: 'GET',
      ...params
    })
}
