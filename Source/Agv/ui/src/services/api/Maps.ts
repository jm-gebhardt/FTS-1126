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

export class Maps<
  SecurityDataType = unknown
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Map
   * @name MapsList
   * @summary Get all available maps
   * @request GET:/maps
   */
  mapsList = (params: RequestParams = {}) =>
    this.request<IMap[], any>({
      path: `/maps`,
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
   * @request GET:/maps/{correlationId}/map.png
   */
  mapPngDetail = (correlationId: string, params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/maps/${correlationId}/map.png`,
      method: 'GET',
      ...params
    })
}
