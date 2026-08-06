import { useCallback, useEffect, useRef, useState } from 'react'
import type { SystemInfo } from '../models'
import { getSafeApiErrorMessage, systemService } from '../services'

interface SystemInfoState {
  readonly data: SystemInfo | null
  readonly loading: boolean
  readonly error: string | null
}

export interface UseSystemInfoResult extends SystemInfoState {
  readonly retry: () => void
}

const initialState: SystemInfoState = {
  data: null,
  loading: true,
  error: null,
}

export function useSystemInfo(): UseSystemInfoResult {
  const [state, setState] = useState<SystemInfoState>(initialState)
  const [requestKey, setRequestKey] = useState(0)
  const activeRequestRef = useRef<AbortController | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    let isCurrentRequest = true
    activeRequestRef.current = controller

    const loadSystemInfo = async () => {
      try {
        const data = await systemService.getSystemInfo(controller.signal)

        if (isCurrentRequest && !controller.signal.aborted) {
          setState({ data, loading: false, error: null })
        }
      } catch (error: unknown) {
        if (isCurrentRequest && !controller.signal.aborted) {
          setState({
            data: null,
            loading: false,
            error: getSafeApiErrorMessage(error),
          })
        }
      } finally {
        if (activeRequestRef.current === controller) {
          activeRequestRef.current = null
        }
      }
    }

    void loadSystemInfo()

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
