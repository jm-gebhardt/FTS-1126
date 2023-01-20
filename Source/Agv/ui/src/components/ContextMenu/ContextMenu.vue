<template>
  <div>
    <div class="agv-name-container">
      <span id="agv-name"> . </span>
    </div>
    <div class="circle open">
      <div class="ring">
        <a
          v-for="(item, index) in data"
          :key="index"
          class="menuItem"
          @click="itemClick(item)"
        >
          <ui-icon :size="36">{{ item.icon }}</ui-icon>
        </a>
      </div>
      <a v-show="isCloseBtnActive" href="#" class="center">
        <ui-icon :size="36">close</ui-icon>
      </a>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import { IContextMenuItem } from './IContextMenuItem'

export default defineComponent({
  name: 'ContextMenu',
  props: {
    data: {
      type: Object,
      default: null
    },
    isCloseBtnActive: Boolean
  },
  mounted() {
    var items = document.querySelectorAll('.menuItem')
    for (var i = 0, itemCount = items.length; i < itemCount; i++) {
      var left =
        (
          50 -
          35 * Math.cos(-0.5 * Math.PI - 2 * (1 / itemCount) * i * Math.PI)
        ).toFixed(4) + '%'

      var top =
        (
          50 +
          35 * Math.sin(-0.5 * Math.PI - 2 * (1 / itemCount) * i * Math.PI)
        ).toFixed(4) + '%'

      items[i].setAttribute('style', 'top: ' + top + '; left: ' + left)
    }
  },
  methods: {
    itemClick(item: IContextMenuItem) {
      item.action()
      const contextMenuElement = document.getElementById('menu')
      if (contextMenuElement) contextMenuElement.style.display = 'none'
    }
  }
})
</script>

<style>
#agv-name {
  color: black;
  -webkit-text-fill-color: white;
  -webkit-text-stroke-width: 1.3px;
  -webkit-text-stroke-color: black;
  font-weight: 900;
  font-size: 20px;
  font-family: Impact;
}

.agv-name-container {
  top: -158px;
  position: absolute;
  left: -100px;
  width: 200px;
  text-align: center;
}

a {
  font-size: 14px;
  text-decoration: none;
  outline: 0;
}

.circle,
.ring {
  height: 256px;
  position: relative;
  width: 256px;
}

.circle {
  margin: 0 auto;
  top: -125px;
  left: -128px;
}

.ring {
  border-radius: 50%;
  opacity: 0;
  overflow: hidden;
}

.ring:after {
  content: '';
  height: 80px;
  width: 80px;
  border-radius: 100%;
  box-shadow: 0px 0px 0px 98px rgb(0 0 0 / 50%);
  top: 88px;
  position: absolute;
  left: 88px;
}

.open .ring {
  opacity: 1;
}

.center {
  background-color: rgb(0 0 0 / 79%);
  border-radius: 50%;
  border: 2px solid #ffffff;
  bottom: 0;
  color: white;
  height: 65px;
  left: 0;
  line-height: 80px;
  margin: auto;
  position: absolute;
  right: 0;
  text-align: center;
  top: 0;
  width: 80px;
  padding-top: 15px;
}

.open .center {
  border-color: #aaaaaa;
}

.menuItem {
  border-radius: 50%;
  color: #eeeeee;
  display: block;
  height: 40px;
  line-height: 40px;
  margin-left: -20px;
  margin-top: -20px;
  position: absolute;
  text-align: center;
  width: 40px;
  z-index: 1;
}

.menuItem:hover {
  background-color: #ababab87;
  cursor: pointer;
}
</style>
