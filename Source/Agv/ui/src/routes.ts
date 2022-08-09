type HomeViewType = typeof import('@/views/HomeView.vue')
type OrderViewType = typeof import('@/views/OrdersView.vue')

export default [
  {
    component: (): Promise<HomeViewType> => import('@/views/HomeView.vue'),
    name: 'Home',
    path: '/'
  },
  {
    component: (): Promise<OrderViewType> => import('@/views/OrdersView.vue'),
    name: 'Orders',
    path: '/order'
  }
]
