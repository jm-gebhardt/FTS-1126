import {
  IAgv,
  IAgvData,
  IAgvGraph,
  IAgvStatus,
  IMap
} from '@/services/api/data-contracts'
import { IPaginatedRestApiStore, IRestApiStore } from '@/services/IRestApiStore'
import { defineStore } from 'pinia'
import { Agvs as AgvApi } from '@/services/api/Agvs'
import { Maps as MapApi } from '@/services/api/Maps'

const agvApi = new AgvApi({
  baseUrl: import.meta.env.VITE_API_URL
})
const mapApi = new MapApi({
  baseUrl: import.meta.env.VITE_API_URL
})

interface AgvStoreState {
  agvs: IPaginatedRestApiStore<Map<string, IAgv>>
  agvGraph: IRestApiStore<Map<string, IAgvGraph>>
  agvData: IRestApiStore<Map<string, IAgvData>>
  agvStatus: IRestApiStore<Map<string, IAgvStatus>>
  maps: IRestApiStore<Map<string, IMap>>
  mapsImage: IRestApiStore<Map<string, string>>
}

export const useAgvStore = defineStore({
  id: 'agvStore',
  state: (): AgvStoreState => ({
    agvs: {
      data: new Map(),
      loading: false,
      error: null,
      currentPage: 0,
      pageSize: 0,
      totalPages: 1,
      totalCount: 0
    },
    agvGraph: {
      data: new Map(),
      loading: false,
      error: null
    },
    agvData: {
      data: new Map(),
      loading: false,
      error: null
    },
    agvStatus: {
      data: new Map(),
      loading: false,
      error: null
    },
    maps: {
      data: new Map(),
      loading: false,
      error: null
    },
    mapsImage: {
      data: new Map(),
      loading: false,
      error: null
    }
  }),
  actions: {
    fetchAgvs: async function (pageNumber = 0, pageSize = 0): Promise<void> {
      this.agvs.loading = true

      return await agvApi
        .agvsList({ pageNumber: pageNumber, pageSize: pageSize })
        .then((response) => {
          this.agvs.error = null
          const data = new Map<string, IAgv>()
          response.data.forEach((agv: IAgv) => {
            if (agv.correlationId != null) {
              data.set(agv.correlationId, agv)
            }
          })
          this.agvs.data = data
          const paginationHeader = response.headers.get('x-pagination')
          if (paginationHeader != null) {
            const pagination = JSON.parse(paginationHeader)
            this.agvs.pageSize = pagination.PageSize
            this.agvs.currentPage = pagination.CurrentPage
            this.agvs.totalPages = pagination.TotalPages
            this.agvs.totalCount = pagination.TotalCount
          }
        })
        .catch((response) => {
          this.agvs.error = response.error
          console.error(response.error)
        })
        .finally(() => (this.agvs.loading = false))
    },
    async updateAgv(agv: IAgv) {
      if (this.agvs.data.has(agv.correlationId)) {
        this.agvs.data.set(agv.correlationId, agv)
      }
    },
    fetchGraph: async function (correlationId: string): Promise<void> {
      this.agvGraph.loading = true

      return await agvApi
        .graphDetail(correlationId)
        .then((response) => {
          this.agvGraph.error = null
          if (response.data.correlationId != null) {
            this.agvGraph.data.set(response.data.correlationId, response.data)
          }
        })
        .catch((response) => {
          this.agvGraph.error = response.error
          console.error(response.error)
        })
        .finally(() => (this.agvGraph.loading = false))
    },
    fetchMaps: async function (): Promise<void> {
      this.maps.loading = true

      return await mapApi
        .mapsList()
        .then((response) => {
          this.maps.error = null

          const data = new Map<string, IMap>()
          response.data.forEach((map: IMap) => {
            if (map.correlationId != null) {
              data.set(map.correlationId, map)
            }
          })
          this.maps.data = data
        })
        .catch((response) => {
          this.maps.error = response.error
          console.error(response.error)
        })
        .finally(() => (this.maps.loading = false))
    },
    MapUrl: function (correlationId: string): string {
      return `${mapApi.baseUrl}/maps/${correlationId}/map.png`
    }
  }
})
