import { useCallback, useEffect, useRef, useState } from 'react'
import type { DocumentListResponse } from '../models'
import { documentService, getSafeApiErrorMessage } from '../services'

interface DocumentsState {
  readonly data: DocumentListResponse | null
  readonly loading: boolean
  readonly error: string | null
}

export interface UseDocumentsResult extends DocumentsState {
  readonly retry: () => void
}

const initialState: DocumentsState = {
  data: null,
  loading: true,
  error: null,
}

export function useDocuments(
  page = 1,
  pageSize = 50,
): UseDocumentsResult {
  const [state, setState] = useState<DocumentsState>(initialState)
  const [requestKey, setRequestKey] = useState(0)
  const activeRequestRef = useRef<AbortController | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    let isCurrentRequest = true
    activeRequestRef.current = controller

    const loadDocuments = async () => {
      try {
        const data = await documentService.getDocuments(
          page,
          pageSize,
          controller.signal,
        )

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
              'Documents are unavailable. Please try again.',
            ),
          })
        }
      } finally {
        if (activeRequestRef.current === controller) {
          activeRequestRef.current = null
        }
      }
    }

    void loadDocuments()

    return () => {
      isCurrentRequest = false
      controller.abort()

      if (activeRequestRef.current === controller) {
        activeRequestRef.current = null
      }
    }
  }, [page, pageSize, requestKey])

  const retry = useCallback(() => {
    activeRequestRef.current?.abort()
    setState(initialState)
    setRequestKey((currentKey) => currentKey + 1)
  }, [])

  return { ...state, retry }
}
