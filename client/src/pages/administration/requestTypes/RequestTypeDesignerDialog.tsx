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
import { useAdminRequestType } from '../../../hooks/useAdminRequestType'
import { useRequestTypeSystemFields } from '../../../hooks/useRequestTypeSystemFields'
import type { RequestTypeVersion } from '../../../models'
import { getLifecycleLabel } from './requestTypeUi'
import { RequestTypeVersionDesigner } from './RequestTypeVersionDesigner'

interface RequestTypeDesignerDialogProps {
  readonly requestTypeId: string
  readonly preferredVersionId: string | null
  readonly onClose: () => void
  readonly onChanged: () => void
}

function chooseVersion(
  versions: readonly RequestTypeVersion[],
  preferredVersionId: string | null,
): RequestTypeVersion | null {
  return (
    versions.find((version) => version.id === preferredVersionId) ??
    versions.find((version) => version.lifecycle === 'draft') ??
    [...versions].sort((left, right) => right.versionNumber - left.versionNumber)[0] ??
    null
  )
}

export function RequestTypeDesignerDialog({
  requestTypeId,
  preferredVersionId,
  onClose,
  onChanged,
}: RequestTypeDesignerDialogProps) {
  const requestType = useAdminRequestType(requestTypeId)
  const systemFields = useRequestTypeSystemFields()
  const [selectedVersionId, setSelectedVersionId] = useState<string | null>(
    preferredVersionId,
  )
  const selectedVersion = useMemo(() => {
    if (!requestType.data) {
      return null
    }

    return chooseVersion(requestType.data.versions, selectedVersionId)
  }, [requestType.data, selectedVersionId])

  const reload = (nextVersionId?: string) => {
    if (nextVersionId) {
      setSelectedVersionId(nextVersionId)
    }

    requestType.retry()
    onChanged()
  }

  return (
    <Dialog open onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface className="administration-dialog request-type-designer-dialog">
        <DialogBody>
          <DialogTitle>Request type designer</DialogTitle>
          <DialogContent className="administration-dialog__content request-type-designer-content">
            <AdministrationResourceState
              loading={requestType.loading}
              error={requestType.error}
              empty={
                !requestType.loading &&
                !requestType.error &&
                (!requestType.data || requestType.data.versions.length === 0)
              }
              loadingLabel="Loading request type designer"
              emptyTitle="Request type unavailable"
              emptyDescription="The request type no longer exists or has no versions."
              onRetry={requestType.retry}
            >
              {requestType.data && selectedVersion ? (
                <>
                  <header className="request-type-designer-heading">
                    <div>
                      <span className="request-type-designer-heading__eyebrow">
                        {requestType.data.code}
                      </span>
                      <h2>{selectedVersion.nameEnglish}</h2>
                      <p dir="rtl" lang="ar">
                        {selectedVersion.nameArabic}
                      </p>
                    </div>
                    <div className="request-type-designer-heading__controls">
                      <StatusBadge
                        status={
                          requestType.data.isArchived
                            ? 'Archived'
                            : getLifecycleLabel(selectedVersion.lifecycle)
                        }
                      />
                      <label>
                        <span>Version</span>
                        <select
                          className="request-type-select"
                          aria-label="Request type version"
                          value={selectedVersion.id}
                          onChange={(event) =>
                            setSelectedVersionId(event.target.value)
                          }
                        >
                          {[...requestType.data.versions]
                            .sort(
                              (left, right) =>
                                right.versionNumber - left.versionNumber,
                            )
                            .map((version) => (
                              <option key={version.id} value={version.id}>
                                Version {version.versionNumber} ·{' '}
                                {getLifecycleLabel(version.lifecycle)}
                              </option>
                            ))}
                        </select>
                      </label>
                      <Button
                        type="button"
                        appearance="subtle"
                        icon={<ArrowClockwise20Regular />}
                        onClick={requestType.retry}
                      >
                        Reload
                      </Button>
                    </div>
                  </header>

                  <RequestTypeVersionDesigner
                    key={`${selectedVersion.id}:${selectedVersion.rowVersion}`}
                    requestType={requestType.data}
                    version={selectedVersion}
                    systemFields={systemFields.data ?? []}
                    systemFieldsLoading={systemFields.loading}
                    systemFieldsError={systemFields.error}
                    onRetrySystemFields={systemFields.retry}
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
