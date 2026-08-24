import {
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Radio,
  RadioGroup,
  Spinner,
} from '@fluentui/react-components'
import {
  ArrowClockwise20Regular,
  PeopleSearch24Regular,
  Search20Regular,
} from '@fluentui/react-icons'
import { useEffect, useRef, useState } from 'react'
import { AdministrationResourceState } from '../../../components/administration/AdministrationResourceState'
import { useDirectoryUsers } from '../../../hooks/useDirectoryUsers'
import type {
  ApplicationUser,
  DirectoryUserSearchResult,
  RoleReference,
} from '../../../models'
import {
  ApiError,
  adminUserService,
  getSafeApiErrorMessage,
} from '../../../services'

interface AddApplicationUserDialogProps {
  readonly roles: readonly RoleReference[]
  readonly rolesLoading: boolean
  readonly rolesError: string | null
  readonly onRetryRoles: () => void
  readonly onClose: () => void
  readonly onAdded: (user: ApplicationUser) => void
}

const minimumQueryLength = 2
const maximumQueryLength = 100
const directoryPageSize = 10
const debounceMilliseconds = 350

function getDirectoryDisplayName(user: DirectoryUserSearchResult): string {
  return user.displayName?.trim() || user.accountName
}

export function AddApplicationUserDialog({
  roles,
  rolesLoading,
  rolesError,
  onRetryRoles,
  onClose,
  onAdded,
}: AddApplicationUserDialogProps) {
  const [queryInput, setQueryInput] = useState('')
  const [debouncedQuery, setDebouncedQuery] = useState('')
  const [page, setPage] = useState(1)
  const [selectedUser, setSelectedUser] =
    useState<DirectoryUserSearchResult | null>(null)
  const [selectedRoleIds, setSelectedRoleIds] = useState<readonly string[]>([])
  const [roleValidationError, setRoleValidationError] = useState<string | null>(
    null,
  )
  const [submissionError, setSubmissionError] = useState<string | null>(null)
  const [submissionConflict, setSubmissionConflict] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const activeRequestRef = useRef<AbortController | null>(null)
  const isMountedRef = useRef(true)
  const normalizedInput = queryInput.trim()
  const searchEnabled = debouncedQuery.length >= minimumQueryLength
  const isDebouncing =
    normalizedInput.length >= minimumQueryLength &&
    normalizedInput !== debouncedQuery
  const directory = useDirectoryUsers(
    debouncedQuery,
    page,
    directoryPageSize,
    searchEnabled,
  )
  const results = directory.data?.items ?? []

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedQuery(
        normalizedInput.length >= minimumQueryLength ? normalizedInput : '',
      )
    }, debounceMilliseconds)

    return () => window.clearTimeout(timeoutId)
  }, [normalizedInput])

  useEffect(() => {
    isMountedRef.current = true

    return () => {
      isMountedRef.current = false
      activeRequestRef.current?.abort()
      activeRequestRef.current = null
    }
  }, [])

  const handleQueryChange = (value: string) => {
    const nextValue = value.slice(0, maximumQueryLength)
    setQueryInput(nextValue)
    setPage(1)
    setSelectedUser(null)
    setSubmissionError(null)
    setSubmissionConflict(false)

    if (nextValue.trim().length < minimumQueryLength) {
      setDebouncedQuery('')
    }
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (activeRequestRef.current !== null) {
      return
    }

    if (selectedUser === null) {
      setSubmissionError('Select one directory user to add.')
      return
    }

    if (selectedRoleIds.length === 0) {
      setRoleValidationError('Select at least one active custom role.')
      return
    }

    const controller = new AbortController()
    activeRequestRef.current = controller
    setSubmissionError(null)
    setSubmissionConflict(false)
    setSubmitting(true)

    try {
      const applicationUser = await adminUserService.addUser(
        {
          selectionToken: selectedUser.selectionToken,
          roleIds: selectedRoleIds,
        },
        controller.signal,
      )

      if (!controller.signal.aborted && isMountedRef.current) {
        onAdded(applicationUser)
      }
    } catch (error: unknown) {
      if (!controller.signal.aborted && isMountedRef.current) {
        setSubmissionConflict(
          error instanceof ApiError &&
            (error.status === 409 ||
              error.code === 'directory.invalid_selection_token'),
        )
        setSubmissionError(
          getSafeApiErrorMessage(
            error,
            'The directory user could not be added. Please try again.',
          ),
        )
      }
    } finally {
      if (activeRequestRef.current === controller) {
        activeRequestRef.current = null

        if (isMountedRef.current) {
          setSubmitting(false)
        }
      }
    }
  }

  return (
    <Dialog
      open
      onOpenChange={(_, dialogData) => {
        if (!dialogData.open && !submitting) {
          onClose()
        }
      }}
    >
      <DialogSurface className="administration-dialog administration-dialog--wide">
        <form aria-busy={submitting} noValidate onSubmit={handleSubmit}>
          <DialogBody>
            <DialogTitle>Add user from Active Directory</DialogTitle>
            <DialogContent className="administration-dialog__content">
              <p className="administration-dialog__introduction">
                Search the configured directory and add access only to Independent
                Approval. This does not create or modify an Active Directory
                account.
              </p>

              {submissionError ? (
                <MessageBar intent="error" politeness="assertive">
                  <MessageBarBody>
                    {submissionError}
                    {submissionConflict ? (
                      <Button
                        type="button"
                        appearance="transparent"
                        icon={<ArrowClockwise20Regular />}
                        onClick={() => {
                          setSelectedUser(null)
                          setSubmissionError(null)
                          setSubmissionConflict(false)
                          directory.retry()
                        }}
                      >
                        Search again
                      </Button>
                    ) : null}
                  </MessageBarBody>
                </MessageBar>
              ) : null}

              <Field
                label="Directory search"
                hint={`Search by display name, account name, UPN, or email. Enter ${minimumQueryLength} to ${maximumQueryLength} characters.`}
                validationMessage={
                  normalizedInput.length > 0 &&
                  normalizedInput.length < minimumQueryLength
                    ? `Enter at least ${minimumQueryLength} characters.`
                    : undefined
                }
                validationState={
                  normalizedInput.length > 0 &&
                  normalizedInput.length < minimumQueryLength
                    ? 'error'
                    : 'none'
                }
              >
                <Input
                  autoFocus
                  value={queryInput}
                  contentBefore={<Search20Regular />}
                  maxLength={maximumQueryLength}
                  disabled={submitting}
                  placeholder="Start typing to search the directory"
                  onChange={(_, inputData) =>
                    handleQueryChange(inputData.value)
                  }
                />
              </Field>

              <section
                className="directory-search-results"
                aria-label="Directory search results"
                aria-busy={isDebouncing || directory.loading}
              >
                {normalizedInput.length < minimumQueryLength ? (
                  <div className="directory-search-prompt" role="status">
                    <PeopleSearch24Regular aria-hidden="true" />
                    <div>
                      <h3>Find a directory user</h3>
                      <p>
                        Enter at least {minimumQueryLength} characters to begin a
                        bounded directory search.
                      </p>
                    </div>
                  </div>
                ) : isDebouncing ? (
                  <div className="directory-search-prompt" role="status">
                    <Spinner size="small" label="Preparing directory search" />
                  </div>
                ) : (
                  <AdministrationResourceState
                    loading={directory.loading}
                    error={directory.error}
                    empty={results.length === 0}
                    loadingLabel="Searching Active Directory"
                    emptyTitle="No directory users found"
                    emptyDescription="Try a different display name, account name, UPN, or email."
                    onRetry={directory.retry}
                  >
                    <>
                      <RadioGroup
                        value={selectedUser?.accountName ?? ''}
                        aria-label="Select a directory user"
                        onChange={(_, radioData) => {
                          const nextUser = results.find(
                            (result) => result.accountName === radioData.value,
                          )
                          setSelectedUser(nextUser ?? null)
                          setSubmissionError(null)
                          setSubmissionConflict(false)
                        }}
                      >
                        {results.map((result) => (
                          <div
                            className="directory-user-option"
                            key={result.accountName}
                          >
                            <Radio
                              value={result.accountName}
                              label={getDirectoryDisplayName(result)}
                              disabled={submitting}
                            />
                            <span>{result.accountName}</span>
                            <span>
                              {result.email ??
                                result.userPrincipalName ??
                                'No email or UPN available'}
                            </span>
                          </div>
                        ))}
                      </RadioGroup>

                      <div className="administration-pagination">
                        <p aria-live="polite">Directory results page {page}</p>
                        <div>
                          <Button
                            type="button"
                            appearance="subtle"
                            disabled={page <= 1 || directory.loading}
                            onClick={() => {
                              setSelectedUser(null)
                              setSubmissionError(null)
                              setSubmissionConflict(false)
                              setPage((currentPage) => currentPage - 1)
                            }}
                          >
                            Previous
                          </Button>
                          <Button
                            type="button"
                            appearance="subtle"
                            disabled={
                              directory.data?.hasMore !== true || directory.loading
                            }
                            onClick={() => {
                              setSelectedUser(null)
                              setSubmissionError(null)
                              setSubmissionConflict(false)
                              setPage((currentPage) => currentPage + 1)
                            }}
                          >
                            Next
                          </Button>
                        </div>
                      </div>
                    </>
                  </AdministrationResourceState>
                )}
              </section>

              <section className="administration-dialog__section">
                <h3>Initial custom roles</h3>
                <p>
                  Assign one or more active roles. System Administrator access is
                  not assignable as a custom role.
                </p>
                {rolesLoading ? (
                  <Spinner size="small" label="Loading active roles" />
                ) : rolesError ? (
                  <MessageBar intent="error">
                    <MessageBarBody>
                      {rolesError}
                      <Button
                        type="button"
                        appearance="transparent"
                        onClick={onRetryRoles}
                      >
                        Retry
                      </Button>
                    </MessageBarBody>
                  </MessageBar>
                ) : roles.length === 0 ? (
                  <p className="administration-dialog__empty-copy">
                    Create an active custom role before adding application users.
                  </p>
                ) : (
                  <Field
                    validationMessage={roleValidationError ?? undefined}
                    validationState={roleValidationError ? 'error' : 'none'}
                  >
                    <div
                      className="administration-checkbox-list"
                      role="group"
                      aria-label="Initial custom roles"
                    >
                      {roles.map((role) => (
                        <Checkbox
                          key={role.id}
                          checked={selectedRoleIds.includes(role.id)}
                          disabled={submitting}
                          label={`${role.nameEnglish} (${role.code})`}
                          onChange={(_, checkboxData) => {
                            setRoleValidationError(null)
                            setSelectedRoleIds((currentIds) =>
                              checkboxData.checked === true
                                ? currentIds.includes(role.id)
                                  ? currentIds
                                  : [...currentIds, role.id]
                                : currentIds.filter(
                                    (roleId) => roleId !== role.id,
                                  ),
                            )
                          }}
                        />
                      ))}
                    </div>
                  </Field>
                )}
              </section>
            </DialogContent>
            <DialogActions>
              <Button
                type="button"
                appearance="secondary"
                disabled={submitting}
                onClick={onClose}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                appearance="primary"
                disabled={
                  submitting ||
                  selectedUser === null ||
                  selectedRoleIds.length === 0 ||
                  rolesLoading ||
                  rolesError !== null ||
                  roles.length === 0
                }
                icon={submitting ? <Spinner size="tiny" /> : undefined}
              >
                {submitting ? 'Adding user' : 'Add application user'}
              </Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  )
}
