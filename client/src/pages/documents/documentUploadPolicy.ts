// These client-side checks mirror the current DocumentStorage configuration
// for prompt feedback only. Server configuration and validation remain authoritative.
export const DOCUMENT_UPLOAD_MAX_FILE_SIZE_BYTES = 26_214_400
export const DOCUMENT_DESCRIPTION_MAX_LENGTH = 2_000

const DOCUMENT_UPLOAD_ALLOWED_EXTENSIONS = [
  '.pdf',
  '.doc',
  '.docx',
  '.xls',
  '.xlsx',
  '.png',
  '.jpg',
  '.jpeg',
  '.txt',
] as const

export const DOCUMENT_UPLOAD_ACCEPT =
  DOCUMENT_UPLOAD_ALLOWED_EXTENSIONS.join(',')

const allowedExtensionSet: ReadonlySet<string> = new Set(
  DOCUMENT_UPLOAD_ALLOWED_EXTENSIONS,
)

export interface DocumentUploadValidationErrors {
  readonly file: string | null
  readonly description: string | null
}

function getNormalizedFileExtension(fileName: string): string {
  const normalizedFileName = fileName.trim().toLowerCase()
  const lastPathSeparatorIndex = Math.max(
    normalizedFileName.lastIndexOf('/'),
    normalizedFileName.lastIndexOf('\\'),
  )
  const baseName = normalizedFileName.slice(lastPathSeparatorIndex + 1)
  const extensionIndex = baseName.lastIndexOf('.')

  return extensionIndex >= 0 ? baseName.slice(extensionIndex) : ''
}

export function validateDocumentUpload(
  file: File | null,
  description: string,
): DocumentUploadValidationErrors {
  let fileError: string | null = null

  if (file === null) {
    fileError = 'Choose a file to upload.'
  } else if (file.size <= 0) {
    fileError = 'The selected file is empty. Choose a file with content.'
  } else if (file.size > DOCUMENT_UPLOAD_MAX_FILE_SIZE_BYTES) {
    fileError = 'The selected file must not exceed 26,214,400 bytes (25 MB).'
  } else if (!allowedExtensionSet.has(getNormalizedFileExtension(file.name))) {
    fileError =
      'Choose a PDF, Word, Excel, PNG, JPEG, or plain-text document.'
  }

  const descriptionError =
    description.trim().length > DOCUMENT_DESCRIPTION_MAX_LENGTH
      ? 'Description must not exceed 2,000 characters.'
      : null

  return {
    file: fileError,
    description: descriptionError,
  }
}
