import { createContext } from 'react'
import type { CurrentUser } from '../models'

export interface ApplicationSessionContextValue {
  readonly data: CurrentUser | null
  readonly loading: boolean
  readonly error: string | null
  readonly retry: () => void
}

export const ApplicationSessionContext =
  createContext<ApplicationSessionContextValue | null>(null)
