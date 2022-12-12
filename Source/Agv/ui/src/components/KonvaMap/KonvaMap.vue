<template>
  <div id="map" ref="map"></div>
</template>

<script lang="ts" setup>
import { onMounted, ref } from 'vue'
import { MapCanvas } from '@/components/KonvaMap/MapCanvas'
import {
  IGraph,
  IAgvPositionStatus,
  IAgvState
} from '@/services/api/data-contracts'

const map = ref()
let graphMap: MapCanvas | undefined

onMounted(() => {
  graphMap = new MapCanvas(map.value)
})

defineExpose({
  addGraphs: (graphs: IGraph[]) => {
    graphMap?.addGraphs(graphs)
  },
  setVisibleAgvs: (agvCorrelationIds: string[]) => {
    graphMap?.setVisibleAgvs(agvCorrelationIds)
  },
  addAgvs: (agvs: IAgvPositionStatus[]) => {
    graphMap?.addAgvs(agvs)
  },
  updateAgvPosition(agv: IAgvPositionStatus) {
    graphMap?.updateAgvPosition(agv)
  },
  updateAgvState(agvState: IAgvState) {
    graphMap?.updateAgvState(agvState)
  },
  setBackground: (
    background: HTMLImageElement,
    resolution: number,
    xOffset: number,
    yOffset: number
  ) => {
    graphMap?.setBackground(background, resolution, xOffset, yOffset)
  },
  clear: () => graphMap?.clearGraph()
})
</script>
<style lang="scss">
#map {
  width: 100%;
  height: 100%;
  display: block;
}
</style>
