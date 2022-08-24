<template>
  <ui-table :data="props.agvs" :thead="thead" :tbody="tbody">
    <template #CurrentState="{ data }">
      <div class="tableCell-align-middle--icon">
        <ui-icon>{{ AgvStatusIcon(data.currentState) }}</ui-icon>
      </div>
      <div class="tableCell-align-middle">{{ data.currentState }}</div>
    </template>

    <ui-pagination fullwidth show-total v-bind="$attrs"></ui-pagination>
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
import { UiPagination } from 'balm-ui'

interface Props {
  agvs: IAgv[]
}

const props = defineProps<Props>()

const thead = ['ID', 'Agv ID', 'Current State']
const tbody = [
  'correlationId',
  {
    field: 'agvId',
    fn: (agv: IAgv): string | null | undefined => agv.agvId
  },
  {
    field: 'currentState',
    slot: 'CurrentState'
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
