import type { ApiDocument } from '../../models'

export type DocumentPreviewKind = 'pdf' | 'image' | 'text'

// Keep very large plain-text files out of the DOM. This limit affects only
// in-page preview; the full server upload limit and Download remain available.
export const DOCUMENT_TEXT_PREVIEW_MAX_FILE_SIZE_BYTES = 1_048_576

export interface DocumentPreviewPolicy {
  readonly kind: DocumentPreviewKind
  readonly baseMimeType: string
}

const previewPolicyByExtension: Readonly<
  Record<string, DocumentPreviewPolicy>
> = {
  '.pdf': { kind: 'pdf', baseMimeType: 'application/pdf' },
  '.png': { kind: 'image', baseMimeType: 'image/png' },
  '.jpg': { kind: 'image', baseMimeType: 'image/jpeg' },
  '.jpeg': { kind: 'image', baseMimeType: 'image/jpeg' },
  '.txt': { kind: 'text', baseMimeType: 'text/plain' },
}

const officeDocumentExtensions: ReadonlySet<string> = new Set([
  '.doc',
  '.docx',
  '.xls',
  '.xlsx',
])

function getNormalizedExtension(fileName: string): string {
  const normalizedFileName = fileName.trim().toLowerCase()
  const lastPathSeparatorIndex = Math.max(
    normalizedFileName.lastIndexOf('/'),
    normalizedFileName.lastIndexOf('\\'),
  )
  const baseName = normalizedFileName.slice(lastPathSeparatorIndex + 1)
  const extensionIndex = baseName.lastIndexOf('.')

  return extensionIndex >= 0 ? baseName.slice(extensionIndex) : ''
}

function getBaseMimeType(contentType: string): string {
  return contentType.split(';', 1)[0]?.trim().toLowerCase() ?? ''
}

export function getDocumentPreviewPolicy(
  libraryDocument: Pick<ApiDocument, 'contentType' | 'originalFileName'>,
): DocumentPreviewPolicy | null {
  const policy =
    previewPolicyByExtension[
      getNormalizedExtension(libraryDocument.originalFileName)
    ]

  if (
    policy === undefined ||
    getBaseMimeType(libraryDocument.contentType) !== policy.baseMimeType
  ) {
    return null
  }

  return policy
}

export function isDocumentPreviewSupported(
  libraryDocument: Pick<ApiDocument, 'contentType' | 'originalFileName'>,
): boolean {
  return getDocumentPreviewPolicy(libraryDocument) !== null
}

export function isDocumentPreviewBlobCompatible(
  libraryDocument: Pick<ApiDocument, 'contentType' | 'originalFileName'>,
  documentBlob: Blob,
): boolean {
  const policy = getDocumentPreviewPolicy(libraryDocument)

  return (
    policy !== null && getBaseMimeType(documentBlob.type) === policy.baseMimeType
  )
}

export function getDocumentPreviewUnavailableMessage(
  libraryDocument: Pick<ApiDocument, 'contentType' | 'originalFileName'>,
): string {
  const extension = getNormalizedExtension(libraryDocument.originalFileName)

  if (officeDocumentExtensions.has(extension)) {
    return 'Preview is unavailable for Word and Excel documents. Download the file to open it in a trusted desktop application.'
  }

  if (previewPolicyByExtension[extension] !== undefined) {
    return 'Preview is unavailable because the document type does not match its filename. Download the file to inspect it safely.'
  }

  return 'Preview is unavailable for this file type. Download the file to open it in a trusted application.'
}
