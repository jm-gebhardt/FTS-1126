<template>
  <div id="nav-main">
    <nav-bar></nav-bar>
  </div>
  <div class="content-main">
    <h1>Home</h1>
    Home sweet home
  </div>
</template>

<script lang="ts" setup>
import NavBar from '@/components/NavBar.vue'

interface Props {
  showNavBar: boolean | undefined
}

defineProps<Props>()
</script>
