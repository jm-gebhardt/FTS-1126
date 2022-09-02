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
    this.client.on('AgvStateChanged', async (message: string) => {
      this.onAgvStateChanged(message)
    })
    this.client.on('AgvDiscovered', async () => {
      // When a new agv has been added, we fetch the full list as we do not know an what page it is
      await this.store.fetchAgvs(
        this.store.agvs.currentPage,
        this.store.agvs.pageSize
      )
    })
  }

  /**
   * Updates agv in agv list view
   * @param message JSON ecnoded IOrder object
   */
  private async onAgvStateChanged(message: string): Promise<void> {
    const agv: IAgv = JSON.parse(message)
    await this.store.updateAgv(agv)
  }

  /**
   * Checks if signalr hub is connected
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
