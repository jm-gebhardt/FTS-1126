<template>
  <ui-list-group-subheader>General information</ui-list-group-subheader>
  <details-list v-model="generalItems"></details-list>
  <ui-list-divider></ui-list-divider>
  <ui-list-group-subheader>Status information</ui-list-group-subheader>
  <details-list v-model="statusItems"></details-list>
  <ui-list-divider></ui-list-divider>
  <ui-list-group-subheader>Data information</ui-list-group-subheader>
  <details-list v-model="dataItems"></details-list>
</template>

<script lang="ts">
export default {
  name: 'AgvDetails'
}
</script>

<script lang="ts" setup>
import { IAgv, IAgvData, IAgvStatus } from '@/services/api/data-contracts'
import { computed, ComputedRef, onMounted } from 'vue'
import DetailsList from '@/components/DetailsList.vue'
import { ListItem } from '@/components/ListItem'
import { useAgvStore } from '@/services/AgvStore'

const { fetchAgvs } = useAgvStore()

interface Props {
  modelValue: IAgv | null
  agvData: IAgvData | null | undefined
  agvStatus: IAgvStatus | null | undefined
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
    { label: 'OrderID', value: props.agvStatus?.orderId },
    { label: 'Busy', value: props.agvStatus?.busy },
    { label: 'Automatic', value: props.agvStatus?.automatic },
    { label: 'Disturbed', value: props.agvStatus?.disturbed },
    { label: 'LastNodeID', value: props.agvStatus?.lastNodeId },
    { label: 'BatteryCharge', value: props.agvStatus?.batteryCharge },
    { label: 'Charging', value: props.agvStatus?.charging }
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

onMounted(() => {
  fetchAgvs()
})
</script>
