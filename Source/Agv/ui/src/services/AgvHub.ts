import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState
} from '@microsoft/signalr'
import {
  IAgv,
  IAgvPositionStatus,
  IHubMethods
} from '@/services/api/data-contracts'

/**
 * Signalr hub for orders
 */
export class AgvHub {
  client: HubConnection
  _onAgvDiscovered?: () => Promise<void>
  _onAgvChanged?: (agvState: IAgv) => Promise<void>
  _onAgvPositionChanged?: (
    agvPositionStatus: IAgvPositionStatus
  ) => Promise<void>

  constructor(
    onAgvDiscovered?: () => Promise<void>,
    onAgvChanged?: (agvState: IAgv) => Promise<void>,
    onAgvPositionChanged?: (
      agvPositionStatus: IAgvPositionStatus
    ) => Promise<void>
  ) {
    this._onAgvDiscovered = onAgvDiscovered
    this._onAgvChanged = onAgvChanged
    this._onAgvPositionChanged = onAgvPositionChanged

    const hubUrl = import.meta.env.VITE_HUB_URL
    this.client = new HubConnectionBuilder().withUrl(hubUrl + '/agv').build()

    // Hub endpoints with respective handlers
    this.client.on(IHubMethods.AgvDiscovered, async () => {
      if (this._onAgvDiscovered) await this._onAgvDiscovered()
    })

    this.client.on(IHubMethods.AgvChanged, async (message: string) => {
      if (this._onAgvChanged) await this._onAgvChanged(JSON.parse(message))
    })

    this.client.on(IHubMethods.AgvPositionChanged, async (message: string) => {
      if (this._onAgvPositionChanged)
        await this._onAgvPositionChanged(JSON.parse(message))
    })
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
      .catch((e) => console.error('AgvHub Connection failed: ', e))
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
      .catch((e) => console.error('AgvHub Disconnect failed: ', e))
  }
}
