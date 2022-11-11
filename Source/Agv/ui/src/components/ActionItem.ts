export interface ActionItem<T> {
  label: string
  data: T
  action: (data: T) => void
}
