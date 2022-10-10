import { describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createTestingPinia } from '@pinia/testing'
import OrdersView from '@/views/OrdersView.vue'
import { useOrderStore } from '@/services/OrderStore'

describe('view', () => {
  describe('OrdersView', () => {
    const orderView = mount(OrdersView, {
      global: {
        plugins: [
          createTestingPinia({
            createSpy: vi.fn
          })
        ]
      }
    })

    const store = useOrderStore()

    it('Header is "Orders"', () => {
      expect(orderView.findAll('h1').at(0)?.text()).toBe('Orders')
    })
    it('Data fetched', () => {
      expect(store.fetchOrders).toHaveBeenCalledTimes(1)
    })
  })
})
