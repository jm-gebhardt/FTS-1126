<template>
  <ui-list
    v-model="selectedItemComputed"
    :data="props.agvs"
    :type="2"
    single-selection
  >
    <template v-for="(item, index) in props.agvs" :key="index">
      <ui-item>
        <template #before="{ iconClass }">
          <ui-icon :class="iconClass">
            {{ AgvStateIcon(item.currentState) }}
          </ui-icon>
        </template>
        <ui-item-text-content>
          <ui-item-text1>{{ item.agvId }}</ui-item-text1>
          <ui-item-text2>{{ item.currentState }}</ui-item-text2>
        </ui-item-text-content>
        <template #after="{ iconClass }">
          <ui-icon :class="iconClass">
            {{ AgvAliveIcon(item.alive) }}
          </ui-icon>
        </template>
      </ui-item>
    </template>
  </ui-list>
</template>

<script lang="ts">
export default {
  name: 'AgvList',
  inheritAttrs: false
}
</script>

<script lang="ts" setup>
import { IAgv } from '@/services/api/data-contracts'
import { computed } from 'vue'

interface Props {
  agvs: IAgv[]
  selectedItem: number | null
}

let props = defineProps<Props>()

const emit = defineEmits(['update:selectedListItemChanged'])

const selectedItemComputed = computed({
  get() {
    return props.selectedItem
  },

  set(value) {
    return emit('update:selectedListItemChanged', value)
  }
})

function AgvStateIcon(status: string | null | undefined): string {
  switch (status) {
    case 'Discovered':
      return 'fiber_new'
    case 'Idle':
      return 'pending'
    case 'Reserved':
      return 'save_alt'
    case 'Busy':
      return 'drive_eta'
    case 'Locked':
      return 'lock'
    case 'PendingLock':
      return 'hourglass_bottom'
    case 'Ready':
      return 'unfold_less'
    default:
      return 'help'
  }
}

function AgvAliveIcon(alive: boolean | null | undefined): string {
  if (alive == true) {
    return ''
  } else {
    return 'signal_wifi_off'
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
