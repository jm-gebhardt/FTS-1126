type HomeViewType = typeof import('@/views/HomeView.vue')
type OrderViewType = typeof import('@/views/OrdersView.vue')
type AgvViewType = typeof import('@/views/AgvView.vue')

export default [
  {
    component: (): Promise<HomeViewType> => import('@/views/HomeView.vue'),
    name: 'Home',
    path: '/'
  },
  {
    component: (): Promise<OrderViewType> => import('@/views/OrdersView.vue'),
    name: 'Orders',
    path: '/orders'
  },
  {
    component: (): Promise<AgvViewType> => import('@/views/AgvView.vue'),
    name: 'AGVs',
    path: '/agvs'
  }
]
