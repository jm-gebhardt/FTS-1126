<template>
  <h1>Orders</h1>
  <div class="buttons">
    <button @click="fetch()">Reload</button>
    Auto-refresh
    <ui-switch
      :model-value="orderHub.isConnected()"
      @selected="() => toggleAutoRefresh()"
      >Auto-Refresh</ui-switch
    >
  </div>
  <order-list
    :orders="data"
    :model-value="currentPage"
    :total="totalCount"
    :page-size="pageSizes"
    @update:page-size="pageSizeChanged"
    @update:model-value="pageChanged"
  ></order-list>

  <ui-spinner v-if="loading" active></ui-spinner>
  <ui-banner v-if="error">Error loading data: {{ error }}</ui-banner>
</template>

<script lang="ts">
import { defineComponent } from 'vue'

export default defineComponent({
  name: 'OrdersView'
})
</script>

<script lang="ts" setup>
import OrderList from '@/components/OrderList.vue'

import { useOrderStore } from '@/services/OrderStore'
import { storeToRefs } from 'pinia'
import { onBeforeUnmount } from 'vue'
import { OrderHub } from '@/services/OrderHub'

const orderHub = new OrderHub()
const orderStore = useOrderStore()

const { data, currentPage, totalCount, loading, error } =
  storeToRefs(orderStore)

const pageSizes = [10, 25, 100]

let requestPageNumber = 1
let requestPageSize = pageSizes[0]

function fetch(): void {
  orderStore.fetchOrders(requestPageNumber, requestPageSize)
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
  if (orderHub.isConnected()) {
    orderHub.disconnect()
  } else {
    orderHub.connect()
  }
}

onBeforeUnmount(async () => {
  orderHub.disconnect()
})

fetch()
</script>

<style lang="scss">
.buttons {
  text-align: right;
}
</style>
