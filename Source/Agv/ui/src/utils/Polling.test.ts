import { describe, it, expect } from 'vitest'
import { Polling } from './Polling'

describe('utils', () => {
  describe('Polling', () => {
    const polling = new Polling(() => {
      console.log('test')
    }, 1000)

    it('start_stop', () => {
      expect(polling.pollingEnabled()).toBe(false)
      polling.start()
      expect(polling.pollingEnabled()).toBe(true)
      polling.stop()
      expect(polling.pollingEnabled()).toBe(false)
    })
  })
})
