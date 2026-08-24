import { useCallback, useEffect, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import type { CurrentUser } from '../models'
import { currentUserService, getSafeApiErrorMessage } from '../services'
import {
  ApplicationSessionContext,
  type ApplicationSessionContextValue,
} from './ApplicationSessionContext'

interface ApplicationSessionProviderProps {
  readonly children: ReactNode
}

interface ApplicationSessionState {
  readonly data: CurrentUser | null
  readonly loading: boolean
  readonly error: string | null
}

const initialState: ApplicationSessionState = {
  data: null,
  loading: true,
  error: null,
}

export function ApplicationSessionProvider({
  children,
}: ApplicationSessionProviderProps) {
  const [state, setState] = useState<ApplicationSessionState>(initialState)
  const [requestKey, setRequestKey] = useState(0)
  const activeRequestRef = useRef<AbortController | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    let isCurrentRequest = true
    activeRequestRef.current = controller

    const loadApplicationSession = async () => {
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
              'Application access could not be confirmed. Please try again.',
            ),
          })
        }
      } finally {
        if (activeRequestRef.current === controller) {
          activeRequestRef.current = null
        }
      }
    }

    void loadApplicationSession()

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

  const value: ApplicationSessionContextValue = { ...state, retry }

  return (
    <ApplicationSessionContext value={value}>
      {children}
    </ApplicationSessionContext>
  )
}
