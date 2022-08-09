<template>
  <div id="nav-main">
    <nav-bar></nav-bar>
  </div>
  <!-- App content -->
  <div id="content-main">
    <RouterView></RouterView>
  </div>
</template>

<script lang="ts" setup>
import NavBar from '@/components/NavBar.vue'
</script>
