import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState
} from '@microsoft/signalr'
import { IAgv } from '@/services/api/data-contracts'
import { useAgvStore } from './AgvStore'

/**
 * Signalr hub for orders
 */
export class AgvHub {
  client: HubConnection
  store = useAgvStore()

  constructor() {
    const hubUrl = import.meta.env.VITE_HUB_URL
    this.client = new HubConnectionBuilder().withUrl(hubUrl + '/agv').build()

    // Hub endpoints with respective handlers
    this.client.on('AgvStateChanged', (message: string) => {
      this.onAgvStateChanged(message)
    })
  }

  /**
   * Updates agv in agv list view
   * @param message JSON ecnoded IOrder object
   */
  private onAgvStateChanged(message: string): void {
    const agv: IAgv = JSON.parse(message)
    this.store.updateAgvList(agv)
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
    console.log('AgvHub connecting...')

    this.client
      .start()
      .then(() => {
        console.log('AgvHub Connected')
      })
      .catch((e) => console.log('AgvHub Connection failed: ', e))
  }

  /**
   * Disconnects signalr hub
   */
  public disconnect(): void {
    console.log('AgvHub disconnecting...')

    this.client
      .stop()
      .then(() => {
        console.log('AgvHub Disconnected')
      })
      .catch((e) => console.log('AgvHub Disconnect failed: ', e))
  }
}
