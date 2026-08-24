import { useCallback } from 'react'
import type { AdministrationSummary } from '../models'
import { administrationSummaryService } from '../services'
import { useApiResource } from './useApiResource'

export function useAdministrationSummary() {
  const loadSummary = useCallback(
    (signal: AbortSignal) => administrationSummaryService.getSummary(signal),
    [],
  )

  return useApiResource<AdministrationSummary>(
    loadSummary,
    'Administration summary is unavailable. Please try again.',
  )
}
