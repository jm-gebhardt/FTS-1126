<template>
  <div id="map" ref="map"></div>
</template>

<script lang="ts" setup>
import { onMounted, ref } from 'vue'
import { MapCanvas } from '@/components/KonvaMap/MapCanvas'
import { IAgvGraph, IAgvPosition } from '@/services/api/data-contracts'

const map = ref()
let graphMap: MapCanvas | undefined

onMounted(() => {
  graphMap = new MapCanvas(map.value)
  graphMap.showAll()
})

defineExpose({
  addAgvs: (agvs: IAgvPosition[]) => {
    graphMap?.addAgvs(agvs)
  },
  addGraphs: (graphs: IAgvGraph[]) => {
    console.log(graphs)
  },
  updateAgvPosition(agv: IAgvPosition) {
    graphMap?.UpdateAgvPosition(agv)
  },
  setBackground: (
    background: HTMLImageElement,
    resolution: number,
    xOffset: number,
    yOffset: number
  ) => {
    graphMap?.setBackground(background, resolution, xOffset, yOffset)
  },
  renderBackground: () => graphMap?.renderBackground(),
  showAll: () => graphMap?.showAll(),
  clear: () => graphMap?.clear()
})
</script>
<style lang="scss">
#map {
  width: 100%;
  height: 100%;
  display: block;
}
</style>
