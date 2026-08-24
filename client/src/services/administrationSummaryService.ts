import type { AdministrationSummary } from '../models'
import { apiClient } from './apiClient'

export interface AdministrationSummaryService {
  readonly getSummary: (
    signal?: AbortSignal,
  ) => Promise<AdministrationSummary>
}

export const administrationSummaryService: AdministrationSummaryService = {
  getSummary: (signal) =>
    apiClient.get<AdministrationSummary>('/api/admin/summary', { signal }),
}
