<template>
  <h1>Orders</h1>
  <div class="toggle-switch">
    Auto reload
    <ui-switch
      :model-value="orderPolling.pollingEnabled()"
      @selected="() => orderPolling.toggle()"
      >Polling</ui-switch
    >
  </div>
  <order-list
    :orders="orders"
    :model-value="currentPage"
    :total="totalCount"
    :page-size="pageSizes"
    @update:page-size="pageSizeChanged"
    @update:model-value="pageChanged"
  ></order-list>

  <ui-spinner v-if="loading" active></ui-spinner>
  <ui-banner v-if="error">Error loading data: {{ error }}</ui-banner>
</template>
<script lang="ts" setup>
import OrderList from '@/components/OrderList.vue'

import { useOrderStore } from '@/services/OrderStore'
import { storeToRefs } from 'pinia'
import { onBeforeUnmount } from 'vue'
import { Polling } from '@/utils/Polling'

const { orders, currentPage, totalCount, loading, error } = storeToRefs(
  useOrderStore()
)
const { fetchOrders } = useOrderStore()

const pageSizes = [10, 25, 100]

let requestPageNumber = 1
let requestPageSize = pageSizes[0]
const orderPolling = new Polling(fetch, 3000)

function fetch(): void {
  fetchOrders(requestPageNumber, requestPageSize)
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

onBeforeUnmount(async () => {
  orderPolling.stop()
})

fetch()
</script>

<style lang="scss">
.toggle-switch {
  text-align: right;
}
</style>
