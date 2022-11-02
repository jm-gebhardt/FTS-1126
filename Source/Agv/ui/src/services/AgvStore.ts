import {
  IAgv,
  IAgvData,
  IGraph,
  IAgvStatus,
  IMap
} from '@/services/api/data-contracts'
import { IRestApiStore } from '@/services/IRestApiStore'
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
  agvs: IRestApiStore<Map<string, IAgv>>
  agvGraph: IRestApiStore<Map<string, IGraph>>
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
      error: null
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
    fetchAgvs: async function (): Promise<void> {
      this.agvs.loading = true

      return await agvApi
        .agvsList()
        .then((response) => {
          this.agvs.error = null
          const data = new Map<string, IAgv>()
          response.data?.forEach((agv: IAgv) => {
            if (agv.correlationId != null) {
              data.set(agv.correlationId, agv)
            }
          })
          this.agvs.data = data
        })
        .catch((response) => {
          // HTTP code 204 = No Content
          if (response.status == 204) {
            this.agvData.data.clear()
          } else {
            this.agvs.error = response.error
            console.error(response.error)
          }
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
          if (response.data?.correlationId != null) {
            this.agvGraph.data.set(response.data.correlationId, response.data)
          }
        })
        .catch((response) => {
          // HTTP code 204 = No Content
          // Entry in AgvRegistry is given but AgvGraph column is NULL
          if (response.status == 204) {
            this.agvGraph.data.clear()
          } else {
            this.agvGraph.error = response.error
            console.error(response.error)
          }
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
          response.data?.forEach((map: IMap) => {
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
    },
    fetchData: async function (correlationId: string): Promise<void> {
      this.agvData.loading = true

      return await agvApi
        .dataDetail(correlationId)
        .then((response) => {
          this.agvData.error = null
          if (response.data?.correlationId != null) {
            this.agvData.data.set(response.data.correlationId, response.data)
          }
        })
        .catch((response) => {
          // HTTP code 204 = No Content
          // Entry in AgvRegistry is given but AgvData column is NULL
          if (response.status == 204) {
            this.agvData.data.clear()
          } else {
            this.agvData.error = response.error
            console.error(response.error)
          }
        })
        .finally(() => (this.agvData.loading = false))
    },
    fetchStatus: async function (correlationId: string): Promise<void> {
      this.agvStatus.loading = true

      return await agvApi
        .statusDetail(correlationId)
        .then((response) => {
          this.agvStatus.error = null
          if (response.data?.correlationId != null) {
            this.agvStatus.data.set(response.data.correlationId, response.data)
          }
        })
        .catch((response) => {
          if (response.status == 204) {
            // HTTP code 204 = No Content
            // Entry in AgvRegistry is given but AgvStatus column is NULL
            this.agvStatus.data.clear()
          } else {
            this.agvStatus.error = response.error
            console.error(response.error)
          }
        })
        .finally(() => (this.agvStatus.loading = false))
    }
  }
})
