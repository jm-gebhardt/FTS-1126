import { describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createTestingPinia } from '@pinia/testing'
import OrdersView from '@/views/OrdersView.vue'

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

    it('Header is "Orders"', () => {
      expect(orderView.findAll('h1').at(0)?.text()).toBe('Orders')
    })
  })
})
