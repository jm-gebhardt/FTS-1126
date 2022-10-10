<template>
  <ui-list
    :key="props.currentPage"
    v-model="selectedItemComputed"
    :type="2"
    single-selection
  >
    <template v-for="(item, index) in props.orders" :key="index">
      <ui-item>
        <template #before="{ iconClass }">
          <ui-icon :class="iconClass">
            {{ OrderStatusIcon(item.currentState) }}
          </ui-icon>
        </template>
        <ui-item-text-content>
          <ui-item-text1>{{ item.orderDetails?.orderId }}</ui-item-text1>
          <ui-item-text2>
            {{ new Date(item.orderDetails?.creationDate).toLocaleString() }}
          </ui-item-text2>
        </ui-item-text-content>
        <ui-item-last-content>
          {{ item.orderDetails.source }} -
          {{ item.orderDetails.destination }}
        </ui-item-last-content>
      </ui-item>
    </template>
    <ui-pagination
      v-model="currentPage"
      fullwidth
      show-total
      v-bind="$attrs"
    ></ui-pagination>
  </ui-list>
</template>

<script lang="ts">
export default {
  name: 'OrderList',
  inheritAttrs: false
}
</script>

<script lang="ts" setup>
import { IOrder } from '@/services/api/data-contracts'
import { UiPagination } from 'balm-ui'
import { computed } from 'vue'

interface Props {
  orders: IOrder[]
  selectedItem: number | null
  currentPage: number
}

let props = defineProps<Props>()

const emit = defineEmits(['update:selectedItemChanged', 'update:pageChanged'])

const selectedItemComputed = computed({
  get() {
    return props.selectedItem
  },

  set(value) {
    return emit('update:selectedItemChanged', value)
  }
})

const currentPage = computed({
  get() {
    return props.currentPage
  },

  set(value) {
    return emit('update:pageChanged', value)
  }
})

function OrderStatusIcon(status: string | null | undefined): string {
  switch (status) {
    case 'OrderReceived':
      return 'fiber_new'
    case 'OrderFailed':
      return 'clear'
    case 'AgvRequested':
      return 'hail'
    case 'TransportOrderSend':
      return 'agriculture'
    case 'TransportOrderStarted':
      return 'agriculture'
    case 'TransportOrderPicked':
      return 'agriculture'
    case 'Final':
      return 'done'
    default:
      return ''
  }
}
</script>
