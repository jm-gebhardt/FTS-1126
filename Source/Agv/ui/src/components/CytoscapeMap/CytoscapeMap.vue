<template>
  <div ref="map" class="map"></div>
</template>

<script lang="ts" setup>
import { onMounted, ref } from 'vue'
import { Map } from '@/components/CytoscapeMap/Map'
import { IAgvGraph } from '@/services/api/data-contracts'

const map = ref()
let graphMap: Map | undefined

onMounted(() => {
  graphMap = new Map(map.value)
  graphMap.showAll()
})

defineExpose({
  addGraphs: (graphs: IAgvGraph[]) => {
    graphMap?.addGraphs(graphs)
  },
  removeAll: () => graphMap?.removeAll(),
  setBackground: (
    background: HTMLImageElement,
    resolution: number,
    xOffset: number,
    yOffset: number
  ) => {
    graphMap?.setBackground(background, resolution, xOffset, yOffset)
  },
  renderBackground: () => graphMap?.renderBackground(),
  showAll: () => graphMap?.showAll()
})
</script>
