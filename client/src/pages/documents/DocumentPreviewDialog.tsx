import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  MessageBar,
  MessageBarBody,
  Spinner,
} from '@fluentui/react-components'
import {
  ArrowClockwise20Regular,
  ArrowDownload20Regular,
} from '@fluentui/react-icons'
import type { DocumentPreviewContent } from '../../hooks/useDocumentPreview'
import type { ApiDocument } from '../../models'

export interface DocumentPreviewDialogProps {
  readonly document: ApiDocument | null
  readonly content: DocumentPreviewContent | null
  readonly loading: boolean
  readonly error: string | null
  readonly downloadError?: string | null
  readonly downloading?: boolean
  readonly onClose: () => void
  readonly onRetry: () => void
  readonly onDownload: (libraryDocument: ApiDocument) => void
}

function renderPreviewContent(
  libraryDocument: ApiDocument,
  content: DocumentPreviewContent | null,
  loading: boolean,
  error: string | null,
  onRetry: () => void,
) {
  if (loading) {
    return (
      <div className="document-preview__state" role="status">
        <Spinner size="small" label="Opening document preview" />
        <p>Loading {libraryDocument.originalFileName}.</p>
      </div>
    )
  }

  if (error !== null) {
    return (
      <div
        className="document-preview__state document-preview__state--error"
        role="alert"
      >
        <p>{error}</p>
        <Button
          type="button"
          appearance="primary"
          icon={<ArrowClockwise20Regular />}
          onClick={onRetry}
        >
          Retry preview
        </Button>
      </div>
    )
  }

  if (content === null) {
    return (
      <div className="document-preview__state" role="status">
        <p>Preview content is unavailable. Download the document instead.</p>
      </div>
    )
  }

  switch (content.kind) {
    case 'pdf':
      return (
        <div className="document-preview__pdf">
          <iframe
            className="document-preview__frame"
            src={content.objectUrl}
            title={`Preview of ${libraryDocument.originalFileName}`}
            sandbox=""
            referrerPolicy="no-referrer"
          />
          <p>
            If the PDF preview is unavailable in this browser, use Download
            below.
          </p>
        </div>
      )
    case 'image':
      return (
        <img
          className="document-preview__image"
          src={content.objectUrl}
          alt={`Preview of ${libraryDocument.originalFileName}`}
        />
      )
    case 'text':
      return (
        <pre
          className="document-preview__text"
          tabIndex={0}
          aria-label={`Text preview of ${libraryDocument.originalFileName}`}
        >
          {content.text}
        </pre>
      )
  }
}

export function DocumentPreviewDialog({
  document: libraryDocument,
  content,
  loading,
  error,
  downloadError = null,
  downloading = false,
  onClose,
  onRetry,
  onDownload,
}: DocumentPreviewDialogProps) {
  return (
    <Dialog
      open={libraryDocument !== null}
      onOpenChange={(_, data) => {
        if (!data.open) {
          onClose()
        }
      }}
    >
      <DialogSurface className="document-preview">
        <DialogBody>
          <DialogTitle>
            {libraryDocument === null
              ? 'Document preview'
              : libraryDocument.originalFileName}
          </DialogTitle>
          <DialogContent>
            {libraryDocument === null
              ? null
              : renderPreviewContent(
                  libraryDocument,
                  content,
                  loading,
                  error,
                  onRetry,
                )}
            {downloadError === null ? null : (
              <MessageBar intent="error" politeness="assertive">
                <MessageBarBody>{downloadError}</MessageBarBody>
              </MessageBar>
            )}
          </DialogContent>
          <DialogActions className="document-preview__actions">
            {libraryDocument === null ? null : (
              <Button
                type="button"
                appearance="secondary"
                icon={<ArrowDownload20Regular />}
                disabled={downloading}
                onClick={() => onDownload(libraryDocument)}
              >
                {downloading ? 'Downloading' : 'Download'}
              </Button>
            )}
            <Button type="button" appearance="primary" onClick={onClose}>
              Close
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
