import { HttpResponse } from '@microsoft/signalr'
import { Agvs as AgvApi } from '@/services/api/Agvs'
import { useAlert } from 'balm-ui'

export abstract class AgvService {
  static agvApi = new AgvApi({
    baseUrl: import.meta.env.VITE_API_URL
  })

  public static setAgvIdle(id: string): void {
    AgvService.agvApi.setIdlePartialUpdate(id).catch((reason: HttpResponse) => {
      console.error('Set AGV idle: ' + reason.statusText)
      const $alert = useAlert()
      $alert(`AGV '${id}' could not be set to Idle`)
    })
  }

  public static lockAgv(id: string): void {
    AgvService.agvApi.lockAgvPartialUpdate(id).catch((reason: HttpResponse) => {
      console.error('lock agv: ' + reason.statusText)
      const $alert = useAlert()
      $alert(`AGV '${id}' could not be locked`)
    })
  }
}
