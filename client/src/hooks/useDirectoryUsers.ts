import { useCallback, useEffect, useState } from 'react'
import type { DirectoryUserSearchResponse } from '../models'
import { adminUserService, getSafeApiErrorMessage } from '../services'

interface DirectoryUsersState {
  readonly key: string
  readonly data: DirectoryUserSearchResponse | null
  readonly loading: boolean
  readonly error: string | null
}

export interface UseDirectoryUsersResult {
  readonly data: DirectoryUserSearchResponse | null
  readonly loading: boolean
  readonly error: string | null
  readonly retry: () => void
}

const initialState: DirectoryUsersState = {
  key: '',
  data: null,
  loading: false,
  error: null,
}

export function useDirectoryUsers(
  query: string,
  page: number,
  pageSize: number,
  enabled: boolean,
): UseDirectoryUsersResult {
  const normalizedQuery = query.trim()
  const searchKey = `${normalizedQuery}\u0000${page}\u0000${pageSize}`
  const [state, setState] = useState<DirectoryUsersState>(initialState)
  const [retryKey, setRetryKey] = useState(0)

  useEffect(() => {
    if (!enabled) {
      return
    }

    const controller = new AbortController()
    let isCurrentRequest = true

    const searchDirectory = async () => {
      try {
        const data = await adminUserService.searchDirectoryUsers(
          { query: normalizedQuery, page, pageSize },
          controller.signal,
        )

        if (isCurrentRequest && !controller.signal.aborted) {
          setState({ key: searchKey, data, loading: false, error: null })
        }
      } catch (error: unknown) {
        if (isCurrentRequest && !controller.signal.aborted) {
          setState({
            key: searchKey,
            data: null,
            loading: false,
            error: getSafeApiErrorMessage(
              error,
              'Directory search is unavailable. Please try again.',
            ),
          })
        }
      }
    }

    void searchDirectory()

    return () => {
      isCurrentRequest = false
      controller.abort()
    }
  }, [enabled, normalizedQuery, page, pageSize, retryKey, searchKey])

  const retry = useCallback(() => {
    if (!enabled) {
      return
    }

    setState({ key: searchKey, data: null, loading: true, error: null })
    setRetryKey((currentKey) => currentKey + 1)
  }, [enabled, searchKey])

  if (!enabled) {
    return { data: null, loading: false, error: null, retry }
  }

  if (state.key !== searchKey) {
    return { data: null, loading: true, error: null, retry }
  }

  return {
    data: state.data,
    loading: state.loading,
    error: state.error,
    retry,
  }
}
