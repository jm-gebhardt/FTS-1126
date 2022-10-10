import { RouteLocationRaw } from 'vue-router'

export interface ListItem {
  label: string
  value: string | null | undefined
  route: RouteLocationRaw | null
}
