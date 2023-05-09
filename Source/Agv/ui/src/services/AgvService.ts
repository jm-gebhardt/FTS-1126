import { Agvs as AgvApi } from '@/services/api/Agvs'
import { useAlert } from 'balm-ui'

export abstract class AgvService {
  static agvApi = new AgvApi({
    baseUrl: import.meta.env.VITE_API_URL
  })

  public static unlockAgv(id: string): void {
    AgvService.agvApi.unlockPartialUpdate(id).catch((response) => {
      console.error('Set AGV idle: ' + response.statusText)
      response.text().then((body: unknown) => {
        const $alert = useAlert()
        $alert(`AGV '${id}' could not be set to Idle. ${body}`)
      })
    })
  }

  public static lockAgv(id: string): void {
    AgvService.agvApi.lockPartialUpdate(id).catch((response) => {
      console.error('lock agv: ' + response.statusText)
      response.text().then((body: unknown) => {
        const $alert = useAlert()
        $alert(`AGV '${id}' could not be locked. ${body}`)
      })
    })
  }
}
