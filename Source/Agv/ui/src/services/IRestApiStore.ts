export interface IRestApiStore<Type> {
  data: Type
  error: string | null
  loading: boolean
}

export interface IPaginatedRestApiStore<Type> extends IRestApiStore<Type> {
  pageSize: number
  totalCount: number
  totalPages: number
  currentPage: number
}
