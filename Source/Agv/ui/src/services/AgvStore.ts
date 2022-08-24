import { defineStore } from 'pinia'
import { IAgv } from '@/services/api/data-contracts'
import { Agv as AgvApi } from '@/services/api/Agv'

interface AgvStoreState {
  agvs: IAgv[]
  currentPage: number
  pageSize: number
  totalCount: number
  totalPages: number
  error: string | null
  loading: boolean
}

const agvApi = new AgvApi({
  baseUrl: import.meta.env.VITE_API_URL
})

export const useAgvStore = defineStore({
  id: 'agvStore',
  state: (): AgvStoreState => ({
    agvs: [],
    loading: false,
    error: null,
    currentPage: 1,
    pageSize: 1,
    totalPages: 1,
    totalCount: 0
  }),

  actions: {
    async fetchAgvs(pageNumber: number, pageSize: number) {
      this.loading = true

      agvApi
        .getAgvList({ PageNumber: pageNumber, PageSize: pageSize })
        .then((response) => {
          this.error = null
          this.agvs = response.data
          const paginationHeader = response.headers.get('x-pagination')
          if (paginationHeader != null) {
            const pagination = JSON.parse(paginationHeader)
            this.pageSize = pagination.PageSize
            this.currentPage = pagination.CurrentPage
            this.totalPages = pagination.TotalPages
            this.totalCount = pagination.TotalCount
          }
        })
        .catch((response) => {
          this.error = response.error
          console.log(response.error)
        })
        .finally(() => (this.loading = false))
    },
    async updateAgvList(agv: IAgv) {
      if (!this.agvs || this.agvs.length < 1) return // No agvs to update

      for (let i = 0; i < this.agvs.length; i++) {
        const element = this.agvs[i]
        if (element.correlationId === agv.correlationId) {
          this.agvs[i] = agv
          return
        }
      }

      // If this is the first page check if a new agv can be added to the list
      if (this.currentPage === 1) {
        const lastAgv = this.agvs[0]

        if (!lastAgv.agvId) return
        if (!agv.agvId) return

        // Add agv to list if it is newer than the latest agv
        if (agv.agvId > lastAgv.agvId) {
          this.agvs.splice(0, 0, agv) // Add new agv
          this.totalCount++

          // Trim agvs array to page size
          if (this.agvs.length > this.pageSize) {
            this.agvs.pop()
          }
        }
      }
    }
  }
})
