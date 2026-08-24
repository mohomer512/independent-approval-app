import { use } from 'react'
import {
  ApplicationSessionContext,
  type ApplicationSessionContextValue,
} from '../app/ApplicationSessionContext'

export type UseCurrentUserResult = ApplicationSessionContextValue

export function useCurrentUser(): UseCurrentUserResult {
  const context = use(ApplicationSessionContext)

  if (context === null) {
    throw new Error(
      'useCurrentUser must be used within ApplicationSessionProvider.',
    )
  }

  return context
}
