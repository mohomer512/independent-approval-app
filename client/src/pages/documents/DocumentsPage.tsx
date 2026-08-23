import { Button, Spinner } from '@fluentui/react-components'
import {
  ArrowClockwise20Regular,
  ArrowDownload20Regular,
  DismissCircle24Regular,
  Document24Regular,
} from '@fluentui/react-icons'
import { useEffect, useRef, useState } from 'react'
import {
  formatDateTime,
  formatFileSize,
  PageHeader,
  SectionPanel,
  StatusBadge,
} from '../../components/common'
import { useDocuments } from '../../hooks/useDocuments'
import type { ApiDocument } from '../../models'
import { documentService, getSafeApiErrorMessage } from '../../services'

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

  const documents = data?.items ?? []

  return (
    <div className="documents-page page-stack">
      <PageHeader
        eyebrow="Content"
        title="Document Library"
        description="Browse and download documents stored in the approval library."
      />

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
                          <td className="data-table__action document-download">
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
                            {downloadError ? (
                              <span
                                className="document-download__error"
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
    </div>
  )
}
