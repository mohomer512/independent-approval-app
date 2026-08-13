import { useCallback, useEffect, useRef, useState } from 'react'
import type { CurrentUser } from '../models'
import { currentUserService, getSafeApiErrorMessage } from '../services'

interface CurrentUserState {
  readonly data: CurrentUser | null
  readonly loading: boolean
  readonly error: string | null
}

export interface UseCurrentUserResult extends CurrentUserState {
  readonly retry: () => void
}

const initialState: CurrentUserState = {
  data: null,
  loading: true,
  error: null,
}

export function useCurrentUser(): UseCurrentUserResult {
  const [state, setState] = useState<CurrentUserState>(initialState)
  const [requestKey, setRequestKey] = useState(0)
  const activeRequestRef = useRef<AbortController | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    let isCurrentRequest = true
    activeRequestRef.current = controller

    const loadCurrentUser = async () => {
      try {
        const data = await currentUserService.getCurrentUser(controller.signal)

        if (isCurrentRequest && !controller.signal.aborted) {
          setState({ data, loading: false, error: null })
        }
      } catch (error: unknown) {
        if (isCurrentRequest && !controller.signal.aborted) {
          setState({
            data: null,
            loading: false,
            error: getSafeApiErrorMessage(
              error,
              'User identity is unavailable. Please try again.',
            ),
          })
        }
      } finally {
        if (activeRequestRef.current === controller) {
          activeRequestRef.current = null
        }
      }
    }

    void loadCurrentUser()

    return () => {
      isCurrentRequest = false
      controller.abort()

      if (activeRequestRef.current === controller) {
        activeRequestRef.current = null
      }
    }
  }, [requestKey])

  const retry = useCallback(() => {
    activeRequestRef.current?.abort()
    setState(initialState)
    setRequestKey((currentKey) => currentKey + 1)
  }, [])

  return { ...state, retry }
}
