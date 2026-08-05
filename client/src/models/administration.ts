export type AdministrationAreaId =
  | 'users'
  | 'roles'
  | 'workflows'
  | 'request-types'
  | 'settings'

export type AdministrationIconName =
  | 'people'
  | 'permissions'
  | 'workflow'
  | 'form'
  | 'settings'

export interface AdministrationArea {
  readonly id: AdministrationAreaId
  readonly title: string
  readonly description: string
  readonly icon: AdministrationIconName
  readonly itemCount: number | null
  readonly itemLabel: string | null
}

export interface AdministrationMetric {
  readonly id: string
  readonly label: string
  readonly value: number
  readonly description: string
}

export interface AdministrationOverview {
  readonly metrics: readonly AdministrationMetric[]
  readonly areas: readonly AdministrationArea[]
}
