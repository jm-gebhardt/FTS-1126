import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { IHostOrderCreator, IOrder } from '@/services/api/data-contracts'
import OrderList from '@/components/OrderList.vue'
import { UiTable } from 'balm-ui'

describe('view', () => {
  describe('OrderList', () => {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    let orderList: any = null

    const testOrders: IOrder[] = [
      {
        correlationId: 'F9114816-CEC4-CCFD-2106-3AAF67C954B4',
        currentState: 'OrderReceived',
        assignedAgv: '',
        orderDetails: {
          orderId: 'Order01',
          source: 'S1',
          destination: 'D1',
          creationDate: '2022-01-01 00:01:00',
          correlationId: 'F9114816-CEC4-CCFD-2106-3AAF67C954B4',
          leNo: '001',
          creator: IHostOrderCreator.Simulation
        }
      },
      {
        correlationId: '31E0077A-8BBE-745F-B21C-FF8D58250532',
        currentState: 'AgvRequested',
        assignedAgv: '',
        orderDetails: {
          orderId: 'Order02',
          source: 'S2',
          destination: 'D1',
          creationDate: '2022-01-01 00:02:00',
          correlationId: '31E0077A-8BBE-745F-B21C-FF8D58250532',
          leNo: '002',
          creator: IHostOrderCreator.Simulation
        }
      },
      {
        correlationId: 'DC9B0948-5832-6C9D-BB8E-2EA1E27598DF',
        currentState: 'Final',
        assignedAgv: '',
        orderDetails: {
          orderId: 'Order03',
          source: 'S1',
          destination: 'D2',
          creationDate: '2022-01-01 00:03:00',
          correlationId: 'DC9B0948-5832-6C9D-BB8E-2EA1E27598DF',
          leNo: '003',
          creator: IHostOrderCreator.Simulation
        }
      },
      {
        correlationId: '9B8985B0-8DA4-33AB-C148-AF494B449F4F',
        currentState: 'OrderFailed',
        assignedAgv: '',
        orderDetails: {
          orderId: 'Order04',
          source: 'S2',
          destination: 'D2',
          creationDate: '2022-01-01 00:03:00',
          correlationId: '9B8985B0-8DA4-33AB-C148-AF494B449F4F',
          leNo: '004',
          creator: IHostOrderCreator.Simulation
        }
      }
    ]

    // SETUP - run before to each unit test
    beforeEach(() => {
      // render the component
      orderList = mount(OrderList, {
        global: {
          components: {
            'ui-table': UiTable
          }
        },
        props: {
          orders: testOrders
        }
      })
    })

    // TEARDOWN - run after to each unit test
    afterEach(() => {
      orderList.unmount()
    })

    it('Order count is correct', () => {
      //expect(orderList.findAll('.mdc-data-table').length).toBe(4)
    })

    it('Pageination - Total page count is correct', async () => {
      const result = orderList.findAll('.mdc-data-table__pagination-total')
      const text = result.at(0)?.text()
      expect(text).toBe('1‑0 of 0') // '‑' ist kein Minus?
    })

    it('mock test', () => {
      const fn = vi.fn()

      fn('hello', 1)

      expect(vi.isMockFunction(fn)).toBe(true)
      expect(fn.mock.calls[0]).toEqual(['hello', 1])

      fn.mockImplementation((arg) => arg)

      fn('world', 2)

      expect(fn.mock.results[1].value).toBe('world')
    })
  })
})
