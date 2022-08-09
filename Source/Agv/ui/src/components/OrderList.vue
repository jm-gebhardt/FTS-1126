<template>
  <ui-table :data="props.orders" :thead="thead" :tbody="tbody">
    <template #CurrentState="{ data }">
      <div class="tableCell-align-middle--icon">
        <ui-icon>{{ OrderStatusIcon(data.currentState) }}</ui-icon>
      </div>
      <div class="tableCell-align-middle">{{ data.currentState }}</div>
    </template>

    <ui-pagination fullwidth show-total v-bind="$attrs"></ui-pagination>
  </ui-table>
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

interface Props {
  orders: IOrder[]
}

const props = defineProps<Props>()

const thead = [
  'ID',
  'Order ID',
  'Current State',
  'Source',
  'Destination',
  'creationDate',
  'Assigned Agv'
]
const tbody = [
  'correlationId',
  {
    field: 'orderId',
    fn: (order: IOrder): string | null | undefined =>
      order.orderDetails?.orderId
  },
  {
    field: 'currentState',
    slot: 'CurrentState'
  },
  {
    field: 'source',
    fn: (order: IOrder): string | null | undefined => order.orderDetails?.source
  },
  {
    field: 'destination',
    fn: (order: IOrder): string | null | undefined =>
      order.orderDetails?.destination
  },
  {
    field: 'destination',
    fn: (order: IOrder): string | null | undefined =>
      order.orderDetails?.creationDate
  },
  'assignedAgv'
]

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
