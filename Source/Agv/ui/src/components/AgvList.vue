<template>
  <ui-table :data="props.agvs" :thead="thead" :tbody="tbody">
    <template #CurrentState="{ data }">
      <div class="tableCell-align-middle--icon">
        <ui-icon>{{ AgvStatusIcon(data.currentState) }}</ui-icon>
      </div>
      <div class="tableCell-align-middle">{{ data.currentState }}</div>
    </template>
    <template #actions="{ data }">
      <button @click="clickSetIdle(data)">Set Idle</button>
    </template>
  </ui-table>
</template>

<script lang="ts">
export default {
  name: 'AgvList',
  inheritAttrs: false
}
</script>

<script lang="ts" setup>
import { IAgv } from '@/services/api/data-contracts'
import { useConfirm, useAlert } from 'balm-ui'
import { Agvs as AgvApi } from '@/services/api/Agvs'
import { HttpResponse } from '@microsoft/signalr'

interface Props {
  agvs: IAgv[]
}

const agvApi = new AgvApi({
  baseUrl: import.meta.env.VITE_API_URL
})

const props = defineProps<Props>()

const thead = ['ID', 'Agv ID', 'Current State', 'Actions']
const tbody = [
  'correlationId',
  {
    field: 'agvId',
    fn: (agv: IAgv): string | null | undefined => agv.agvId
  },
  {
    field: 'currentState',
    slot: 'CurrentState'
  },
  {
    slot: 'actions'
  }
]

function AgvStatusIcon(status: string | null | undefined): string {
  switch (status) {
    case 'Discovered':
      return 'fiber_new'
    case 'Idle':
      return 'pending'
    case 'Reserved':
      return 'save_alt'
    default:
      return '🤔'
  }
}

function clickSetIdle(agv: IAgv): void {
  const $confirm = useConfirm()

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  $confirm(`Set state of AGV '${agv.agvId}' to Idle?`).then((result: any) => {
    // According to the balm-ui documentation 'result' should be a boolean but in the node module code it's declared as a void
    // During runtime it actually is a boolean, so we need to cast it
    if (result as boolean) {
      agvApi
        .setIdlePartialUpdate(agv.correlationId)
        .then((value) => {
          console.log('setIdle: ' + value.statusText)
        })
        .catch((reason: HttpResponse) => {
          console.log('setIdle: ' + reason.statusText)
          const $alert = useAlert()
          $alert(`AGV '${agv.agvId}' could not be set to Idle`)
        })
    }
  })
}
</script>

<style lang="scss">
.tableCell-align-middle {
  display: inline-block;
  vertical-align: middle;

  &--icon {
    @extend .tableCell-align-middle;
    padding-right: 10px;
  }
}
</style>
