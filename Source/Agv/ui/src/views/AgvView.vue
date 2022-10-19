<template>
  <h1>Agvs</h1>
  <div class="buttons">
    <button @click="fetch()">Reload</button>
    Auto-refresh
    <ui-switch
      :model-value="agvHub.isConnected()"
      @selected="() => toggleAutoRefresh()"
      >Auto-Refresh
    </ui-switch>
  </div>
  <ui-grid>
    <ui-grid-cell class="agv-list-cell" columns="6">
      <agv-list
        :agvs="Array.from(agvs.data.values())"
        :selected-item="selectedItemIdx"
        :click-set-idle="(agvs) => clickSetIdle(agvs)"
        @update:selected-list-item-changed="selectedItemChanged"
      ></agv-list>
    </ui-grid-cell>
    <ui-grid-cell columns="6">
      <agv-details
        v-if="selectedItemIdx != null && selectedCorrelationId != null"
        v-model="Array.from(agvs.data.values())[selectedItemIdx]"
        :agv-data="agvData.data.get(selectedCorrelationId)"
        :agv-status="agvStatus.data.get(selectedCorrelationId)"
      ></agv-details>
    </ui-grid-cell>
  </ui-grid>
  <ui-spinner v-if="agvs.loading" active></ui-spinner>
  <ui-banner v-if="agvs.error">Error loading data: {{ agvs.error }}</ui-banner>
</template>
<script lang="ts" setup>
import AgvList from '@/components/AgvList.vue'
import AgvDetails from '@/components/AgvDetails.vue'

import { useAgvStore } from '@/services/AgvStore'
import { storeToRefs } from 'pinia'
import { onBeforeUnmount, onMounted, Ref, ref } from 'vue'
import { AgvHub } from '@/services/AgvHub'
import { IAgv } from '@/services/api/data-contracts'
import { useConfirm, useAlert } from 'balm-ui'
import { HttpResponse } from '@microsoft/signalr'
import { Agvs as AgvApi } from '@/services/api/Agvs'
import { useRoute, useRouter } from 'vue-router'

const agvHub = new AgvHub(onAgvDiscovered, onAgvStateChanged)

const { agvs, agvData, agvStatus } = storeToRefs(useAgvStore())
const { fetchAgvs, updateAgv, fetchData, fetchMaps, fetchStatus } =
  useAgvStore()

const agvApi = new AgvApi({
  baseUrl: import.meta.env.VITE_API_URL
})

const route = useRoute()
const router = useRouter()

let selectedItemIdx: Ref<number | null> = ref(null)
let selectedCorrelationId: Ref<string | null> = ref(null)

/**
 * Selected item changed
 * Update selected item and changes route
 * @param value
 */
async function selectedItemChanged(value: number | null): Promise<void> {
  let query: { [key: string]: string | number | null } = {}

  selectedItemIdx.value = value
  selectedCorrelationId.value =
    value != null
      ? Array.from(agvs.value.data.values())[value].correlationId
      : null
  if (selectedCorrelationId.value != undefined) {
    await fetchData(selectedCorrelationId.value)
    await fetchStatus(selectedCorrelationId.value)
  }

  if (selectedCorrelationId.value != null) {
    query['correlationId'] = selectedCorrelationId.value
  }

  pushCurrentRoute()
}

/**
 * Handler for hub event 'onAgvDiscovered'
 */
async function onAgvDiscovered(): Promise<void> {
  // When a new agv has been added, we fetch the full list as we do not know an what page it is
  await fetchAgvs()
}

/**
 * Handler for hub event 'onAgvStateChanged'
 */
async function onAgvStateChanged(agv: IAgv): Promise<void> {
  await updateAgv(agv)
}

function fetch(): void {
  fetchAgvs().then(() => {
    if (selectedCorrelationId.value != null) {
      let selectedAgv = agvs.value.data.get(selectedCorrelationId.value)
      if (selectedAgv != undefined) {
        selectedItemIdx.value = Array.from(agvs.value.data.values()).indexOf(
          selectedAgv
        )
      }
    }
  })
  fetchMaps()
}

function toggleAutoRefresh(): void {
  if (agvHub.isConnected()) {
    agvHub.disconnect()
  } else {
    agvHub.connect()
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
        .catch((reason: HttpResponse) => {
          console.log('setIdle: ' + reason.statusText)
          const $alert = useAlert()
          $alert(`AGV '${agv.agvId}' could not be set to Idle`)
        })
    }
  })
}

function pushCurrentRoute(): void {
  let query: { [key: string]: string | number | null } = {}

  if (selectedCorrelationId.value != null) {
    query['correlationId'] = selectedCorrelationId.value
  }

  router?.push({ name: 'AGVs', query: query })
}

onBeforeUnmount(async () => {
  agvHub.disconnect()
})

onMounted(() => {
  if (route?.query?.correlationId != undefined) {
    selectedCorrelationId.value = <string>route?.query?.correlationId
  }

  pushCurrentRoute()
  fetch()
})
</script>

<style lang="scss">
.buttons {
  text-align: right;
}
</style>
