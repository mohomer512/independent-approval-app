import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
} from '@fluentui/react-components'
import { ArrowClockwise20Regular } from '@fluentui/react-icons'
import { useMemo, useState } from 'react'
import { AdministrationResourceState } from '../../../components/administration/AdministrationResourceState'
import { StatusBadge } from '../../../components/common'
import { useAdminWorkflow } from '../../../hooks/useAdminWorkflow'
import type { WorkflowOptions, WorkflowVersion } from '../../../models'
import { WorkflowVersionDesigner } from './WorkflowVersionDesigner'
import { getWorkflowLifecycleLabel } from './workflowUi'

interface WorkflowDesignerDialogProps {
  readonly workflowId: string
  readonly preferredVersionId: string | null
  readonly options: WorkflowOptions | null
  readonly optionsLoading: boolean
  readonly optionsError: string | null
  readonly onRetryOptions: () => void
  readonly onClose: () => void
  readonly onChanged: () => void
}

function chooseVersion(
  versions: readonly WorkflowVersion[],
  preferredVersionId: string | null,
): WorkflowVersion | null {
  return (
    versions.find((version) => version.id === preferredVersionId) ??
    versions.find((version) => version.lifecycle === 'draft') ??
    [...versions].sort((left, right) => right.versionNumber - left.versionNumber)[0] ??
    null
  )
}

export function WorkflowDesignerDialog({
  workflowId,
  preferredVersionId,
  options,
  optionsLoading,
  optionsError,
  onRetryOptions,
  onClose,
  onChanged,
}: WorkflowDesignerDialogProps) {
  const workflow = useAdminWorkflow(workflowId)
  const [selectedVersionId, setSelectedVersionId] = useState<string | null>(
    preferredVersionId,
  )
  const selectedVersion = useMemo(
    () =>
      workflow.data
        ? chooseVersion(workflow.data.versions, selectedVersionId)
        : null,
    [selectedVersionId, workflow.data],
  )

  const reload = (nextVersionId?: string) => {
    if (nextVersionId) setSelectedVersionId(nextVersionId)
    workflow.retry()
    onChanged()
  }

  return (
    <Dialog open onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface className="administration-dialog workflow-designer-dialog">
        <DialogBody>
          <DialogTitle>Workflow designer</DialogTitle>
          <DialogContent className="administration-dialog__content workflow-designer-content">
            <AdministrationResourceState
              loading={workflow.loading}
              error={workflow.error}
              empty={
                !workflow.loading &&
                !workflow.error &&
                (!workflow.data || workflow.data.versions.length === 0)
              }
              loadingLabel="Loading workflow designer"
              emptyTitle="Workflow unavailable"
              emptyDescription="The workflow no longer exists or has no versions."
              onRetry={workflow.retry}
            >
              {workflow.data && selectedVersion ? (
                <>
                  <header className="request-type-designer-heading">
                    <div>
                      <span className="request-type-designer-heading__eyebrow">
                        {workflow.data.code}
                      </span>
                      <h2>{selectedVersion.nameEnglish}</h2>
                      <p dir="rtl" lang="ar">{selectedVersion.nameArabic}</p>
                    </div>
                    <div className="request-type-designer-heading__controls">
                      <StatusBadge
                        status={
                          workflow.data.isArchived
                            ? 'Archived'
                            : getWorkflowLifecycleLabel(selectedVersion.lifecycle)
                        }
                      />
                      {selectedVersion.isInUse ? <StatusBadge status="In use" /> : null}
                      <label>
                        <span>Version</span>
                        <select
                          className="request-type-select"
                          aria-label="Workflow version"
                          value={selectedVersion.id}
                          onChange={(event) => setSelectedVersionId(event.target.value)}
                        >
                          {[...workflow.data.versions]
                            .sort((left, right) => right.versionNumber - left.versionNumber)
                            .map((version) => (
                              <option key={version.id} value={version.id}>
                                Version {version.versionNumber} ·{' '}
                                {getWorkflowLifecycleLabel(version.lifecycle)}
                                {version.isInUse ? ' · In use' : ''}
                              </option>
                            ))}
                        </select>
                      </label>
                      <Button type="button" appearance="subtle" icon={<ArrowClockwise20Regular />} onClick={workflow.retry}>
                        Reload
                      </Button>
                    </div>
                  </header>
                  <WorkflowVersionDesigner
                    key={`${selectedVersion.id}:${selectedVersion.rowVersion}`}
                    workflow={workflow.data}
                    version={selectedVersion}
                    options={options}
                    optionsLoading={optionsLoading}
                    optionsError={optionsError}
                    onRetryOptions={onRetryOptions}
                    onReload={reload}
                  />
                </>
              ) : null}
            </AdministrationResourceState>
          </DialogContent>
          <DialogActions>
            <Button type="button" appearance="secondary" onClick={onClose}>
              Close designer
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
