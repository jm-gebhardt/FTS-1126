export interface BalmUiOption {
  label: string
  value: string | number
}

/**
 * Create BalmUI default option from Map.
 * The value will be the key and the label specfied using labelSelector
 * @param map
 * @param labelSelector:
 */
export function createOptionFromMap<Type>(
  map: Map<string, Type>,
  labelSelector: (obj: Type) => string,
  sorted = true
): BalmUiOption[] {
  const options: BalmUiOption[] = []
  for (const [correlationId, obj] of map) {
    options.push({ label: labelSelector(obj), value: correlationId })
  }
  if (sorted) {
    return sortOptions(options)
  }
  return options
}

/**
 * Create BalmUI default option from list.
 * @param list
 * @param labelSelector
 * @param valueSelector
 */
export function createOptionsFromList<Type>(
  list: IterableIterator<Type> | Array<Type>,
  labelSelector: (obj: Type) => string,
  valueSelector: (obj: Type) => string | number,
  sorted = true
): BalmUiOption[] {
  const options: BalmUiOption[] = []
  for (const obj of list) {
    options.push({ label: labelSelector(obj), value: valueSelector(obj) })
  }

  if (sorted) {
    return sortOptions(options)
  }
  return options
}

/**
 * Sort options by label
 * @param options
 */
function sortOptions(options: BalmUiOption[]): BalmUiOption[] {
  return options.sort((n0, n1) => (n0.label < n1.label ? -1 : 1))
}
