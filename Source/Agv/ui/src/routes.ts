type HomeViewType = typeof import('@/views/HomeView.vue')
type OrderViewType = typeof import('@/views/OrdersView.vue')
type AgvViewType = typeof import('@/views/AgvView.vue')
type MapViewType = typeof import('@/views/MapView.vue')

export default [
  {
    component: (): Promise<HomeViewType> => import('@/views/HomeView.vue'),
    name: 'Home',
    path: '/',
    props: { showNavBar: true }
  },
  {
    component: (): Promise<OrderViewType> => import('@/views/OrdersView.vue'),
    name: 'Orders',
    path: '/orders',
    props: { showNavBar: true }
  },
  {
    component: (): Promise<AgvViewType> => import('@/views/AgvView.vue'),
    name: 'AGVs',
    path: '/agvs',
    props: { showNavBar: true }
  },
  {
    component: (): Promise<MapViewType> => import('@/views/MapView.vue'),
    name: 'Maps',
    path: '/maps',
    props: { showNavBar: true }
  },
  {
    component: (): Promise<OrderViewType> => import('@/views/OrdersView.vue'),
    name: 'SingleOrdersPage',
    path: '/integrated/orders',
    alias: '/i/orders',
    props: { showNavBar: false }
  },
  {
    component: (): Promise<AgvViewType> => import('@/views/AgvView.vue'),
    name: 'SingleAgvPage',
    path: '/integrated/agvs',
    alias: '/i/agvs',
    props: { showNavBar: false }
  },
  {
    component: (): Promise<MapViewType> => import('@/views/MapView.vue'),
    name: 'SingleMapPage',
    path: '/integrated/maps',
    alias: '/i/maps',
    props: { showNavBar: false }
  }
]
