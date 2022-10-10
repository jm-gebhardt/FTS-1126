<template>
  <ui-list :key="props.modelValue" :type="2" non-interactive>
    <template v-for="item in props.modelValue" :key="item.label">
      <ui-item @click.stop>
        <ui-item-text-content v-if="item.route != null">
          <router-link :to="item.route">
            <ui-item-text1>{{ item.value }}</ui-item-text1>
            <ui-item-text2>{{ item.label }}</ui-item-text2>
          </router-link>
        </ui-item-text-content>
        <ui-item-text-content v-else>
          <ui-item-text1>{{ item.value }}</ui-item-text1>
          <ui-item-text2>{{ item.label }}</ui-item-text2>
        </ui-item-text-content>
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
import { ListItem } from '@/components/ListItem'

interface Props {
  modelValue: ListItem[]
}

const props = defineProps<Props>()
</script>
