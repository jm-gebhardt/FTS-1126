<template>
  <h1>Agvs</h1>
  <div class="buttons">
    <button @click="fetch()">Reload</button>
    Auto-refresh
    <ui-switch
      :model-value="agvHub.isConnected()"
      @selected="() => toggleAutoRefresh()"
      >Auto-Refresh</ui-switch
    >
  </div>
  <agv-list
    :agvs="Array.from(agvs.data.values())"
    :model-value="agvs.currentPage"
    :total="agvs.totalCount"
    :page-size="pageSizes"
    @update:page-size="pageSizeChanged"
    @update:model-value="pageChanged"
  ></agv-list>

  <ui-spinner v-if="agvs.loading" active></ui-spinner>
  <ui-banner v-if="agvs.error">Error loading data: {{ agvs.error }}</ui-banner>
</template>
<script lang="ts" setup>
import AgvList from '@/components/AgvList.vue'

import { useAgvStore } from '@/services/AgvStore'
import { storeToRefs } from 'pinia'
import { onBeforeUnmount } from 'vue'
import { AgvHub } from '@/services/AgvHub'

const agvHub = new AgvHub()

const { agvs } = storeToRefs(useAgvStore())
const { fetchAgvs } = useAgvStore()

const pageSizes = [10, 25, 100]

let requestPageNumber = 1
let requestPageSize = pageSizes[0]

function fetch(): void {
  fetchAgvs(requestPageNumber, requestPageSize)
}

interface IPageSizeChangedEvent {
  page: number
  pageSize: number
}

function pageSizeChanged(event: IPageSizeChangedEvent): void {
  requestPageNumber = event.page
  requestPageSize = event.pageSize
  fetch()
}

function pageChanged(pageNumber: number): void {
  requestPageNumber = pageNumber
  fetch()
}

function toggleAutoRefresh(): void {
  if (agvHub.isConnected()) {
    agvHub.disconnect()
  } else {
    agvHub.connect()
  }
}

onBeforeUnmount(async () => {
  agvHub.disconnect()
})

fetch()
</script>

<style lang="scss">
.buttons {
  text-align: right;
}
</style>
