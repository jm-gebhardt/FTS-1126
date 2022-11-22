<template>
  <ui-list :key="props.modelValue" :type="2" non-interactive>
    <template v-for="item in props.modelValue" :key="item.label">
      <ui-item @click.stop>
        <ui-button
          class="action-button"
          outlined
          @click="item.action(item.data)"
          >{{ item.label }}</ui-button
        >
      </ui-item>
    </template>
  </ui-list>
</template>

<script lang="ts">
export default {
  name: 'DetailsList'
}
</script>

<script lang="ts" setup>
import { ActionItem } from '@/components/ActionItem'

interface Props {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  modelValue: ActionItem<any>[]
}

const props = defineProps<Props>()
</script>

<style lang="scss">
.action-button {
  width: 100%;
}
</style>
