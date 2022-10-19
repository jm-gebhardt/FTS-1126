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

import { IAgvPositionStatus } from './data-contracts'
import { HttpClient, RequestParams } from './http-client'

export class AgvsPositions<
  SecurityDataType = unknown
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Agv
   * @name AgvsPositionsList
   * @summary Retrieve list of agv positions
   * @request GET:/agvsPositions
   */
  agvsPositionsList = (params: RequestParams = {}) =>
    this.request<IAgvPositionStatus[], any>({
      path: `/agvsPositions`,
      method: 'GET',
      format: 'json',
      ...params
    })
}
