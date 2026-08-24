import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  MessageBar,
  MessageBarBody,
  Spinner,
  Textarea,
} from '@fluentui/react-components'
import { ArrowUpload20Regular } from '@fluentui/react-icons'
import { useEffect, useRef, useState } from 'react'
import { formatFileSize } from '../../components/common'
import type { ApiDocument } from '../../models'
import { documentService, getSafeApiErrorMessage } from '../../services'
import {
  DOCUMENT_DESCRIPTION_MAX_LENGTH,
  DOCUMENT_UPLOAD_ACCEPT,
  DOCUMENT_UPLOAD_MAX_FILE_SIZE_BYTES,
  validateDocumentUpload,
} from './documentUploadPolicy'

interface DocumentUploadDialogProps {
  readonly open: boolean
  readonly onOpenChange: (open: boolean) => void
  readonly onUploaded: (uploadedDocument: ApiDocument) => void
}

interface UploadValidationErrors {
  readonly file: string | null
  readonly description: string | null
}

const noValidationErrors: UploadValidationErrors = {
  file: null,
  description: null,
}

export function DocumentUploadDialog({
  open,
  onOpenChange,
  onUploaded,
}: DocumentUploadDialogProps) {
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [description, setDescription] = useState('')
  const [validationErrors, setValidationErrors] =
    useState<UploadValidationErrors>(noValidationErrors)
  const [submissionError, setSubmissionError] = useState<string | null>(null)
  const [isUploading, setIsUploading] = useState(false)
  const fileInputRef = useRef<HTMLInputElement | null>(null)
  const descriptionInputRef = useRef<HTMLTextAreaElement | null>(null)
  const activeUploadRef = useRef<AbortController | null>(null)
  const isMountedRef = useRef(true)

  useEffect(() => {
    isMountedRef.current = true

    return () => {
      isMountedRef.current = false
      activeUploadRef.current?.abort()
      activeUploadRef.current = null
    }
  }, [])

  const resetForm = () => {
    setSelectedFile(null)
    setDescription('')
    setValidationErrors(noValidationErrors)
    setSubmissionError(null)

    if (fileInputRef.current !== null) {
      fileInputRef.current.value = ''
    }
  }

  const closeDialog = () => {
    if (activeUploadRef.current !== null) {
      return
    }

    resetForm()
    onOpenChange(false)
  }

  const handleFileChange = (file: File | null) => {
    setSelectedFile(file)
    setSubmissionError(null)
    setValidationErrors((currentErrors) => ({
      ...currentErrors,
      file: validateDocumentUpload(file, description).file,
    }))
  }

  const handleDescriptionChange = (nextDescription: string) => {
    setDescription(nextDescription)
    setSubmissionError(null)
    setValidationErrors((currentErrors) => ({
      ...currentErrors,
      description: validateDocumentUpload(selectedFile, nextDescription)
        .description,
    }))
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (activeUploadRef.current !== null) {
      return
    }

    const nextValidationErrors = validateDocumentUpload(
      selectedFile,
      description,
    )
    setValidationErrors(nextValidationErrors)

    if (
      nextValidationErrors.file !== null ||
      nextValidationErrors.description !== null ||
      selectedFile === null
    ) {
      if (nextValidationErrors.file !== null || selectedFile === null) {
        fileInputRef.current?.focus()
      } else {
        descriptionInputRef.current?.focus()
      }

      return
    }

    const controller = new AbortController()
    activeUploadRef.current = controller
    setSubmissionError(null)
    setIsUploading(true)

    try {
      const uploadedDocument = await documentService.uploadDocument(
        selectedFile,
        description,
        controller.signal,
      )

      if (controller.signal.aborted || !isMountedRef.current) {
        return
      }

      resetForm()
      onOpenChange(false)
      onUploaded(uploadedDocument)
    } catch (uploadError: unknown) {
      if (!controller.signal.aborted && isMountedRef.current) {
        setSubmissionError(
          getSafeApiErrorMessage(
            uploadError,
            'The document could not be uploaded. Check the selected file and try again.',
          ),
        )
      }
    } finally {
      if (activeUploadRef.current === controller) {
        activeUploadRef.current = null

        if (isMountedRef.current) {
          setIsUploading(false)
        }
      }
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(_, data) => {
        if (!data.open) {
          closeDialog()
        }
      }}
    >
      <DialogSurface className="document-upload-dialog">
        <form
          className="document-upload-dialog__form"
          aria-busy={isUploading}
          noValidate
          onSubmit={(event) => void handleSubmit(event)}
        >
          <DialogBody>
            <DialogTitle>Upload new file</DialogTitle>
            <DialogContent className="document-upload-dialog__content">
              <p className="document-upload-dialog__introduction">
                Select a document to add to the approval library. The server
                performs the final file validation before storing it.
              </p>

              {submissionError ? (
                <MessageBar intent="error" politeness="assertive">
                  <MessageBarBody>{submissionError}</MessageBarBody>
                </MessageBar>
              ) : null}

              <div className="document-upload-dialog__fields">
                <Field
                  label="File"
                  required
                  hint={`Allowed extensions: ${DOCUMENT_UPLOAD_ACCEPT.split(',').join(', ')}. Maximum size: ${formatFileSize(DOCUMENT_UPLOAD_MAX_FILE_SIZE_BYTES)}.`}
                  validationMessage={validationErrors.file ?? undefined}
                  validationState={validationErrors.file ? 'error' : 'none'}
                >
                  {(fieldControlProps) => (
                    <input
                      {...fieldControlProps}
                      className="document-upload-dialog__file-input"
                      ref={fileInputRef}
                      type="file"
                      name="File"
                      accept={DOCUMENT_UPLOAD_ACCEPT}
                      required
                      disabled={isUploading}
                      onChange={(event) =>
                        handleFileChange(
                          event.currentTarget.files?.[0] ?? null,
                        )
                      }
                    />
                  )}
                </Field>

                {selectedFile ? (
                  <div
                    className="document-upload-dialog__selection"
                    role="status"
                    aria-live="polite"
                  >
                    <span className="document-upload-dialog__selection-name">
                      {selectedFile.name}
                    </span>
                    <span>{formatFileSize(selectedFile.size)}</span>
                  </div>
                ) : null}

                <Field
                  label="Description"
                  hint={`Optional context to help people identify this document. ${description.length.toLocaleString()} of ${DOCUMENT_DESCRIPTION_MAX_LENGTH.toLocaleString()} characters used.`}
                  validationMessage={validationErrors.description ?? undefined}
                  validationState={
                    validationErrors.description ? 'error' : 'none'
                  }
                >
                  <Textarea
                    ref={descriptionInputRef}
                    value={description}
                    name="Description"
                    rows={4}
                    resize="vertical"
                    disabled={isUploading}
                    onChange={(event) =>
                      handleDescriptionChange(event.currentTarget.value)
                    }
                  />
                </Field>
              </div>
            </DialogContent>
            <DialogActions className="document-upload-dialog__actions">
              <Button
                type="button"
                appearance="secondary"
                disabled={isUploading}
                onClick={closeDialog}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                appearance="primary"
                icon={
                  isUploading ? (
                    <Spinner size="tiny" aria-hidden="true" />
                  ) : (
                    <ArrowUpload20Regular />
                  )
                }
                disabled={isUploading}
                aria-busy={isUploading}
              >
                {isUploading
                  ? 'Uploading'
                  : submissionError
                    ? 'Retry upload'
                    : 'Upload'}
              </Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  )
}
