<template>
  <div id="map" ref="map"></div>
</template>

<script lang="ts" setup>
import { onMounted, ref } from 'vue'
import { MapCanvas } from '@/components/KonvaMap/MapCanvas'
import { IAgvGraph, IAgvPositionReceived } from '@/services/api/data-contracts'

const map = ref()
let graphMap: MapCanvas | undefined

onMounted(() => {
  graphMap = new MapCanvas(map.value)
})

defineExpose({
  addGraphs: (graphs: IAgvGraph[]) => {
    graphMap?.addGraphs(graphs)
  },
  setVisibleAgvs: (agvCorrelationIds: string[]) => {
    graphMap?.setVisibleAgvs(agvCorrelationIds)
  },
  addAgvs: (agvs: IAgvPositionReceived[]) => {
    graphMap?.addAgvs(agvs)
  },
  updateAgvPosition(agv: IAgvPositionReceived) {
    graphMap?.updateAgvPosition(agv)
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
