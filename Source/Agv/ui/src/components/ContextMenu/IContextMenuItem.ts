export interface IContextMenuItem {
  name: string
  icon: string
  id: string
  action: action
}

interface action {
  (): void
}
