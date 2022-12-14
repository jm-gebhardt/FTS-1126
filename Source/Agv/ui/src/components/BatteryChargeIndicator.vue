<template>
  <div class="battery-charge-container">
    <svg :width="width + 10 + decorationSize()" :height="height + 4">
      <rect
        x="3"
        y="2"
        :width="chargeBarWidth()"
        :height="height"
        :fill="chargeBarColor()"
      />
      <rect
        x="2"
        y="2"
        rx="6"
        ry="6"
        :width="width + 2"
        :height="height"
        stroke="black"
        stroke-width="3"
        fill="transparent"
      />
      <path
        x="2"
        y="2"
        :d="path"
        stroke="black"
        stroke-width="3"
        fill="transparent"
      />
      <text
        :x="width / 2"
        :y="height * 0.75"
        :font-size="Math.floor(height * 0.6)"
        text-anchor="middle"
      >
        {{ getCharge() }}%
      </text>
    </svg>
  </div>
</template>

<script lang="ts">
export default {
  name: 'BatteryChargeIndicator'
}
</script>

<script lang="ts" setup>
const props = defineProps<{
  charge: number | null | undefined
}>()

const width = 160
const height = 40

const path = `
  M ${width + 4},${Math.floor(height * 0.35)}
  l ${decorationSize()},0
  a 4,4 0 0 1 4,4
  l 0,${Math.floor(height * 0.2)}
  a-4,4 0 0 1 -4,4
  l-${decorationSize()},0`

function getCharge(): number {
  return Math.max(Math.min(Math.round(props.charge ?? 0), 100), 0) // Clamp charge to valid range [0..100]
}

function decorationSize(): number {
  return Math.max(Math.floor(width * 0.02), 3)
}

function chargeBarWidth(): number {
  const charge = getCharge()
  return width * charge * 0.01
}

function chargeBarColor(): string {
  const charge = getCharge()
  const red = charge < 50 ? 200 : 400 - 4 * charge
  const green = charge > 50 ? 200 : 4 * charge
  return `rgb(${red}, ${green}, 0)`
}
</script>

<style lang="scss">
.battery-charge-container {
  width: fit-content;
  height: fit-content;
  margin: 4px;
}
</style>
