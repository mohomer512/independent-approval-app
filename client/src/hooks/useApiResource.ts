import { useCallback, useEffect, useRef, useState } from 'react'
import { getSafeApiErrorMessage } from '../services'

interface ApiResourceState<T> {
  readonly data: T | null
  readonly loading: boolean
  readonly error: string | null
}

export interface UseApiResourceResult<T> extends ApiResourceState<T> {
  readonly retry: () => void
}

const createInitialState = <T,>(): ApiResourceState<T> => ({
  data: null,
  loading: true,
  error: null,
})

export function useApiResource<T>(
  load: (signal: AbortSignal) => Promise<T>,
  unavailableMessage: string,
): UseApiResourceResult<T> {
  const [state, setState] = useState<ApiResourceState<T>>(createInitialState)
  const [requestKey, setRequestKey] = useState(0)
  const activeRequestRef = useRef<AbortController | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    let isCurrentRequest = true
    activeRequestRef.current = controller

    const loadResource = async () => {
      try {
        const data = await load(controller.signal)

        if (isCurrentRequest && !controller.signal.aborted) {
          setState({ data, loading: false, error: null })
        }
      } catch (error: unknown) {
        if (isCurrentRequest && !controller.signal.aborted) {
          setState({
            data: null,
            loading: false,
            error: getSafeApiErrorMessage(error, unavailableMessage),
          })
        }
      } finally {
        if (activeRequestRef.current === controller) {
          activeRequestRef.current = null
        }
      }
    }

    void loadResource()

    return () => {
      isCurrentRequest = false
      controller.abort()

      if (activeRequestRef.current === controller) {
        activeRequestRef.current = null
      }
    }
  }, [load, requestKey, unavailableMessage])

  const retry = useCallback(() => {
    activeRequestRef.current?.abort()
    setState(createInitialState())
    setRequestKey((currentKey) => currentKey + 1)
  }, [])

  return { ...state, retry }
}
