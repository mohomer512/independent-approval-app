import {
  Button,
  Checkbox,
  Input,
} from '@fluentui/react-components'
import {
  Add20Regular,
  ArrowClockwise20Regular,
  ArrowLeft20Regular,
  Edit20Regular,
  Eye20Regular,
  Search20Regular,
} from '@fluentui/react-icons'
import { useRef, useState } from 'react'
import { useNavigate } from 'react-router'
import { AdministrationResourceState } from '../../../components/administration/AdministrationResourceState'
import { PageHeader, SectionPanel, StatusBadge } from '../../../components/common'
import { useAdminRequestTypes } from '../../../hooks/useAdminRequestTypes'
import type { RequestTypeListItem } from '../../../models'
import { CreateRequestTypeDialog } from './CreateRequestTypeDialog'
import { RequestTypeDesignerDialog } from './RequestTypeDesignerDialog'
import { getLifecycleLabel } from './requestTypeUi'

const pageSize = 25

interface DesignerSelection {
  readonly requestTypeId: string
  readonly preferredVersionId: string | null
}

function getStatus(requestType: RequestTypeListItem): string {
  if (requestType.isArchived) {
    return 'Archived'
  }

  return requestType.displayVersion
    ? getLifecycleLabel(requestType.displayVersion.lifecycle)
    : 'Unavailable'
}

export function RequestTypesPage() {
  const navigate = useNavigate()
  const [searchInput, setSearchInput] = useState('')
  const [appliedSearch, setAppliedSearch] = useState('')
  const [includeArchived, setIncludeArchived] = useState(false)
  const [page, setPage] = useState(1)
  const [creating, setCreating] = useState(false)
  const [designerSelection, setDesignerSelection] =
    useState<DesignerSelection | null>(null)
  const createTriggerRef = useRef<HTMLButtonElement | null>(null)
  const designerTriggerRef = useRef<HTMLButtonElement | null>(null)
  const requestTypes = useAdminRequestTypes(
    appliedSearch,
    includeArchived,
    page,
    pageSize,
  )
  const items = requestTypes.data?.items ?? []
  const totalCount = requestTypes.data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))

  const closeCreate = () => {
    setCreating(false)
    window.requestAnimationFrame(() => createTriggerRef.current?.focus())
  }

  const closeDesigner = () => {
    setDesignerSelection(null)
    window.requestAnimationFrame(() => designerTriggerRef.current?.focus())
  }

  return (
    <div className="administration-list-page page-stack">
      <PageHeader
        eyebrow="Administration"
        title="Request types"
        description="Design bilingual, versioned request forms while protected system fields remain under application control."
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
                createTriggerRef.current = event.currentTarget
                setCreating(true)
              }}
            >
              Create request type
            </Button>
          </div>
        }
      />

      <SectionPanel
        title="Versioned definitions"
        description="Drafts are editable. Published and in-use versions remain stable; clone a published version to continue designing."
        action={
          <Button
            type="button"
            appearance="outline"
            icon={<ArrowClockwise20Regular />}
            disabled={requestTypes.loading}
            onClick={requestTypes.retry}
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
          }}
        >
          <Input
            value={searchInput}
            maxLength={200}
            contentBefore={<Search20Regular />}
            aria-label="Search request types"
            placeholder="Search code or English/Arabic name"
            onChange={(_, data) => setSearchInput(data.value)}
          />
          <Button type="submit" appearance="primary">
            Search
          </Button>
          <Checkbox
            checked={includeArchived}
            label="Include archived request types"
            onChange={(_, data) => {
              setPage(1)
              setIncludeArchived(data.checked === true)
            }}
          />
        </form>

        <AdministrationResourceState
          loading={requestTypes.loading}
          error={requestTypes.error}
          empty={items.length === 0}
          loadingLabel="Loading request types"
          emptyTitle="No request types found"
          emptyDescription={
            appliedSearch
              ? 'No request types match the current search and filters.'
              : 'Create a draft request type to begin designing its form.'
          }
          onRetry={requestTypes.retry}
        >
          <>
            <div className="data-table-scroll" tabIndex={0}>
              <table className="data-table administration-table request-types-table">
                <caption className="visually-hidden">
                  Versioned request type definitions
                </caption>
                <thead>
                  <tr>
                    <th scope="col">Request type</th>
                    <th scope="col">Code</th>
                    <th scope="col">Displayed version</th>
                    <th scope="col">Fields</th>
                    <th scope="col">Versions</th>
                    <th scope="col">Status</th>
                    <th scope="col">
                      <span className="visually-hidden">Actions</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((requestType) => {
                    const displayVersion = requestType.displayVersion
                    const preferredVersionId =
                      requestType.draftVersionId ??
                      requestType.latestPublishedVersionId ??
                      displayVersion?.id ??
                      null
                    const editable =
                      !requestType.isArchived && requestType.draftVersionId !== null

                    return (
                      <tr key={requestType.id}>
                        <td>
                          <span className="data-table__primary">
                            {displayVersion?.nameEnglish ?? requestType.code}
                          </span>
                          {displayVersion ? (
                            <span
                              className="data-table__secondary"
                              dir="rtl"
                              lang="ar"
                            >
                              {displayVersion.nameArabic}
                            </span>
                          ) : null}
                        </td>
                        <td>
                          <code>{requestType.code}</code>
                        </td>
                        <td>
                          {displayVersion
                            ? `Version ${displayVersion.versionNumber}`
                            : 'None'}
                          {requestType.draftVersionId ? (
                            <span className="data-table__secondary">
                              Draft available
                            </span>
                          ) : null}
                        </td>
                        <td>{displayVersion?.fieldCount ?? 0}</td>
                        <td>{requestType.versionCount}</td>
                        <td>
                          <StatusBadge status={getStatus(requestType)} />
                        </td>
                        <td className="data-table__action">
                          <Button
                            type="button"
                            appearance="subtle"
                            size="small"
                            icon={editable ? <Edit20Regular /> : <Eye20Regular />}
                            aria-label={`${editable ? 'Design' : 'View'} ${displayVersion?.nameEnglish ?? requestType.code}`}
                            onClick={(event) => {
                              designerTriggerRef.current = event.currentTarget
                              setDesignerSelection({
                                requestTypeId: requestType.id,
                                preferredVersionId,
                              })
                            }}
                          >
                            {editable ? 'Design' : 'View'}
                          </Button>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>

            <div className="administration-pagination">
              <p aria-live="polite">
                Page {page} of {totalPages} · {totalCount} request types
              </p>
              <div>
                <Button
                  type="button"
                  appearance="subtle"
                  disabled={page <= 1}
                  onClick={() => setPage((current) => current - 1)}
                >
                  Previous
                </Button>
                <Button
                  type="button"
                  appearance="subtle"
                  disabled={page >= totalPages}
                  onClick={() => setPage((current) => current + 1)}
                >
                  Next
                </Button>
              </div>
            </div>
          </>
        </AdministrationResourceState>
      </SectionPanel>

      {creating ? (
        <CreateRequestTypeDialog
          onClose={closeCreate}
          onSaved={(created) => {
            const draft = created.versions.find(
              (version) => version.lifecycle === 'draft',
            )
            setCreating(false)
            designerTriggerRef.current = createTriggerRef.current
            setDesignerSelection({
              requestTypeId: created.id,
              preferredVersionId: draft?.id ?? null,
            })
            requestTypes.retry()
          }}
        />
      ) : null}

      {designerSelection ? (
        <RequestTypeDesignerDialog
          key={designerSelection.requestTypeId}
          requestTypeId={designerSelection.requestTypeId}
          preferredVersionId={designerSelection.preferredVersionId}
          onClose={closeDesigner}
          onChanged={requestTypes.retry}
        />
      ) : null}
    </div>
  )
}
