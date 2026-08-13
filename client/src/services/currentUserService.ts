import type { CurrentUser } from '../models'
import { apiClient } from './apiClient'

export interface CurrentUserService {
  readonly getCurrentUser: (signal?: AbortSignal) => Promise<CurrentUser>
}

export const currentUserService: CurrentUserService = {
  getCurrentUser: (signal) =>
    apiClient.get<CurrentUser>('/api/auth/me', { signal }),
}
