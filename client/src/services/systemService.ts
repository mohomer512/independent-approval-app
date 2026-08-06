import type { SystemInfo } from '../models'
import { apiClient } from './apiClient'

export interface SystemService {
  readonly getSystemInfo: (signal?: AbortSignal) => Promise<SystemInfo>
}

export const systemService: SystemService = {
  getSystemInfo: (signal) =>
    apiClient.get<SystemInfo>('/api/system/info', { signal }),
}
