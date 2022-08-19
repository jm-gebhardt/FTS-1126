import { defineStore } from 'pinia'
import { IOrder } from '@/services/api/data-contracts'
import { Order as OrderApi } from '@/services/api/Order'

interface OrderStoreState {
  orders: IOrder[]
  currentPage: number
  pageSize: number
  totalCount: number
  totalPages: number
  error: string | null
  loading: boolean
}

const orderApi = new OrderApi({
  baseUrl: import.meta.env.VITE_API_URL
})

export const useOrderStore = defineStore({
  id: 'orderStore',
  state: (): OrderStoreState => ({
    orders: [],
    loading: false,
    error: null,
    currentPage: 1,
    pageSize: 1,
    totalPages: 1,
    totalCount: 0
  }),

  actions: {
    async fetchOrders(pageNumber: number, pageSize: number) {
      this.loading = true

      orderApi
        .orderList({ PageNumber: pageNumber, PageSize: pageSize })
        .then((response) => {
          this.error = null
          this.orders = response.data
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
    async updateOrderList(order: IOrder) {
      if (!this.orders || this.orders.length < 1) return // No orders to update

      for (let i = 0; i < this.orders.length; i++) {
        const element = this.orders[i]
        if (element.correlationId === order.correlationId) {
          this.orders[i] = order
          return
        }
      }

      // If this is the first page check if a new order can be added to the list
      if (this.currentPage === 1) {
        const lastOrder = this.orders[0]

        if (!lastOrder.orderDetails?.creationDate) return
        if (!order.orderDetails?.creationDate) return

        // Add order to list if it is newer than the latest order
        if (
          order.orderDetails?.creationDate >
          lastOrder.orderDetails?.creationDate
        ) {
          this.orders.splice(0, 0, order) // Add new order
          this.totalCount++

          // Trim orders array to page size
          if (this.orders.length > this.pageSize) {
            this.orders.pop()
          }
        }
      }
    }
  }
})
