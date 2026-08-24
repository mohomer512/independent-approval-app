import { useCallback, useEffect, useRef, useState } from 'react'
import type { ApiDocument } from '../models'
import { documentService, getSafeApiErrorMessage } from '../services'
import {
  DOCUMENT_TEXT_PREVIEW_MAX_FILE_SIZE_BYTES,
  getDocumentPreviewPolicy,
  getDocumentPreviewUnavailableMessage,
  isDocumentPreviewBlobCompatible,
  type DocumentPreviewKind,
} from '../pages/documents/documentPreviewPolicy'

export type DocumentPreviewContent =
  | {
      readonly kind: Extract<DocumentPreviewKind, 'pdf' | 'image'>
      readonly objectUrl: string
    }
  | {
      readonly kind: Extract<DocumentPreviewKind, 'text'>
      readonly text: string
    }

interface DocumentPreviewState {
  readonly document: ApiDocument | null
  readonly content: DocumentPreviewContent | null
  readonly loading: boolean
  readonly error: string | null
}

export interface UseDocumentPreviewResult extends DocumentPreviewState {
  readonly isOpen: boolean
  readonly openDocument: (libraryDocument: ApiDocument) => Promise<void>
  readonly closePreview: () => void
  readonly retry: () => void
}

interface ActivePreviewRequest {
  readonly documentId: string
  readonly controller: AbortController
}

const initialState: DocumentPreviewState = {
  document: null,
  content: null,
  loading: false,
  error: null,
}

export function useDocumentPreview(): UseDocumentPreviewResult {
  const [state, setState] = useState<DocumentPreviewState>(initialState)
  const activeRequestRef = useRef<ActivePreviewRequest | null>(null)
  const selectedDocumentRef = useRef<ApiDocument | null>(null)
  const previewObjectUrlRef = useRef<string | null>(null)
  const isMountedRef = useRef(true)

  const revokePreviewObjectUrl = useCallback(() => {
    const objectUrl = previewObjectUrlRef.current

    if (objectUrl === null) {
      return
    }

    previewObjectUrlRef.current = null
    URL.revokeObjectURL(objectUrl)
  }, [])

  const abortActiveRequest = useCallback(() => {
    const activeRequest = activeRequestRef.current

    if (activeRequest === null) {
      return
    }

    activeRequestRef.current = null
    activeRequest.controller.abort()
  }, [])

  const closePreview = useCallback(() => {
    selectedDocumentRef.current = null
    abortActiveRequest()
    revokePreviewObjectUrl()

    if (isMountedRef.current) {
      setState(initialState)
    }
  }, [abortActiveRequest, revokePreviewObjectUrl])

  const openDocument = useCallback(
    async (libraryDocument: ApiDocument) => {
      if (
        activeRequestRef.current?.documentId === libraryDocument.id
      ) {
        return
      }

      abortActiveRequest()
      revokePreviewObjectUrl()
      selectedDocumentRef.current = libraryDocument

      const previewPolicy = getDocumentPreviewPolicy(libraryDocument)

      if (previewPolicy === null) {
        setState({
          document: libraryDocument,
          content: null,
          loading: false,
          error: getDocumentPreviewUnavailableMessage(libraryDocument),
        })
        return
      }

      const activeRequest: ActivePreviewRequest = {
        documentId: libraryDocument.id,
        controller: new AbortController(),
      }

      activeRequestRef.current = activeRequest
      setState({
        document: libraryDocument,
        content: null,
        loading: true,
        error: null,
      })

      const isCurrentRequest = () =>
        isMountedRef.current &&
        !activeRequest.controller.signal.aborted &&
        activeRequestRef.current === activeRequest &&
        selectedDocumentRef.current?.id === libraryDocument.id

      try {
        const documentBlob = await documentService.downloadDocument(
          libraryDocument.downloadUrl,
          activeRequest.controller.signal,
        )

        if (!isCurrentRequest()) {
          return
        }

        if (!isDocumentPreviewBlobCompatible(libraryDocument, documentBlob)) {
          revokePreviewObjectUrl()
          setState({
            document: libraryDocument,
            content: null,
            loading: false,
            error:
              'The downloaded document type could not be previewed safely. Download the file to inspect it in a trusted application.',
          })
          return
        }

        if (previewPolicy.kind === 'text') {
          if (
            documentBlob.size > DOCUMENT_TEXT_PREVIEW_MAX_FILE_SIZE_BYTES
          ) {
            setState({
              document: libraryDocument,
              content: null,
              loading: false,
              error:
                'This text document is too large to preview safely. Download the file instead.',
            })
            return
          }

          const text = await documentBlob.text()

          if (!isCurrentRequest()) {
            return
          }

          setState({
            document: libraryDocument,
            content: { kind: 'text', text },
            loading: false,
            error: null,
          })
          return
        }

        const objectUrl = URL.createObjectURL(documentBlob)

        if (!isCurrentRequest()) {
          URL.revokeObjectURL(objectUrl)
          return
        }

        previewObjectUrlRef.current = objectUrl
        setState({
          document: libraryDocument,
          content: { kind: previewPolicy.kind, objectUrl },
          loading: false,
          error: null,
        })
      } catch (previewError: unknown) {
        if (isCurrentRequest()) {
          revokePreviewObjectUrl()
          setState({
            document: libraryDocument,
            content: null,
            loading: false,
            error: getSafeApiErrorMessage(
              previewError,
              'The document could not be opened. Please try again or download the file.',
            ),
          })
        }
      } finally {
        if (activeRequestRef.current === activeRequest) {
          activeRequestRef.current = null
        }
      }
    },
    [abortActiveRequest, revokePreviewObjectUrl],
  )

  const retry = useCallback(() => {
    const selectedDocument = selectedDocumentRef.current

    if (selectedDocument !== null) {
      void openDocument(selectedDocument)
    }
  }, [openDocument])

  useEffect(() => {
    isMountedRef.current = true

    return () => {
      isMountedRef.current = false
      selectedDocumentRef.current = null
      abortActiveRequest()
      revokePreviewObjectUrl()
    }
  }, [abortActiveRequest, revokePreviewObjectUrl])

  return {
    ...state,
    isOpen: state.document !== null,
    openDocument,
    closePreview,
    retry,
  }
}
