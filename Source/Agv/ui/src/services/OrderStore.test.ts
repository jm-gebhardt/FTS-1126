import { beforeEach, describe, expect, it } from 'vitest'
import { useOrderStore } from '@/services/OrderStore'
import { IHostOrderCreator, IOrder } from '@/services/api/data-contracts'
import { createPinia, setActivePinia } from 'pinia'
import { createApp, UnwrapRef } from 'vue'

describe('view', () => {
  describe('dataView', () => {
    const testState = {
      data: [] as IOrder[],
      currentPage: 1,
      pageSize: 10,
      totalCount: 4,
      totalPages: 2,
      error: null,
      loading: false,
      startDate: null,
      endDate: null
    }
    const app = createApp({}) // Init vue app
    let testdata: IOrder[] = []

    beforeEach(() => {
      // init pina store
      const pinia = createPinia()
      app.use(pinia)
      setActivePinia(pinia)

      // set test data
      testdata = [
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
      testState.data = testdata
    })

    function createOrder(
      correlationId: string,
      orderId: string,
      currentState = 'OrderReceived',
      creationDate = ''
    ): IOrder {
      return {
        correlationId: correlationId,
        currentState: currentState,
        assignedAgv: '',
        orderDetails: {
          orderId: orderId,
          source: 'S1',
          destination: 'D1',
          creationDate: creationDate,
          correlationId: correlationId,
          leNo: '004',
          creator: IHostOrderCreator.Simulation
        }
      }
    }

    it('Order count is correct (static test)', () => {
      const store = useOrderStore()
      store.$state = testState

      expect(store.data.length).toBe(4)
    })

    it('updateOrderList - Update existing', () => {
      const store = useOrderStore()
      store.$state = testState

      expect(store.data.length).toBe(4)
      expect(store.data[0].currentState).toBe('OrderReceived')

      const correlationId = 'F9114816-CEC4-CCFD-2106-3AAF67C954B4'
      const orderId = 'Order01'
      const newState = 'AgvRequested' // OrderReceived -> AgvRequested
      const order: IOrder = createOrder(correlationId, orderId, newState)

      store.updateOrderList(order)
      expect(store.data.length).toBe(4) // same count

      const index = store.data.indexOf(order)
      expect(index).toBe(0)

      const newOrder = store.data.find(
        (element) => element.correlationId == correlationId
      )
      expect(newOrder?.orderDetails?.orderId).toBe(orderId)
      expect(newOrder?.currentState).toBe('AgvRequested') // new state
    })

    it('updateOrderList - Add new', () => {
      const store = useOrderStore()
      store.$state = testState

      expect(store.data.length).toBe(4)
      expect(store.data[0].currentState).toBe('OrderReceived')

      const correlationId = 'A3686A5B-2171-977C-140C-C943284F2FA0' // new id
      const orderId = 'Order05'
      const state = 'OrderReceived'
      const creationDate = '2022-01-01 00:05:00' // new creation date
      const order: IOrder = createOrder(
        correlationId,
        orderId,
        state,
        creationDate
      )

      store.updateOrderList(order)
      expect(store.data.length).toBe(5) // new count

      const index = store.data.indexOf(order)
      expect(index).toBe(0)

      const newOrder = store.data.find(
        (element) => element.correlationId == correlationId
      )
      expect(newOrder?.orderDetails?.orderId).toBe(orderId)
    })

    it('updateOrderList - no change because Order is not in list', () => {
      const store = useOrderStore()
      store.$state = testState

      expect(store.data.length).toBe(4)
      expect(store.data[0].currentState).toBe('OrderReceived')

      const correlationId = 'A3686A5B-2171-977C-140C-C943284F2FA0' // new id
      const orderId = 'Order05'
      const state = 'OrderReceived'
      const creationDate = '2022-01-01 00:00:00' // old creation date
      const order: IOrder = createOrder(
        correlationId,
        orderId,
        state,
        creationDate
      )

      store.updateOrderList(order)
      expect(store.data.length).toBe(4) // same count

      const index = store.data.indexOf(order)
      expect(index).toBe(-1) // not found

      const newOrder = store.data.find(
        (element) => element.correlationId == correlationId
      )
      expect(newOrder).toBeUndefined() // not found
    })

    it('updateOrderList - no change because not on page 1', () => {
      const store = useOrderStore()
      store.$state = testState
      store.$state.currentPage = 2

      expect(store.data.length).toBe(4)
      expect(store.data[0].currentState).toBe('OrderReceived')

      const correlationId = 'A3686A5B-2171-977C-140C-C943284F2FA0' // new id
      const orderId = 'Order05'
      const state = 'OrderReceived'
      const creationDate = '2022-01-01 00:00:00' // old creation
      const order: IOrder = createOrder(
        correlationId,
        orderId,
        state,
        creationDate
      )

      store.updateOrderList(order)
      expect(store.data.length).toBe(4) // same count

      const index = store.data.indexOf(order)
      expect(index).toBe(-1) // not found

      const newOrder = store.data.find(
        (element: UnwrapRef<IOrder>) => element.correlationId == correlationId
      )
      expect(newOrder).toBeUndefined() // not found
    })
  })
})
