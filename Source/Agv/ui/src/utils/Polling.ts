/**
 * Polling Class
 */
export class Polling {
  _polling: ReturnType<typeof setInterval> | null = null
  _callback: () => void
  _interval: number

  /**
   * Polling constructor
   * @param callback - Method to be called periodically
   * @param interval - Interval time in ms
   */
  constructor(callback: () => void, interval: number) {
    this._callback = callback
    this._interval = interval
  }

  /**
   * Start polling
   */
  start(): void {
    if (this._polling == null) {
      this._polling = setInterval(() => {
        this._callback()
      }, this._interval)
    }
  }

  /**
   * Stop polling
   */
  stop(): void {
    if (this._polling != null) {
      clearInterval(this._polling)
      this._polling = null
    }
  }

  /**
   * Toggle polling
   */
  toggle(): void {
    if (this._polling == null) {
      this.start()
    } else {
      this.stop()
    }
  }

  /**
   * Is polling enabled
   */
  pollingEnabled(): boolean {
    return this._polling != null
  }
}
