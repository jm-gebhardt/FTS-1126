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

import { IHubMethods } from './data-contracts'
import { HttpClient, RequestParams } from './http-client'

export class Meta<
  SecurityDataType = unknown
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Meta
   * @name HubMethodsList
   * @summary Get available SignalR hub methods
   * @request GET:/meta/hubMethods
   */
  hubMethodsList = (params: RequestParams = {}) =>
    this.request<IHubMethods[], any>({
      path: `/meta/hubMethods`,
      method: 'GET',
      format: 'json',
      ...params
    })
}
