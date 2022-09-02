<template>
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
    <cytoscape-map ref="mapComponent"></cytoscape-map>
  </div>
</template>

<script lang="ts" setup>
import CytoscapeMap from '@/components/CytoscapeMap/CytoscapeMap.vue'
import { storeToRefs } from 'pinia'
import { useAgvStore } from '@/services/AgvStore'
import { onMounted, ref } from 'vue'
import { IAgv, IAgvGraph, IMap } from '@/services/api/data-contracts'
import {
  BalmUiOption,
  createOptionFromMap,
  createOptionsFromList
} from '@/utils/BalmUiOptions'

const { agvs, agvGraph, maps } = storeToRefs(useAgvStore())
const { fetchAgvs, fetchGraph, fetchMaps, MapUrl } = useAgvStore()

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
})

/**
 * Update the listed agvs
 */
function updateAgvs(): void {
  fetchAgvs().then(() => {
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

    fetchSelectedAgvGraphs().then(() => {
      updateGraphElements()
    })
  })
}

/**
 * Callback for AGV selection has changed
 */
function selectedAgvsChanged(): void {
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

  const image = new Image()
  image.src = MapUrl(selectedMap.value)
  image.onload = (): void => {
    // set background when image is loaded
    mapComponent.value.setBackground(
      image,
      mapObject.resolution,
      mapObject.xOffset,
      mapObject.yOffset
    )
    mapComponent.value.renderBackground()
    mapComponent.value.showAll()
  }
  selectedMapRelatedAgvs.value = mapObject.relatedAgvs
  updateAgvs()
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
  mapComponent.value.removeAll()

  const graphs: IAgvGraph[] = []
  for (const agv of selectedAgvs.value) {
    const graph: IAgvGraph | undefined = agvGraph.value.data.get(agv)
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

.map {
  width: calc(100vw - #{vars.$drawer-width} - #{$map-menu-width} - 60px);
  height: calc(100vh - 100px);
  display: block;
}
</style>
