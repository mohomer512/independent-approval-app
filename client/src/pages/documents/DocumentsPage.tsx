import {
  Button,
  MessageBar,
  MessageBarActions,
  MessageBarBody,
  Spinner,
  useRestoreFocusTarget,
} from '@fluentui/react-components'
import {
  ArrowClockwise20Regular,
  ArrowDownload20Regular,
  ArrowUpload24Regular,
  Dismiss20Regular,
  DismissCircle24Regular,
  Document24Regular,
  Eye20Regular,
} from '@fluentui/react-icons'
import { useEffect, useRef, useState } from 'react'
import {
  formatDateTime,
  formatFileSize,
  PageHeader,
  SectionPanel,
  StatusBadge,
} from '../../components/common'
import { useDocumentPreview } from '../../hooks/useDocumentPreview'
import { useDocuments } from '../../hooks/useDocuments'
import type { ApiDocument } from '../../models'
import { documentService, getSafeApiErrorMessage } from '../../services'
import { DocumentPreviewDialog } from './DocumentPreviewDialog'
import { DocumentUploadDialog } from './DocumentUploadDialog'
import {
  getDocumentPreviewUnavailableMessage,
  isDocumentPreviewSupported,
} from './documentPreviewPolicy'

const initialPage = 1
const initialPageSize = 50

function getFileExtension(fileName: string): string {
  const extension = /\.([^.]+)$/.exec(fileName)?.[1]

  return extension ? extension.slice(0, 4).toUpperCase() : 'FILE'
}

export function DocumentsPage() {
  const { data, loading, error, retry } = useDocuments(
    initialPage,
    initialPageSize,
  )
  const preview = useDocumentPreview()
  const restoreFocusTargetAttributes = useRestoreFocusTarget()
  const [uploadDialogOpen, setUploadDialogOpen] = useState(false)
  const [uploadSuccessMessage, setUploadSuccessMessage] = useState<
    string | null
  >(null)
  const [downloadingIds, setDownloadingIds] = useState<ReadonlySet<string>>(
    () => new Set(),
  )
  const [downloadErrors, setDownloadErrors] = useState<
    Readonly<Record<string, string>>
  >({})
  const activeDownloadsRef = useRef(new Map<string, AbortController>())
  const isMountedRef = useRef(true)

  useEffect(() => {
    const activeDownloads = activeDownloadsRef.current
    isMountedRef.current = true

    return () => {
      isMountedRef.current = false
      activeDownloads.forEach((controller) => controller.abort())
      activeDownloads.clear()
    }
  }, [])

  const handleDownload = async (libraryDocument: ApiDocument) => {
    if (activeDownloadsRef.current.has(libraryDocument.id)) {
      return
    }

    const controller = new AbortController()
    activeDownloadsRef.current.set(libraryDocument.id, controller)
    setDownloadingIds((currentIds) => {
      const nextIds = new Set(currentIds)
      nextIds.add(libraryDocument.id)
      return nextIds
    })
    setDownloadErrors((currentErrors) => {
      const nextErrors = { ...currentErrors }
      delete nextErrors[libraryDocument.id]
      return nextErrors
    })

    let objectUrl: string | null = null
    let downloadLink: HTMLAnchorElement | null = null

    try {
      const documentBlob = await documentService.downloadDocument(
        libraryDocument.downloadUrl,
        controller.signal,
      )

      if (controller.signal.aborted) {
        return
      }

      objectUrl = URL.createObjectURL(documentBlob)
      downloadLink = window.document.createElement('a')
      downloadLink.href = objectUrl
      downloadLink.download = libraryDocument.originalFileName
      downloadLink.hidden = true
      window.document.body.append(downloadLink)
      downloadLink.click()
    } catch (downloadError: unknown) {
      if (!controller.signal.aborted && isMountedRef.current) {
        setDownloadErrors((currentErrors) => ({
          ...currentErrors,
          [libraryDocument.id]: getSafeApiErrorMessage(
            downloadError,
            'The document could not be downloaded. Please try again.',
          ),
        }))
      }
    } finally {
      downloadLink?.remove()

      if (objectUrl !== null) {
        URL.revokeObjectURL(objectUrl)
      }

      if (activeDownloadsRef.current.get(libraryDocument.id) === controller) {
        activeDownloadsRef.current.delete(libraryDocument.id)

        if (isMountedRef.current) {
          setDownloadingIds((currentIds) => {
            const nextIds = new Set(currentIds)
            nextIds.delete(libraryDocument.id)
            return nextIds
          })
        }
      }
    }
  }

  const handleUploaded = (uploadedDocument: ApiDocument) => {
    setUploadSuccessMessage(
      `${uploadedDocument.originalFileName} was uploaded successfully.`,
    )
    retry()
  }

  const documents = data?.items ?? []

  return (
    <div className="documents-page page-stack">
      <PageHeader
        eyebrow="Content"
        title="Document Library"
        description="Browse and download documents stored in the approval library."
        actions={
          <Button
            {...restoreFocusTargetAttributes}
            type="button"
            appearance="primary"
            icon={<ArrowUpload24Regular />}
            onClick={() => {
              setUploadSuccessMessage(null)
              setUploadDialogOpen(true)
            }}
          >
            Upload new file
          </Button>
        }
      />

      {uploadSuccessMessage === null ? null : (
        <MessageBar
          className="document-upload-success"
          intent="success"
          politeness="polite"
        >
          <MessageBarBody>{uploadSuccessMessage}</MessageBarBody>
          <MessageBarActions
            containerAction={
              <Button
                type="button"
                appearance="transparent"
                icon={<Dismiss20Regular />}
                aria-label="Dismiss upload confirmation"
                onClick={() => setUploadSuccessMessage(null)}
              />
            }
          />
        </MessageBar>
      )}

      <SectionPanel
        title="Library contents"
        description="Documents currently available from the independent approval API."
        action={
          <Button
            type="button"
            appearance="outline"
            icon={<ArrowClockwise20Regular />}
            disabled={loading}
            onClick={retry}
          >
            Refresh
          </Button>
        }
      >
        <div className="document-library" aria-busy={loading}>
          {loading ? (
            <div className="document-library__state" role="status">
              <Spinner size="small" label="Loading documents" />
              <p>Requesting the latest document library contents.</p>
            </div>
          ) : error ? (
            <div
              className="document-library__state document-library__state--error"
              role="alert"
            >
              <div className="document-library__state-icon" aria-hidden="true">
                <DismissCircle24Regular />
              </div>
              <div className="document-library__state-copy">
                <h3>Document library is unavailable</h3>
                <p>{error}</p>
              </div>
              <Button
                type="button"
                appearance="primary"
                icon={<ArrowClockwise20Regular />}
                onClick={retry}
              >
                Retry
              </Button>
            </div>
          ) : documents.length === 0 ? (
            <div
              className="document-library__state document-library__state--empty"
              role="status"
            >
              <div className="document-library__state-icon" aria-hidden="true">
                <Document24Regular />
              </div>
              <div className="document-library__state-copy">
                <h3>No documents available</h3>
                <p>Documents added to the library will appear here.</p>
              </div>
            </div>
          ) : (
            <>
              <div className="data-table-scroll" tabIndex={0}>
                <table className="data-table data-table--documents">
                  <caption className="visually-hidden">
                    Documents available in the approval library
                  </caption>
                  <thead>
                    <tr>
                      <th scope="col">Document</th>
                      <th scope="col">Description</th>
                      <th scope="col">Status</th>
                      <th scope="col">Uploaded by</th>
                      <th scope="col">Uploaded</th>
                      <th scope="col">
                        <span className="visually-hidden">Actions</span>
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {documents.map((libraryDocument) => {
                      const isDownloading = downloadingIds.has(
                        libraryDocument.id,
                      )
                      const downloadError = downloadErrors[libraryDocument.id]
                      const description = libraryDocument.description?.trim()
                      const downloadLabel = isDownloading
                        ? 'Downloading'
                        : downloadError
                          ? 'Retry download'
                          : 'Download'
                      const previewSupported = isDocumentPreviewSupported(
                        libraryDocument,
                      )
                      const previewUnavailableMessage = previewSupported
                        ? null
                        : getDocumentPreviewUnavailableMessage(
                            libraryDocument,
                          )
                      const isOpeningPreview =
                        preview.loading &&
                        preview.document?.id === libraryDocument.id

                      return (
                        <tr key={libraryDocument.id}>
                          <td>
                            <span className="document-name">
                              <span
                                className="document-name__extension"
                                aria-hidden="true"
                              >
                                {getFileExtension(
                                  libraryDocument.originalFileName,
                                )}
                              </span>
                              <span>
                                <span className="data-table__primary">
                                  {libraryDocument.originalFileName}
                                </span>
                                <span className="data-table__secondary">
                                  {formatFileSize(libraryDocument.fileSize)}
                                </span>
                              </span>
                            </span>
                          </td>
                          <td
                            className={
                              description
                                ? 'document-description'
                                : 'document-description document-description--empty'
                            }
                          >
                            {description || 'No description provided'}
                          </td>
                          <td>
                            <StatusBadge status={libraryDocument.status} />
                          </td>
                          <td>{libraryDocument.uploadedBy}</td>
                          <td>
                            <time dateTime={libraryDocument.uploadedAtUtc}>
                              {formatDateTime(libraryDocument.uploadedAtUtc)}
                            </time>
                          </td>
                          <td className="data-table__action document-actions">
                            <div className="document-actions__buttons">
                              {previewSupported ? (
                                <Button
                                  {...restoreFocusTargetAttributes}
                                  type="button"
                                  appearance="subtle"
                                  size="small"
                                  icon={<Eye20Regular />}
                                  disabled={isOpeningPreview}
                                  aria-label={`${isOpeningPreview ? 'Opening preview of' : 'Open preview of'} ${libraryDocument.originalFileName}`}
                                  onClick={() =>
                                    void preview.openDocument(libraryDocument)
                                  }
                                >
                                  {isOpeningPreview ? 'Opening' : 'Open'}
                                </Button>
                              ) : (
                                <span
                                  className="document-actions__preview-unavailable"
                                >
                                  <span aria-hidden="true">
                                    Download to open
                                  </span>
                                  <span className="visually-hidden">
                                    {previewUnavailableMessage}
                                  </span>
                                </span>
                              )}
                              <Button
                                type="button"
                                appearance="subtle"
                                size="small"
                                icon={<ArrowDownload20Regular />}
                                disabled={isDownloading}
                                aria-label={`${downloadLabel} ${libraryDocument.originalFileName}`}
                                onClick={() =>
                                  void handleDownload(libraryDocument)
                                }
                              >
                                {downloadLabel}
                              </Button>
                            </div>
                            {downloadError ? (
                              <span
                                className="document-actions__error"
                                role="alert"
                              >
                                {downloadError}
                              </span>
                            ) : null}
                          </td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
              <div className="table-footer">
                <span>
                  Showing {documents.length} of {data?.totalCount ?? 0} documents
                </span>
                <span>Page {data?.page ?? initialPage}</span>
              </div>
            </>
          )}
        </div>
      </SectionPanel>

      <DocumentUploadDialog
        open={uploadDialogOpen}
        onOpenChange={setUploadDialogOpen}
        onUploaded={handleUploaded}
      />
      <DocumentPreviewDialog
        document={preview.document}
        content={preview.content}
        loading={preview.loading}
        error={preview.error}
        downloadError={
          preview.document === null
            ? null
            : downloadErrors[preview.document.id] ?? null
        }
        downloading={
          preview.document === null
            ? false
            : downloadingIds.has(preview.document.id)
        }
        onClose={preview.closePreview}
        onRetry={preview.retry}
        onDownload={(libraryDocument) =>
          void handleDownload(libraryDocument)
        }
      />
    </div>
  )
}
