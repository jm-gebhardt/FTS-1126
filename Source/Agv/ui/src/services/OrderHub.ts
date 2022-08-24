import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState
} from '@microsoft/signalr'
import { IOrder } from '@/services/api/data-contracts'
import { useOrderStore } from './OrderStore'

/**
 * Signalr hub for orders
 */
export class OrderHub {
  client: HubConnection
  store = useOrderStore()

  constructor() {
    const hubUrl = import.meta.env.VITE_HUB_URL
    this.client = new HubConnectionBuilder().withUrl(hubUrl + '/order').build()

    // Hub endpoints with respective handlers
    this.client.on('OrderStateChanged', (message: string) => {
      this.onTransportOrderChanged(message)
    })
  }

  /**
   * Updates order in order list view
   * @param message JSON ecnoded IOrder object
   */
  private onTransportOrderChanged(message: string): void {
    const order: IOrder = JSON.parse(message)
    this.store.updateOrderList(order)
  }

  /**
   * Checks if signalr hub is conneced
   * @returns True if hub is connected, otherwise false
   */
  public isConnected(): boolean {
    return this.client.state === HubConnectionState.Connected
  }

  /**
   * Connects signalr hub to server
   */
  public connect(): void {
    console.log('OrderHub connecting...')

    this.client
      .start()
      .then(() => {
        console.log('OrderHub Connected')
      })
      .catch((e) => console.log('OrderHub Connection failed: ', e))
  }

  /**
   * Disconnects signalr hub
   */
  public disconnect(): void {
    console.log('OrderHub disconnecting...')

    this.client
      .stop()
      .then(() => {
        console.log('OrderHub Disconnected')
      })
      .catch((e) => console.log('OrderHub Disconnect failed: ', e))
  }
}
