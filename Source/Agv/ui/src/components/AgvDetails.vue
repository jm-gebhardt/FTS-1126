<template>
  <div id="div-outside">
    <div class="div-inside">
      <ui-list-group-subheader>General information</ui-list-group-subheader>
      <details-list v-model="generalItems"></details-list>
      <ui-list-divider></ui-list-divider>
      <ui-list-group-subheader>Status information</ui-list-group-subheader>
      <details-list v-model="statusItems"></details-list>
      <ui-list-divider></ui-list-divider>
      <battery-charge-indicator :charge="props.agvState?.batteryCharge" />
      <ui-list-divider></ui-list-divider>
      <ui-list-group-subheader>Data information</ui-list-group-subheader>
      <details-list v-model="dataItems"></details-list>
      <ui-list-divider></ui-list-divider>
      <ui-list-group-subheader>Actions</ui-list-group-subheader>
      <actions-list v-model="actionItems"></actions-list>
    </div>
    <div class="div-inside">
      <img :src="agvImagePath" width="720" alt="png file not found" />
    </div>
  </div>
</template>

<script lang="ts">
export default {
  name: 'AgvDetails'
}
</script>

<script lang="ts" setup>
import { IAgv, IAgvData, IAgvState } from '@/services/api/data-contracts'
import { computed, ComputedRef, onMounted } from 'vue'
import DetailsList from '@/components/DetailsList.vue'
import ActionsList from '@/components/ActionsList.vue'
import BatteryChargeIndicator from '@/components/BatteryChargeIndicator.vue'
import { ListItem } from '@/components/ListItem'
import { useAgvStore } from '@/services/AgvStore'
import {
  UiListGroupSubheader,
  UiListDivider,
  useAlert,
  useConfirm
} from 'balm-ui'
import { ActionItem } from './ActionItem'
import { HttpResponse } from '@microsoft/signalr'
import { Agvs as AgvApi } from '@/services/api/Agvs'

const { fetchAgvs } = useAgvStore()

const agvApi = new AgvApi({
  baseUrl: import.meta.env.VITE_API_URL
})

interface Props {
  modelValue: IAgv | null
  agvData: IAgvData | null | undefined
  agvState: IAgvState | null | undefined
}

const props = defineProps<Props>()

let generalItems: ComputedRef<ListItem[]> = computed(() => {
  if (props.modelValue == null) {
    return [] as ListItem[]
  }
  return [
    // General information
    { label: 'CorrelationId', value: props.modelValue?.correlationId },
    { label: 'AGV ID', value: props.modelValue?.agvId }
  ] as ListItem[]
})

let statusItems: ComputedRef<ListItem[]> = computed(() => {
  if (props.modelValue == null) {
    return [] as ListItem[]
  }
  return [
    // Status information
    { label: 'OrderID', value: props.agvState?.orderId },
    { label: 'Busy', value: props.agvState?.busy },
    { label: 'Automatic', value: props.agvState?.automatic },
    { label: 'Disturbed', value: props.agvState?.disturbed },
    { label: 'LastNodeID', value: props.agvState?.lastNodeId },
    { label: 'Charging', value: props.agvState?.charging }
  ] as ListItem[]
})

let dataItems: ComputedRef<ListItem[]> = computed(() => {
  if (props.modelValue == null) {
    return [] as ListItem[]
  }
  return [
    // Data information
    { label: 'Model', value: props.agvData?.model },
    { label: 'IPv4 Address', value: props.agvData?.ipV4Address },
    { label: 'Payload', value: props.agvData?.payload }
  ] as ListItem[]
})

let actionItems: ComputedRef<ActionItem<IAgv>[]> = computed(() => {
  if (props.modelValue == null) {
    return [] as ActionItem<IAgv>[]
  }
  return [
    // Actions
    {
      label: 'Set Idle',
      data: props.modelValue,
      action: setAgvIdle
    },
    {
      label: 'Lock',
      data: props.modelValue,
      action: lockAgv
    },
    {
      label: 'Refresh Parameters',
      data: props.modelValue,
      action: refreshParameters
    }
  ] as ActionItem<IAgv>[]
})

let agvImagePath: ComputedRef<string | undefined> = computed(() => {
  return import.meta.env.VITE_BASE_PATH + props.agvData?.model + '.png'
})

function setAgvIdle(agv: IAgv | null): void {
  if (agv === null) return

  const $confirm = useConfirm()

  $confirm(`Set state of AGV '${agv.agvId}' to Idle?`)
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    .then((result: any) => {
      // According to the balm-ui documentation 'result' should be a boolean but in the node module code it's declared as a void
      // During runtime it actually is a boolean, so we need to cast it
      if (result as boolean) {
        agvApi
          .setIdlePartialUpdate(agv.correlationId)
          .catch((reason: HttpResponse) => {
            console.error('Set AGV idle: ' + reason.statusText)
            const $alert = useAlert()
            $alert(`AGV '${agv.agvId}' could not be set to Idle`)
          })
      }
    })
}

function lockAgv(agv: IAgv | null): void {
  if (agv === null) return

  const $confirm = useConfirm()

  $confirm(`Lock AGV '${agv.agvId}'?`)
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    .then((result: any) => {
      // According to the balm-ui documentation 'result' should be a boolean but in the node module code it's declared as a void
      // During runtime it actually is a boolean, so we need to cast it
      if (result as boolean) {
        agvApi
          .lockAgvPartialUpdate(agv.correlationId)
          .catch((reason: HttpResponse) => {
            console.error('lock agv: ' + reason.statusText)
            const $alert = useAlert()
            $alert(`AGV '${agv.agvId}' could not be locked`)
          })
      }
    })
}

function refreshParameters(agv: IAgv | null): void {
  if (agv === null) return

  agvApi.emitRequestDataPartialUpdate(agv.correlationId)
  agvApi.emitRequestGraphPartialUpdate(agv.correlationId)
}

onMounted(() => {
  fetchAgvs()
})
</script>

<style lang="scss">
#div-outside {
  display: flex;
}
.div-inside {
  width: 800px;
}
</style>
