<template>
  <div v-if="showNavBar" id="nav-main">
    <nav-bar></nav-bar>
  </div>
  <div class="content-main">
    <h1>Maps</h1>
    <div>
      <div id="map-menu">
        <ui-select
          id="map-menu-maps"
          v-model="selectedMap"
          :options="mapOptions"
          outlined
          @update:model-value="selectedMapChanged"
        >
          Maps
        </ui-select>
        <ui-list>
          <template v-for="item in agvOptions" :key="item.value">
            <ui-item>
              <ui-item-first-content>
                <ui-checkbox
                  v-model="selectedAgvs"
                  :value="item.value"
                  :input-id="item.value"
                  @click.stop
                  @update:model-value="selectedAgvsChanged"
                >
                </ui-checkbox>
              </ui-item-first-content>
              <ui-item-text-content>{{ item.label }}</ui-item-text-content>
            </ui-item>
          </template>
        </ui-list>
      </div>
      <div id="map-container">
        <konva-map ref="mapComponent"></konva-map>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue'

export default defineComponent({
  name: 'MapView'
})
</script>

<script lang="ts" setup>
import NavBar from '@/components/NavBar.vue'
import KonvaMap from '@/components/KonvaMap/KonvaMap.vue'
import { storeToRefs } from 'pinia'
import { useAgvStore } from '@/services/AgvStore'
import { onBeforeUnmount, onMounted, ref } from 'vue'
import {
  IAgv,
  IAgvState,
  IGraph,
  IMap,
  IAgvPositionStatus
} from '@/services/api/data-contracts'
import { AgvsPositions as AgvPosApi } from '@/services/api/AgvsPositions'
import { AgvHub } from '@/services/AgvHub'
import {
  BalmUiOption,
  createOptionFromMap,
  createOptionsFromList
} from '@/utils/BalmUiOptions'
import {
  UiSelect,
  UiList,
  UiItem,
  UiItemFirstContent,
  UiCheckbox,
  UiItemTextContent
} from 'balm-ui'

interface Props {
  showNavBar: boolean | undefined
}

defineProps<Props>()

const agvPosApi = new AgvPosApi({
  baseUrl: import.meta.env.VITE_API_URL
})

const agvHub = new AgvHub(undefined, onAgvStateChanged, onAgvPositionChanged)

const { agvs, agvStates, agvGraph, maps } = storeToRefs(useAgvStore())
const { fetchAgvs, fetchState, updateAgvState, fetchGraph, fetchMaps, MapUrl } =
  useAgvStore()

/*-------------------------------------*/

const selectedAgvs = ref([] as string[])
const selectedMap = ref(null)
const selectedMapRelatedAgvs = ref([] as string[])
const mapComponent = ref()

const agvOptions = ref([] as BalmUiOption[])
const mapOptions = ref([] as BalmUiOption[])

onMounted(() => {
  fetchMaps().then(() => {
    mapOptions.value = createOptionFromMap<IMap>(
      maps.value.data,
      (map) => map.mapId
    )
  })

  updateAgvs()

  agvHub.connect()
})

onBeforeUnmount(async () => {
  agvHub.disconnect()
})

/**
 * Handler for hub event 'onAgvStateChanged'
 */
async function onAgvStateChanged(agvState: IAgvState): Promise<void> {
  await updateAgvState(agvState) // Update store
  mapComponent.value.updateAgvState(agvState)
}

/**
 * Handler for hub event 'onAgvPositionChanged'
 */
async function onAgvPositionChanged(
  agvPositionStatus: IAgvPositionStatus
): Promise<void> {
  mapComponent.value.updateAgvPosition(agvPositionStatus)
}

/**
 * Update the listed agvs
 */
function updateAgvs(): void {
  fetchAgvs().then(() => {
    // Get status for AGVs
    agvs.value.data.forEach((agv) => {
      fetchState(agv.correlationId).then(() => {
        const agvState = agvStates.value.data.get(agv.correlationId)
        if (agvState) {
          mapComponent.value.updateAgvState(agvState)
        }
      })
    })

    // Only show selected AGVs
    const relevantAgvs: IAgv[] = []
    for (const agvCorrelationId of selectedMapRelatedAgvs.value) {
      const agv: IAgv | undefined = agvs.value.data.get(agvCorrelationId)
      if (agv != undefined) {
        relevantAgvs.push(agv)
      }
    }

    agvOptions.value = createOptionsFromList<IAgv>(
      relevantAgvs,
      (agv) => agv.agvId,
      (agv) => agv.correlationId
    )

    selectedAgvs.value = selectedMapRelatedAgvs.value
    mapComponent.value.setVisibleAgvs(selectedAgvs.value)
    fetchSelectedAgvGraphs().then(() => {
      updateGraphElements()
    })
  })
}

function updateAgvPositions(): void {
  agvPosApi
    .agvsPositionsList()
    .then((response) => {
      response.data.forEach((agvPositionStatus) => {
        onAgvPositionChanged(agvPositionStatus)

        fetchState(agvPositionStatus.correlationId).then(() => {
          const agvState = agvStates.value.data.get(
            agvPositionStatus.correlationId
          )
          if (agvState) {
            mapComponent.value.updateAgvState(agvState)
          }
        })
      })
    })
    .catch((response) => {
      console.error(response.error)
    })
}
/**
 * Callback for AGV selection has changed
 */
function selectedAgvsChanged(): void {
  mapComponent.value.setVisibleAgvs(selectedAgvs.value)
  fetchSelectedAgvGraphs().then(() => {
    updateGraphElements()
  })
}

/**
 * Callback when selected map has changed
 * @returns: Promise
 */
async function selectedMapChanged(): Promise<void> {
  if (selectedMap.value == null) {
    return
  }

  const mapObject: IMap | undefined = maps.value.data.get(selectedMap.value)
  if (mapObject == undefined) {
    return
  }

  await setMap(selectedMap.value, mapObject)

  updateAgvs()
}

async function setMap(mapUrl: string, mapObject: IMap): Promise<void> {
  const image = new Image()
  image.src = MapUrl(mapUrl)
  image.onload = (): void => {
    // set background when image is loaded
    mapComponent.value.setBackground(
      image,
      mapObject.resolution,
      mapObject.xOffset,
      mapObject.yOffset
    )
  }
  selectedMapRelatedAgvs.value = mapObject.relatedAgvs

  updateAgvPositions()
}

/**
 * Fetch Graphs of all selected AGVs
 * @returns: Promise
 */
async function fetchSelectedAgvGraphs(): Promise<void> {
  for (const agv of selectedAgvs.value) {
    if (!agvGraph.value.data.has(agv)) {
      await fetchGraph(agv)
    }
  }
}

/**
 * Update Graph elements on map
 */
function updateGraphElements(): void {
  mapComponent.value.clear()

  const graphs: IGraph[] = []
  for (const agv of selectedAgvs.value) {
    const graph: IGraph | undefined = agvGraph.value.data.get(agv)
    if (graph != undefined) {
      graphs.push(graph)
    }
  }
  mapComponent.value.addGraphs(graphs)
}
</script>

<style lang="scss">
@use '@/styles/vars';
@use '@material/theme/theme-color';
@use 'balm-ui/src/material-components-web/drawer/variables' as drawerVars;

$map-menu-width: 150px;

#map-menu {
  overflow-y: auto;
  overflow-x: hidden;
  height: 100%;
  top: 0;
  right: 0;
  position: fixed;
  padding: 15px;
  border-left: 1px solid
    rgba(
      theme-color.prop-value(theme-color.$on-surface),
      drawerVars.$divider-opacity
    );
  background-color: theme-color.prop-value(theme-color.$surface);
  z-index: 1;
}

#map-manu-agvs,
#map-menu-maps {
  clear: left;
  width: $map-menu-width;
}

#map-container {
  border: 2px solid #888888;
  background-color: #dddddd;
  width: calc(100vw - #{vars.$drawer-width} - #{$map-menu-width} - 100px);
  height: calc(100vh - 100px);
  display: block;
}
</style>
