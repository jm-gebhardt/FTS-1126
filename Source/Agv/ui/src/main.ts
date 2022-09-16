import { createApp } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'
import { createPinia } from 'pinia'

import '@/styles/index.scss'

import App from './App.vue'
import BalmUI from 'balm-ui'
import VueKonva from 'vue-konva'
import routes from './routes'

const router = createRouter({
  history: createWebHistory(import.meta.env.VITE_BASE_PATH),
  routes
})

const pinia = createPinia()

const app = createApp(App)
app.use(router)
app.use(pinia)
app.use(BalmUI)
app.use(VueKonva)
app.mount('#app')
