<template>
  <div v-if="showNavBar" id="nav-main">
    <nav-bar></nav-bar>
  </div>
  <div class="content-main">
    <h1>Orders</h1>
    <ui-grid>
      <ui-grid-cell class="order-menu-cell" columns="6">
        <ui-datepicker
          :model-value="dateRange"
          :config="{
            enableTime: true,
            time_24hr: true,
            mode: 'range',
            altInput: true,
            altFormat: 'Y-m-d'
          }"
          @update:model-value="updateDateRange"
        ></ui-datepicker>
        <button @click="() => updateDateRange(getToday())">Today</button>
      </ui-grid-cell>
      <ui-grid-cell class="order-menu-cell" columns="6">
        <div class="buttons">
          <button @click="fetch()">Reload</button>
          Auto-refresh
          <ui-switch
            :model-value="orderHub.isConnected()"
            @selected="() => toggleAutoRefresh()"
            >Auto-Refresh</ui-switch
          >
        </div>
      </ui-grid-cell>
    </ui-grid>
    <ui-grid class="demo-grid">
      <ui-grid-cell class="order-list-cell" columns="6">
        <order-list
          :orders="data"
          :current-page="currentPage"
          :total="totalCount"
          :page-size="pageSizes"
          :selected-item="selectedItemIdx"
          @update:page-size="pageSizeChanged"
          @update:page-changed="pageChanged"
          @update:selected-item-changed="selectedItemChanged"
        ></order-list>
      </ui-grid-cell>
      <ui-grid-cell columns="6">
        <order-details
          v-if="selectedItemIdx != null"
          v-model="data[selectedItemIdx]"
          class="order-details-cell"
          :show-nav-bar="showNavBar"
        >
        </order-details>
      </ui-grid-cell>
    </ui-grid>

    <ui-spinner v-if="loading" active></ui-spinner>
    <ui-alert v-if="error" state="error">
      Error loading data: {{ error }}
    </ui-alert>
  </div>
</template>

<script lang="ts" setup>
import NavBar from '@/components/NavBar.vue'
import OrderList from '@/components/OrderList.vue'
import OrderDetails from '@/components/OrderDetails.vue'
import { IOrder } from '@/services/api/data-contracts'
import { OrderHub } from '@/services/OrderHub'
import { useOrderStore } from '@/services/OrderStore'

import { storeToRefs } from 'pinia'
import { computed, onBeforeUnmount, onMounted, Ref, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

interface Props {
  showNavBar: boolean | undefined
}

defineProps<Props>()

const orderHub = new OrderHub(onOrderStateChanged)
let selectedItemIdx: Ref<number | null> = ref(null)
let selectedItem: Ref<IOrder | null> = ref(null)
let selectedCorrelationId: string | null

const dateRange = computed(() => {
  if (startDate.value != null && endDate.value != null) {
    return [startDate.value, endDate.value]
  } else {
    // Default date range
    return getToday()
  }
})

const { fetchOrders, updateOrderList } = useOrderStore()
const { data, currentPage, totalCount, loading, error, startDate, endDate } =
  storeToRefs(useOrderStore())
const route = useRoute()
const router = useRouter()

const pageSizes = [10, 25, 100]

let requestPageNumber = 1
let requestPageSize = pageSizes[0]

/**
 * Updates order in order list view
 */
async function onOrderStateChanged(order: IOrder): Promise<void> {
  await updateOrderList(order)
}

/**
 * Fetch Orders and update route
 */
async function fetch(): Promise<void> {
  await fetchOrders(
    requestPageNumber,
    requestPageSize,
    startDate.value == null ? undefined : startDate.value,
    endDate.value == null ? undefined : endDate.value
  )
  selectedItem.value = data.value.filter(
    (item) => item.correlationId == selectedCorrelationId
  )[0]
  selectedItemIdx.value = data.value.indexOf(selectedItem.value)
  pushCurrentRoute()
}

/**
 * Get todays date range
 */
function getToday(): Date[] {
  let start = new Date()
  start.setHours(0, 0, 0, 0)
  let end = new Date()
  end.setHours(23, 59, 59, 999)
  return [start, end]
}

interface IPageSizeChangedEvent {
  page: number
  pageSize: number
}

/**
 * Page Size Changed
 * @param event
 */
function pageSizeChanged(event: IPageSizeChangedEvent): void {
  requestPageNumber = event.page
  requestPageSize = event.pageSize
  fetch()
}

/**
 * Page changed
 * @param pageNumber
 */
function pageChanged(pageNumber: number): void {
  selectedItemIdx.value = null
  selectedItem.value = null
  requestPageNumber = pageNumber
  fetch()
}

/**
 * Selected item changed
 * Update selected item and changes route
 * @param value
 */
function selectedItemChanged(value: number | null): void {
  selectedItemIdx.value = value
  selectedCorrelationId = value != null ? data.value[value].correlationId : null
  pushCurrentRoute()
}

/**
 * Update date range fetch new orders
 * @param value
 */
function updateDateRange(value: Date[]): void {
  startDate.value = value[0]
  endDate.value = value[1]
  requestPageNumber = 1
  selectedItem.value = null
  selectedCorrelationId = null
  fetch()
}

/**
 * Toggle auto refresh
 */
function toggleAutoRefresh(): void {
  if (orderHub.isConnected()) {
    orderHub.disconnect()
  } else {
    orderHub.connect()
  }
}

/**
 * Push current route to vue router to display
 */
function pushCurrentRoute(): void {
  let query: { [key: string]: string | number | null } = {}

  if (selectedCorrelationId != null) {
    query['correlationId'] = selectedCorrelationId
  }

  if (requestPageNumber != null) {
    query['page'] = requestPageNumber
  }
  if (requestPageSize != null) {
    query['pageSize'] = requestPageSize
  }
  if (startDate.value != null) {
    query['startDate'] = new Date(startDate.value).toISOString()
  }
  if (endDate.value != null) {
    query['endDate'] = new Date(endDate.value).toISOString()
  }

  if (router && route.name) {
    router.push({ name: route.name, query: query })
  }
}

onMounted(() => {
  if (
    route?.query?.startDate != undefined &&
    route?.query?.endDate != undefined
  ) {
    startDate.value = new Date(<string>route.query.startDate)
    endDate.value = new Date(<string>route?.query.endDate)
  } else {
    let today = getToday()
    startDate.value = today[0]
    endDate.value = today[1]
  }

  if (route?.query?.page != undefined) {
    requestPageNumber = parseInt(<string>route?.query?.page)
  } else {
    requestPageNumber = 1
  }

  if (route?.query?.pageSize != undefined) {
    requestPageSize = parseInt(<string>route?.query?.pageSize)
  } else {
    requestPageSize = pageSizes[0]
  }

  if (route?.query?.correlationId != undefined) {
    selectedCorrelationId = <string>route?.query?.correlationId
  }

  pushCurrentRoute()
  fetch()
})

onBeforeUnmount(async () => {
  orderHub.disconnect()
})
</script>

<style lang="scss">
@use '@material/layout-grid/variables';
@use 'sass:map';

.buttons {
  text-align: right;
}

.order-details-cell {
  border: 1px solid rgba(0, 0, 0, 0.12);
  border-radius: var(--mdc-shape-medium, 4px);
  /* Only fixed position in desktop mode */
  @media (min-width: map.get(variables.$breakpoints, 'desktop')) {
    position: fixed;
  }
}

.order-menu-cell > * {
  margin-right: 10px;
}
</style>
