import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState
} from '@microsoft/signalr'
import { IHubMethods, IOrder } from '@/services/api/data-contracts'

/**
 * Signalr hub for orders
 */
export class OrderHub {
  client: HubConnection
  _onOrderStateChanged?: (order: IOrder) => Promise<void>

  constructor(onOrderStateChanged: (order: IOrder) => Promise<void>) {
    this._onOrderStateChanged = onOrderStateChanged

    const hubUrl = import.meta.env.VITE_HUB_URL
    this.client = new HubConnectionBuilder().withUrl(hubUrl + '/order').build()

    // Hub endpoints with respective handlers
    this.client.on(IHubMethods.OrderStateChanged, (message: string) => {
      if (this._onOrderStateChanged)
        this._onOrderStateChanged(JSON.parse(message))
    })
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
      .catch((e) => console.error('OrderHub Connection failed: ', e))
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
      .catch((e) => console.error('OrderHub Disconnect failed: ', e))
  }
}
