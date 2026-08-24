import {
  Button,
  Checkbox,
  Input,
  MessageBar,
  MessageBarBody,
} from '@fluentui/react-components'
import {
  Add20Regular,
  ArrowClockwise20Regular,
  ArrowLeft20Regular,
  Eye20Regular,
  Search20Regular,
} from '@fluentui/react-icons'
import { useRef, useState } from 'react'
import { useNavigate } from 'react-router'
import { AdministrationResourceState } from '../../../components/administration/AdministrationResourceState'
import {
  formatDateTime,
  PageHeader,
  SectionPanel,
  StatusBadge,
} from '../../../components/common'
import { useAdminRoles } from '../../../hooks/useAdminRoles'
import { useAdminUsers } from '../../../hooks/useAdminUsers'
import type { ApplicationUser } from '../../../models'
import { AddApplicationUserDialog } from './AddApplicationUserDialog'
import { UserDetailsDialog } from './UserDetailsDialog'

const pageSize = 25

function getUserStatus(user: ApplicationUser): string {
  if (user.isRemoved) {
    return 'Removed'
  }

  if (user.isLocked) {
    return 'Locked'
  }

  return user.isActive ? 'Active' : 'Inactive'
}

export function UsersPage() {
  const navigate = useNavigate()
  const [searchInput, setSearchInput] = useState('')
  const [appliedSearch, setAppliedSearch] = useState('')
  const [includeRemoved, setIncludeRemoved] = useState(false)
  const [page, setPage] = useState(1)
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null)
  const [addDialogOpen, setAddDialogOpen] = useState(false)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const detailTriggerRef = useRef<HTMLButtonElement | null>(null)
  const addTriggerRef = useRef<HTMLButtonElement | null>(null)
  const { data, loading, error, retry } = useAdminUsers(
    appliedSearch,
    includeRemoved,
    page,
    pageSize,
  )
  const roles = useAdminRoles('', false, 1, 100)
  const users = data?.items ?? []
  const totalPages = Math.max(
    1,
    Math.ceil((data?.totalCount ?? 0) / pageSize),
  )

  const closeDetails = () => {
    setSelectedUserId(null)
    window.requestAnimationFrame(() => detailTriggerRef.current?.focus())
  }

  const closeAddDialog = () => {
    setAddDialogOpen(false)
    window.requestAnimationFrame(() => addTriggerRef.current?.focus())
  }

  return (
    <div className="administration-list-page page-stack">
      <PageHeader
        eyebrow="Administration"
        title="Users"
        description="Manage application access, status, and custom role assignments. Active Directory accounts are not modified here."
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
                addTriggerRef.current = event.currentTarget
                setSuccessMessage(null)
                setAddDialogOpen(true)
              }}
            >
              Add from directory
            </Button>
          </div>
        }
      />

      {successMessage ? (
        <MessageBar intent="success" politeness="polite">
          <MessageBarBody>{successMessage}</MessageBarBody>
        </MessageBar>
      ) : null}

      <SectionPanel
        title="Application users"
        description="Search current and removed application-access records."
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
        <form
          className="administration-toolbar"
          role="search"
          onSubmit={(event) => {
            event.preventDefault()
            setPage(1)
            setAppliedSearch(searchInput.trim())
            retry()
          }}
        >
          <Input
            value={searchInput}
            contentBefore={<Search20Regular />}
            aria-label="Search application users"
            placeholder="Search name, account, UPN, or email"
            onChange={(_, inputData) => setSearchInput(inputData.value)}
          />
          <Button type="submit" appearance="primary">
            Search
          </Button>
          <Checkbox
            checked={includeRemoved}
            label="Include removed access"
            onChange={(_, checkboxData) => {
              setPage(1)
              setIncludeRemoved(checkboxData.checked === true)
              retry()
            }}
          />
        </form>

        <AdministrationResourceState
          loading={loading}
          error={error}
          empty={users.length === 0}
          loadingLabel="Loading application users"
          emptyTitle="No application users found"
          emptyDescription={
            appliedSearch
              ? 'No users match the current search and filters.'
              : 'Application users will appear here after they are added.'
          }
          onRetry={retry}
        >
          <>
            <div className="data-table-scroll" tabIndex={0}>
              <table className="data-table administration-table">
                <caption className="visually-hidden">
                  Application users and access status
                </caption>
                <thead>
                  <tr>
                    <th scope="col">User</th>
                    <th scope="col">Account</th>
                    <th scope="col">Roles</th>
                    <th scope="col">Status</th>
                    <th scope="col">Last access</th>
                    <th scope="col">
                      <span className="visually-hidden">Actions</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((user) => (
                    <tr key={user.id}>
                      <td>
                        <span className="data-table__primary">
                          {user.displayName}
                        </span>
                        <span className="data-table__secondary">
                          {user.email ?? 'No email available'}
                        </span>
                      </td>
                      <td>{user.accountName}</td>
                      <td>
                        {user.roles.length > 0
                          ? user.roles.map((role) => role.nameEnglish).join(', ')
                          : 'No custom roles'}
                      </td>
                      <td>
                        <StatusBadge status={getUserStatus(user)} />
                      </td>
                      <td>
                        {user.lastSuccessfulAccessAtUtc ? (
                          <time dateTime={user.lastSuccessfulAccessAtUtc}>
                            {formatDateTime(user.lastSuccessfulAccessAtUtc)}
                          </time>
                        ) : (
                          'Never'
                        )}
                      </td>
                      <td className="data-table__action">
                        <Button
                          type="button"
                          appearance="subtle"
                          size="small"
                          icon={<Eye20Regular />}
                          aria-label={`View ${user.displayName}`}
                          onClick={(event) => {
                            detailTriggerRef.current = event.currentTarget
                            setSelectedUserId(user.id)
                          }}
                        >
                          View
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="administration-pagination">
              <p aria-live="polite">
                Page {page} of {totalPages} · {data?.totalCount ?? 0} users
              </p>
              <div>
                <Button
                  type="button"
                  appearance="subtle"
                  disabled={page <= 1}
                  onClick={() => {
                    setPage((currentPage) => currentPage - 1)
                    retry()
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
                    retry()
                  }}
                >
                  Next
                </Button>
              </div>
            </div>
          </>
        </AdministrationResourceState>
      </SectionPanel>

      {selectedUserId ? (
        <UserDetailsDialog
          userId={selectedUserId}
          availableRoles={roles.data?.items ?? []}
          rolesLoading={roles.loading}
          rolesError={roles.error}
          onRetryRoles={roles.retry}
          onClose={closeDetails}
          onChanged={retry}
        />
      ) : null}

      {addDialogOpen ? (
        <AddApplicationUserDialog
          roles={(roles.data?.items ?? []).filter(
            (role) => role.isActive && !role.isArchived,
          )}
          rolesLoading={roles.loading}
          rolesError={roles.error}
          onRetryRoles={roles.retry}
          onClose={closeAddDialog}
          onAdded={(user) => {
            setSuccessMessage(
              `${user.displayName} was added to Independent Approval.`,
            )
            closeAddDialog()
            retry()
          }}
        />
      ) : null}
    </div>
  )
}
