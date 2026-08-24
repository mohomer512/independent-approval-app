import {
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Input,
  MessageBar,
  MessageBarBody,
} from '@fluentui/react-components'
import {
  Add20Regular,
  Archive20Regular,
  ArrowClockwise20Regular,
  ArrowLeft20Regular,
  Edit20Regular,
  Eye20Regular,
  Search20Regular,
} from '@fluentui/react-icons'
import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router'
import { AdministrationResourceState } from '../../../components/administration/AdministrationResourceState'
import { PageHeader, SectionPanel, StatusBadge } from '../../../components/common'
import { useAdminPermissions } from '../../../hooks/useAdminPermissions'
import { useAdminRoles } from '../../../hooks/useAdminRoles'
import type { ApplicationRole } from '../../../models'
import {
  ApiError,
  adminRoleService,
  getSafeApiErrorMessage,
} from '../../../services'
import { RoleEditorDialog } from './RoleEditorDialog'

const pageSize = 25

function getRoleStatus(role: ApplicationRole): string {
  if (role.isArchived) {
    return 'Archived'
  }

  return role.isActive ? 'Active' : 'Inactive'
}

export function RolesPage() {
  const navigate = useNavigate()
  const [searchInput, setSearchInput] = useState('')
  const [appliedSearch, setAppliedSearch] = useState('')
  const [includeArchived, setIncludeArchived] = useState(false)
  const [page, setPage] = useState(1)
  const [creatingRole, setCreatingRole] = useState(false)
  const [selectedRole, setSelectedRole] = useState<ApplicationRole | null>(null)
  const [archiveRole, setArchiveRole] = useState<ApplicationRole | null>(null)
  const [archiveError, setArchiveError] = useState<string | null>(null)
  const [archiveConflict, setArchiveConflict] = useState(false)
  const [archiving, setArchiving] = useState(false)
  const editorTriggerRef = useRef<HTMLButtonElement | null>(null)
  const archiveTriggerRef = useRef<HTMLButtonElement | null>(null)
  const archiveRequestRef = useRef<AbortController | null>(null)
  const isMountedRef = useRef(true)
  const roles = useAdminRoles(
    appliedSearch,
    includeArchived,
    page,
    pageSize,
  )
  const permissions = useAdminPermissions()
  const roleItems = roles.data?.items ?? []
  const totalPages = Math.max(
    1,
    Math.ceil((roles.data?.totalCount ?? 0) / pageSize),
  )

  useEffect(() => {
    isMountedRef.current = true

    return () => {
      isMountedRef.current = false
      archiveRequestRef.current?.abort()
      archiveRequestRef.current = null
    }
  }, [])

  const closeEditor = () => {
    setCreatingRole(false)
    setSelectedRole(null)
    window.requestAnimationFrame(() => editorTriggerRef.current?.focus())
  }

  const closeArchiveDialog = () => {
    if (archiving) {
      return
    }

    setArchiveRole(null)
    setArchiveError(null)
    setArchiveConflict(false)
    window.requestAnimationFrame(() => archiveTriggerRef.current?.focus())
  }

  const handleArchive = async () => {
    if (!archiveRole || archiveRequestRef.current !== null) {
      return
    }

    const controller = new AbortController()
    archiveRequestRef.current = controller
    setArchiving(true)
    setArchiveError(null)
    setArchiveConflict(false)

    try {
      await adminRoleService.archiveRole(
        archiveRole.id,
        { rowVersion: archiveRole.rowVersion },
        controller.signal,
      )

      if (!controller.signal.aborted && isMountedRef.current) {
        setArchiveRole(null)
        roles.retry()
        window.requestAnimationFrame(() => archiveTriggerRef.current?.focus())
      }
    } catch (error: unknown) {
      if (!controller.signal.aborted && isMountedRef.current) {
        setArchiveConflict(error instanceof ApiError && error.status === 409)
        setArchiveError(
          getSafeApiErrorMessage(
            error,
            'The role could not be archived. Please try again.',
          ),
        )
      }
    } finally {
      if (archiveRequestRef.current === controller) {
        archiveRequestRef.current = null

        if (isMountedRef.current) {
          setArchiving(false)
        }
      }
    }
  }

  return (
    <div className="administration-list-page page-stack">
      <PageHeader
        eyebrow="Administration"
        title="Roles and permissions"
        description="Create custom roles and assign only permissions from the fixed application catalogue."
        actions={
          <div className="administration-page-actions">
            <Button
              type="button"
              appearance="subtle"
              icon={<ArrowLeft20Regular />}
              onClick={() => navigate('/administration')}
            >
              Administration
            </Button>
            <Button
              type="button"
              appearance="primary"
              icon={<Add20Regular />}
              onClick={(event) => {
                editorTriggerRef.current = event.currentTarget
                setCreatingRole(true)
              }}
            >
              Create role
            </Button>
          </div>
        }
      />

      <SectionPanel
        title="Custom roles"
        description="Role codes are permanent. Referenced roles are archived instead of deleted."
        action={
          <Button
            type="button"
            appearance="outline"
            icon={<ArrowClockwise20Regular />}
            disabled={roles.loading}
            onClick={roles.retry}
          >
            Refresh
          </Button>
        }
      >
        <form
          className="administration-toolbar"
          role="search"
          onSubmit={(event) => {
            event.preventDefault()
            setPage(1)
            setAppliedSearch(searchInput.trim())
            roles.retry()
          }}
        >
          <Input
            value={searchInput}
            contentBefore={<Search20Regular />}
            aria-label="Search custom roles"
            placeholder="Search code or English/Arabic name"
            onChange={(_, inputData) => setSearchInput(inputData.value)}
          />
          <Button type="submit" appearance="primary">
            Search
          </Button>
          <Checkbox
            checked={includeArchived}
            label="Include archived roles"
            onChange={(_, checkboxData) => {
              setPage(1)
              setIncludeArchived(checkboxData.checked === true)
              roles.retry()
            }}
          />
        </form>

        <AdministrationResourceState
          loading={roles.loading}
          error={roles.error}
          empty={roleItems.length === 0}
          loadingLabel="Loading custom roles"
          emptyTitle="No custom roles found"
          emptyDescription={
            appliedSearch
              ? 'No roles match the current search and filters.'
              : 'Create a custom role to begin assigning catalogue permissions.'
          }
          onRetry={roles.retry}
        >
          <>
            <div className="data-table-scroll" tabIndex={0}>
              <table className="data-table administration-table">
                <caption className="visually-hidden">
                  Custom application roles and permissions
                </caption>
                <thead>
                  <tr>
                    <th scope="col">Role</th>
                    <th scope="col">Code</th>
                    <th scope="col">Permissions</th>
                    <th scope="col">Users</th>
                    <th scope="col">Status</th>
                    <th scope="col">
                      <span className="visually-hidden">Actions</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {roleItems.map((role) => (
                    <tr key={role.id}>
                      <td>
                        <span className="data-table__primary">
                          {role.nameEnglish}
                        </span>
                        <span
                          className="data-table__secondary"
                          dir="rtl"
                          lang="ar"
                        >
                          {role.nameArabic}
                        </span>
                      </td>
                      <td>
                        <code>{role.code}</code>
                      </td>
                      <td>{role.permissionCodes.length}</td>
                      <td>{role.userCount}</td>
                      <td>
                        <StatusBadge status={getRoleStatus(role)} />
                      </td>
                      <td className="data-table__action">
                        <div className="administration-table__actions">
                          <Button
                            type="button"
                            appearance="subtle"
                            size="small"
                            icon={
                              role.isArchived ? (
                                <Eye20Regular />
                              ) : (
                                <Edit20Regular />
                              )
                            }
                            aria-label={`${role.isArchived ? 'View' : 'Edit'} ${role.nameEnglish}`}
                            onClick={(event) => {
                              editorTriggerRef.current = event.currentTarget
                              setSelectedRole(role)
                            }}
                          >
                            {role.isArchived ? 'View' : 'Edit'}
                          </Button>
                          {!role.isArchived ? (
                            <Button
                              type="button"
                              appearance="subtle"
                              size="small"
                              icon={<Archive20Regular />}
                              aria-label={`Archive ${role.nameEnglish}`}
                              onClick={(event) => {
                                archiveTriggerRef.current = event.currentTarget
                                setArchiveError(null)
                                setArchiveConflict(false)
                                setArchiveRole(role)
                              }}
                            >
                              Archive
                            </Button>
                          ) : null}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="administration-pagination">
              <p aria-live="polite">
                Page {page} of {totalPages} · {roles.data?.totalCount ?? 0} roles
              </p>
              <div>
                <Button
                  type="button"
                  appearance="subtle"
                  disabled={page <= 1}
                  onClick={() => {
                    setPage((currentPage) => currentPage - 1)
                    roles.retry()
                  }}
                >
                  Previous
                </Button>
                <Button
                  type="button"
                  appearance="subtle"
                  disabled={page >= totalPages}
                  onClick={() => {
                    setPage((currentPage) => currentPage + 1)
                    roles.retry()
                  }}
                >
                  Next
                </Button>
              </div>
            </div>
          </>
        </AdministrationResourceState>
      </SectionPanel>

      {creatingRole || selectedRole ? (
        <RoleEditorDialog
          key={selectedRole?.id ?? 'new-role'}
          role={selectedRole}
          permissions={permissions.data ?? []}
          permissionsLoading={permissions.loading}
          permissionsError={permissions.error}
          onRetryPermissions={permissions.retry}
          onClose={closeEditor}
          onSaved={() => {
            closeEditor()
            roles.retry()
          }}
        />
      ) : null}

      <Dialog
        open={archiveRole !== null}
        onOpenChange={(_, dialogData) => {
          if (!dialogData.open) {
            closeArchiveDialog()
          }
        }}
      >
        <DialogSurface className="administration-dialog">
          <DialogBody>
            <DialogTitle>Archive role?</DialogTitle>
            <DialogContent className="administration-dialog__content">
              <p>
                {archiveRole?.nameEnglish} will no longer be available for new
                assignments. Existing historical references will be retained.
              </p>
              {archiveError ? (
                <MessageBar intent="error" politeness="assertive">
                  <MessageBarBody>
                    {archiveError}
                    {archiveConflict
                      ? ' Reload the role list before trying again.'
                      : ''}
                  </MessageBarBody>
                </MessageBar>
              ) : null}
            </DialogContent>
            <DialogActions>
              <Button
                type="button"
                appearance="secondary"
                disabled={archiving}
                onClick={closeArchiveDialog}
              >
                Cancel
              </Button>
              <Button
                type="button"
                appearance="primary"
                disabled={archiving}
                onClick={() => void handleArchive()}
              >
                {archiving ? 'Archiving' : 'Archive role'}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  )
}
