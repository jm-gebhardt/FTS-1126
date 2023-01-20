<template>
  <ContextMenu
    :data="contextMenuItems"
    :is-close-btn-active="false"
    @mouseover="setId()"
  />
</template>

<script lang="ts">
import ContextMenu from './ContextMenu.vue'
import { defineComponent } from 'vue'
import { AgvService } from '../../services/AgvService'

export default defineComponent({
  name: 'AgvContext',
  components: {
    ContextMenu
  },
  data() {
    return {
      contextMenuItems: [
        {
          name: 'lock',
          icon: 'lock',
          id: '',
          action(): void {
            AgvService.lockAgv(this.id)
          }
        },
        {
          name: 'set idle',
          icon: 'play_arrow',
          id: '',
          action(): void {
            AgvService.setAgvIdle(this.id)
          }
        }
      ]
    }
  },
  methods: {
    setId() {
      var menu = document.getElementById('menu')
      this.contextMenuItems.map(
        (x) => (x.id = menu?.dataset.correlationId || '')
      )
    }
  }
})
</script>
