<template>
  <details-list v-model="items"></details-list>
</template>

<script lang="ts">
export default {
  name: 'OrderDetails'
}
</script>

<script lang="ts" setup>
import { IOrder } from '@/services/api/data-contracts'
import { computed, ComputedRef, onMounted } from 'vue'
import DetailsList from '@/components/DetailsList.vue'
import { ListItem } from '@/components/ListItem'
import { storeToRefs } from 'pinia'
import { useAgvStore } from '@/services/AgvStore'

const { agvs } = storeToRefs(useAgvStore())
const { fetchAgvs } = useAgvStore()

interface Props {
  modelValue: IOrder | null
}

const props = defineProps<Props>()

let items: ComputedRef<ListItem[]> = computed(() => {
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
        name: 'AGVs',
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

onMounted(() => {
  fetchAgvs()
})
</script>
