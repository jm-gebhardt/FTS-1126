import { defineStore } from 'pinia'
import { IOrder } from '@/services/api/data-contracts'
import { Orders as OrderApi } from '@/services/api/Orders'
import { IDateRangeRestApiStore } from '@/services/IRestApiStore'

type OrderStoreState = IDateRangeRestApiStore<IOrder[]>

const orderApi = new OrderApi({
  baseUrl: import.meta.env.VITE_API_URL
})

export const useOrderStore = defineStore({
  id: 'orderStore',
  state: (): OrderStoreState => ({
    data: [],
    loading: false,
    error: null,
    currentPage: 1,
    pageSize: 1,
    totalPages: 1,
    totalCount: 0,
    startDate: null,
    endDate: null
  }),

  actions: {
    async fetchOrders(
      pageNumber: number,
      pageSize: number,
      start?: Date,
      end?: Date
    ) {
      this.loading = true

      await orderApi
        .ordersList({
          PageNumber: pageNumber,
          PageSize: pageSize,
          Start: start != undefined ? new Date(start).toISOString() : undefined,
          End: end != undefined ? new Date(end).toISOString() : undefined
        })
        .then((response) => {
          this.error = null
          this.data = response.data
          const paginationHeader = response.headers.get('x-pagination')
          const dateRangeHeader = response.headers.get('x-dateRange')
          if (paginationHeader != null) {
            const pagination = JSON.parse(paginationHeader)
            this.pageSize = pagination.PageSize
            this.currentPage = pagination.CurrentPage
            this.totalPages = pagination.TotalPages
            this.totalCount = pagination.TotalCount
          }
          if (dateRangeHeader != null) {
            const pagination = JSON.parse(dateRangeHeader)
            this.startDate = pagination.Start
            this.endDate = pagination.End
          }
        })
        .catch((response) => {
          this.error = `${response.status}: ${response.statusText} | ${response.error}`
          console.error(response.status, response.statusText, response.error)
        })
        .finally(() => (this.loading = false))
    },
    async updateOrderList(order: IOrder) {
      if (!this.data || this.data.length < 1) return // No orders to update

      for (let i = 0; i < this.data.length; i++) {
        const element = this.data[i]
        if (element.correlationId === order.correlationId) {
          this.data[i] = order
          return
        }
      }

      // If this is the first page check if a new order can be added to the list
      if (this.currentPage === 1) {
        const lastOrder = this.data[0]

        if (!lastOrder.orderDetails?.creationDate) return
        if (!order.orderDetails?.creationDate) return

        // Add order to list if it is newer than the latest order
        if (
          order.orderDetails?.creationDate >
          lastOrder.orderDetails?.creationDate
        ) {
          this.data.splice(0, 0, order) // Add new order
          this.totalCount++

          // Trim orders array to page size
          if (this.data.length > this.pageSize) {
            this.data.pop()
          }
        }
      }
    }
  }
})
