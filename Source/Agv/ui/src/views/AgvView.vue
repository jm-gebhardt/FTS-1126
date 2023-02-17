<template>
  <div v-if="showNavBar" id="nav-main">
    <nav-bar></nav-bar>
  </div>
  <div class="content-main">
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
          @update:selected-list-item-changed="selectedItemChanged"
        ></agv-list>
      </ui-grid-cell>
      <ui-grid-cell class="agv-list-cell" columns="6">
        <agv-details
          v-if="selectedItemIdx != null && selectedCorrelationId != null"
          v-model="selectedAgv"
          :agv-data="selectedAgvData"
          :agv-state="selectedAgvState"
        ></agv-details>
      </ui-grid-cell>
    </ui-grid>
    <ui-spinner v-if="agvs.loading" active></ui-spinner>
    <ui-banner v-if="agvs.error"
      >Error loading data: {{ agvs.error }}</ui-banner
    >
  </div>
</template>

<script lang="ts" setup>
import NavBar from '@/components/NavBar.vue'
import AgvList from '@/components/AgvList.vue'
import AgvDetails from '@/components/AgvDetails.vue'

import { useAgvStore } from '@/services/AgvStore'
import { storeToRefs } from 'pinia'
import { onBeforeUnmount, onMounted, Ref, ref } from 'vue'
import { AgvHub } from '@/services/AgvHub'
import { IAgv, IAgvData, IAgvState } from '@/services/api/data-contracts'
import { useRoute, useRouter } from 'vue-router'
import { UiSwitch, UiGrid, UiGridCell, UiSpinner, UiBanner } from 'balm-ui'

interface Props {
  showNavBar: boolean | undefined
}

defineProps<Props>()

const agvHub = new AgvHub(onAgvDiscovered, onAgvStateChanged)

const { agvs, agvData, agvStates } = storeToRefs(useAgvStore())
const { fetchAgvs, fetchState, updateAgvState, fetchData, fetchMaps } =
  useAgvStore()

const route = useRoute()
const router = useRouter()

let selectedItemIdx: Ref<number | null> = ref(null)
let selectedCorrelationId: Ref<string | null> = ref(null)

let selectedAgv: Ref<IAgv | null> = ref(null)
let selectedAgvData: Ref<IAgvData | null | undefined> = ref(null)
let selectedAgvState: Ref<IAgvState | null | undefined> = ref(null)

/**
 * Selected item changed
 * Update selected item and changes route
 * @param value
 */
async function selectedItemChanged(index: number | null): Promise<void> {
  let query: { [key: string]: string | number | null } = {}

  selectedItemIdx.value = index
  let agv: IAgv | null = null

  if (index !== null) {
    agv = Array.from(agvs.value.data.values())[index]
  }

  const lastSelectedCorrelationId = selectedCorrelationId.value

  if (agv) {
    selectedCorrelationId.value = agv.correlationId
    selectedAgv.value = agv

    selectedAgvData.value = agvData.value.data.get(agv.correlationId)
    selectedAgvState.value = agvStates.value.data.get(agv.correlationId)
  } else {
    selectedCorrelationId.value = null
    selectedAgv.value = null
    selectedAgvData.value = null
    selectedAgvState.value = null
  }

  if (selectedCorrelationId.value != null) {
    query['correlationId'] = selectedCorrelationId.value
  }

  if (selectedCorrelationId.value != lastSelectedCorrelationId) {
    pushCurrentRoute()
  }

  if (selectedCorrelationId.value != null) {
    await fetchData(selectedCorrelationId.value).then(() => {
      if (selectedCorrelationId.value != null) {
        selectedAgvData.value = agvData.value.data.get(
          selectedCorrelationId.value
        )
      }
    })

    await fetchState(selectedCorrelationId.value).then(() => {
      if (selectedCorrelationId.value != null) {
        selectedAgvState.value = agvStates.value.data.get(
          selectedCorrelationId.value
        )
      }
    })
  }
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
async function onAgvStateChanged(agvState: IAgvState): Promise<void> {
  await updateAgvState(agvState)

  // Refresh selected AGV status if the status belongs to the selected AGV
  if (selectedCorrelationId.value === agvState.correlationId) {
    selectedAgvState.value = agvState
  }
}

function fetch(): void {
  fetchAgvs().then(() => {
    if (selectedCorrelationId.value != null) {
      const index = Array.from(agvs.value.data.keys()).indexOf(
        selectedCorrelationId.value
      )
      if (index !== -1) {
        selectedItemChanged(index)
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

function pushCurrentRoute(): void {
  let query: { [key: string]: string | number | null } = {}

  if (selectedCorrelationId.value != null) {
    query['correlationId'] = selectedCorrelationId.value
  }

  if (router && route.name) {
    router.push({ name: route.name, query: query })
  }
}

onBeforeUnmount(async () => {
  agvHub.disconnect()
})

onMounted(() => {
  if (route?.query?.correlationId != undefined) {
    selectedCorrelationId.value = <string>route?.query?.correlationId
    pushCurrentRoute()
  }

  fetch()
  agvHub.connect()
})
</script>

<style lang="scss">
.buttons {
  text-align: right;
}

.agv-list-cell {
  width: 400px;
}
</style>
