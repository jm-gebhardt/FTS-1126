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
  _onAgvStateChanged?: (agv: IAgv) => Promise<void>
  _onAgvPositionChanged?: (
    agvPositionStatus: IAgvPositionStatus
  ) => Promise<void>

  constructor(
    onAgvDiscovered?: () => Promise<void>,
    onAgvStateChanged?: (agv: IAgv) => Promise<void>,
    onAgvPositionChanged?: (
      agvPositionStatus: IAgvPositionStatus
    ) => Promise<void>
  ) {
    this._onAgvDiscovered = onAgvDiscovered
    this._onAgvStateChanged = onAgvStateChanged
    this._onAgvPositionChanged = onAgvPositionChanged

    const hubUrl = import.meta.env.VITE_HUB_URL
    this.client = new HubConnectionBuilder().withUrl(hubUrl + '/agv').build()

    // Hub endpoints with respective handlers
    this.client.on(IHubMethods.AgvDiscovered, async () => {
      if (this._onAgvDiscovered) this._onAgvDiscovered()
    })

    this.client.on(IHubMethods.AgvStateChanged, async (message: string) => {
      if (this._onAgvStateChanged) this._onAgvStateChanged(JSON.parse(message))
    })

    this.client.on(IHubMethods.AgvPositionChanged, async (message: string) => {
      if (this._onAgvPositionChanged)
        this._onAgvPositionChanged(JSON.parse(message))
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
