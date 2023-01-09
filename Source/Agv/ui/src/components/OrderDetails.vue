<template>
  <details-list v-model="listItems"></details-list>
  <ui-list-divider></ui-list-divider>
  <ui-list-group-subheader>Actions</ui-list-group-subheader>
  <actions-list v-model="actionItems"></actions-list>
</template>

<script lang="ts">
export default {
  name: 'OrderDetails'
}
</script>

<script lang="ts" setup>
import { computed, ComputedRef, onMounted } from 'vue'
import {
  UiListDivider,
  UiListGroupSubheader,
  useAlert,
  useConfirm
} from 'balm-ui'
import { storeToRefs } from 'pinia'
import { HttpResponse } from '@microsoft/signalr'
import ActionsList from '@/components/ActionsList.vue'
import DetailsList from '@/components/DetailsList.vue'
import { ActionItem } from '@/components/ActionItem'
import { ListItem } from '@/components/ListItem'
import { useAgvStore } from '@/services/AgvStore'
import { Orders as OrderApi } from '@/services/api/Orders'
import { IOrder } from '@/services/api/data-contracts'

const { agvs } = storeToRefs(useAgvStore())
const { fetchAgvs } = useAgvStore()

const orderApi = new OrderApi({
  baseUrl: import.meta.env.VITE_API_URL
})

interface Props {
  modelValue: IOrder | null
  showNavBar: boolean | undefined
}

const props = defineProps<Props>()

let listItems: ComputedRef<ListItem[]> = computed(() => {
  if (props.modelValue == null) {
    return [] as ListItem[]
  }
  return [
    { label: 'Order Id', value: props.modelValue.orderDetails.orderId },
    { label: 'ID', value: props.modelValue.correlationId },
    { label: 'State', value: props.modelValue.currentState },
    {
      label: 'Assigned AGV',
      value:
        props.modelValue.assignedAgv != null
          ? agvs.value.data.get(props.modelValue.assignedAgv)?.agvId
          : '-',
      route: {
        name: props.showNavBar ? 'AGVs' : 'SingleAgvPage',
        query: { correlationId: props.modelValue.assignedAgv }
      }
    },
    {
      label: 'Source / Destination',
      value:
        props.modelValue.orderDetails.source +
        ' / ' +
        props.modelValue.orderDetails.destination
    },
    { label: 'LeNo', value: props.modelValue.orderDetails.leNo },
    {
      label: 'Creation Date',
      value: new Date(
        props.modelValue.orderDetails.creationDate
      ).toLocaleString()
    },
    { label: 'Failed Reason', value: props.modelValue.failedReason }
  ] as ListItem[]
})

let actionItems: ComputedRef<ActionItem<IOrder>[]> = computed(() => {
  if (props.modelValue == null) {
    return [] as ActionItem<IOrder>[]
  }
  return [
    // Actions
    {
      label: 'Reset Order',
      data: props.modelValue,
      action: resetOrder
    },
    {
      label: 'Complete Order',
      data: props.modelValue,
      action: completeOrder
    }
  ] as ActionItem<IOrder>[]
})

function resetOrder(order: IOrder | null): void {
  if (order === null) return

  const $confirm = useConfirm()

  $confirm(`Reset order '${order.orderDetails.orderId}'?`)
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    .then((result: any) => {
      // According to the balm-ui documentation 'result' should be a boolean but in the node module code it's declared as a void
      // During runtime it actually is a boolean, so we need to cast it
      if (result as boolean) {
        orderApi
          .resetOrderPartialUpdate(order.correlationId)
          .catch((reason: HttpResponse) => {
            console.error('Reset order: ' + reason.statusText)
            const $alert = useAlert()
            $alert(`Order '${order.orderDetails.orderId}' could not be resend`)
          })
      }
    })
}

function completeOrder(order: IOrder | null): void {
  if (order === null) return

  const $confirm = useConfirm()

  $confirm(`Complete order '${order.orderDetails.orderId}'?`)
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    .then((result: any) => {
      // According to the balm-ui documentation 'result' should be a boolean but in the node module code it's declared as a void
      // During runtime it actually is a boolean, so we need to cast it
      if (result as boolean) {
        orderApi
          .completeOrderPartialUpdate(order.correlationId)
          .catch((reason: HttpResponse) => {
            console.error('Complete order: ' + reason.statusText)
            const $alert = useAlert()
            $alert(
              `Order '${order.orderDetails.orderId}' could not be completed`
            )
          })
      }
    })
}

onMounted(() => {
  fetchAgvs()
})
</script>
