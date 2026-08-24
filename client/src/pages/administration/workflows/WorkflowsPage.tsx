import { Button, Checkbox, Input } from '@fluentui/react-components'
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
import { useAdminWorkflows } from '../../../hooks/useAdminWorkflows'
import { useWorkflowOptions } from '../../../hooks/useWorkflowOptions'
import type { WorkflowListItem } from '../../../models'
import { CreateWorkflowDialog } from './CreateWorkflowDialog'
import { WorkflowDesignerDialog } from './WorkflowDesignerDialog'
import { getWorkflowLifecycleLabel } from './workflowUi'

const pageSize = 25

interface DesignerSelection {
  readonly workflowId: string
  readonly preferredVersionId: string | null
}

function getStatus(workflow: WorkflowListItem): string {
  if (workflow.isArchived || workflow.displayVersion?.lifecycle === 'archived') {
    return 'Archived'
  }
  if (workflow.displayVersion?.isInUse) return 'In use'
  return workflow.displayVersion
    ? getWorkflowLifecycleLabel(workflow.displayVersion.lifecycle)
    : 'Unavailable'
}

export function WorkflowsPage() {
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
  const workflows = useAdminWorkflows(
    appliedSearch,
    includeArchived,
    page,
    pageSize,
  )
  const options = useWorkflowOptions()
  const items = workflows.data?.items ?? []
  const totalCount = workflows.data?.totalCount ?? 0
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
        title="Approval workflows"
        description="Build versioned approval graphs with role assignments, transitions, loops, and step-level field permissions."
        actions={
          <div className="administration-page-actions">
            <Button type="button" appearance="subtle" icon={<ArrowLeft20Regular />} onClick={() => navigate('/administration')}>
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
              Create workflow
            </Button>
          </div>
        }
      />

      <SectionPanel
        title="Versioned workflow definitions"
        description="Only drafts are editable. Published and in-use graphs remain stable; clone them to begin a new draft."
        action={
          <Button type="button" appearance="outline" icon={<ArrowClockwise20Regular />} disabled={workflows.loading} onClick={workflows.retry}>
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
          <Input value={searchInput} maxLength={200} contentBefore={<Search20Regular />} aria-label="Search workflows" placeholder="Search code or English/Arabic name" onChange={(_, data) => setSearchInput(data.value)} />
          <Button type="submit" appearance="primary">Search</Button>
          <Checkbox checked={includeArchived} label="Include archived workflows" onChange={(_, data) => { setPage(1); setIncludeArchived(data.checked === true) }} />
        </form>

        <AdministrationResourceState
          loading={workflows.loading}
          error={workflows.error}
          empty={items.length === 0}
          loadingLabel="Loading approval workflows"
          emptyTitle="No workflows found"
          emptyDescription={appliedSearch ? 'No workflows match the current search and filters.' : 'Create a draft workflow against a published request-type version.'}
          onRetry={workflows.retry}
        >
          <>
            <div className="data-table-scroll" tabIndex={0}>
              <table className="data-table administration-table workflows-table">
                <caption className="visually-hidden">Versioned approval workflow definitions</caption>
                <thead>
                  <tr>
                    <th scope="col">Workflow</th>
                    <th scope="col">Code</th>
                    <th scope="col">Request type</th>
                    <th scope="col">Graph</th>
                    <th scope="col">Versions</th>
                    <th scope="col">Status</th>
                    <th scope="col"><span className="visually-hidden">Actions</span></th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((workflow) => {
                    const version = workflow.displayVersion
                    const preferredVersionId = workflow.draftVersionId ?? workflow.latestPublishedVersionId ?? version?.id ?? null
                    const editable = !workflow.isArchived && workflow.draftVersionId !== null
                    return (
                      <tr key={workflow.id}>
                        <td>
                          <span className="data-table__primary">{version?.nameEnglish ?? workflow.code}</span>
                          {version ? <span className="data-table__secondary" dir="rtl" lang="ar">{version.nameArabic}</span> : null}
                        </td>
                        <td><code>{workflow.code}</code></td>
                        <td>
                          {version ? `${version.requestTypeCode} · v${version.requestTypeVersionNumber}` : 'Unavailable'}
                        </td>
                        <td>
                          {version ? `${version.activeStepCount} steps · ${version.activeTransitionCount} transitions` : '—'}
                        </td>
                        <td>{workflow.versionCount}</td>
                        <td>
                          <div className="workflow-status-stack">
                            <StatusBadge status={getStatus(workflow)} />
                            {(workflow.isArchived || version?.lifecycle === 'archived') && version?.isInUse ? <StatusBadge status="In use" /> : null}
                          </div>
                        </td>
                        <td className="data-table__action">
                          <Button
                            type="button"
                            appearance="subtle"
                            size="small"
                            icon={editable ? <Edit20Regular /> : <Eye20Regular />}
                            aria-label={`${editable ? 'Design' : 'View'} ${version?.nameEnglish ?? workflow.code}`}
                            onClick={(event) => {
                              designerTriggerRef.current = event.currentTarget
                              setDesignerSelection({ workflowId: workflow.id, preferredVersionId })
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
              <p aria-live="polite">Page {page} of {totalPages} · {totalCount} workflows</p>
              <div>
                <Button type="button" appearance="subtle" disabled={page <= 1} onClick={() => setPage((current) => current - 1)}>Previous</Button>
                <Button type="button" appearance="subtle" disabled={page >= totalPages} onClick={() => setPage((current) => current + 1)}>Next</Button>
              </div>
            </div>
          </>
        </AdministrationResourceState>
      </SectionPanel>

      {creating ? (
        <CreateWorkflowDialog
          requestTypeVersions={options.data?.requestTypeVersions ?? []}
          optionsLoading={options.loading}
          optionsError={options.error}
          onRetryOptions={options.retry}
          onClose={closeCreate}
          onSaved={(created) => {
            const draft = created.versions.find((version) => version.lifecycle === 'draft')
            setCreating(false)
            designerTriggerRef.current = createTriggerRef.current
            setDesignerSelection({ workflowId: created.id, preferredVersionId: draft?.id ?? null })
            workflows.retry()
          }}
        />
      ) : null}
      {designerSelection ? (
        <WorkflowDesignerDialog
          key={designerSelection.workflowId}
          workflowId={designerSelection.workflowId}
          preferredVersionId={designerSelection.preferredVersionId}
          options={options.data}
          optionsLoading={options.loading}
          optionsError={options.error}
          onRetryOptions={options.retry}
          onClose={closeDesigner}
          onChanged={workflows.retry}
        />
      ) : null}
    </div>
  )
}
