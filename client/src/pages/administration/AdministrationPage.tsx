import { Button, Card } from '@fluentui/react-components'
import {
  ArrowRight20Regular,
  Flow24Regular,
  Form24Regular,
  People24Regular,
  Settings24Regular,
  Shield24Regular,
} from '@fluentui/react-icons'
import type { ReactNode } from 'react'
import { useNavigate } from 'react-router'
import { AdministrationResourceState } from '../../components/administration/AdministrationResourceState'
import { PageHeader, SectionPanel, SummaryCard } from '../../components/common'
import { useAdministrationSummary } from '../../hooks/useAdministrationSummary'
import type {
  AdministrationAreaId,
  AdministrationIconName,
  AdministrationSummary,
} from '../../models'

interface AdministrationAreaDefinition {
  readonly id: AdministrationAreaId
  readonly title: string
  readonly description: string
  readonly icon: AdministrationIconName
  readonly path: string
  readonly getCount: (summary: AdministrationSummary) => number | null
  readonly itemLabel: string | null
}

const administrationAreas: readonly AdministrationAreaDefinition[] = [
  {
    id: 'users',
    title: 'Users',
    description: 'Manage application access, role assignments, and account status.',
    icon: 'people',
    path: '/administration/users',
    getCount: (summary) => summary.activeUserCount,
    itemLabel: 'active users',
  },
  {
    id: 'roles',
    title: 'Roles and permissions',
    description: 'Create application roles from the fixed permission catalogue.',
    icon: 'permissions',
    path: '/administration/roles',
    getCount: (summary) => summary.activeRoleCount,
    itemLabel: 'active roles',
  },
  {
    id: 'workflows',
    title: 'Approval workflows',
    description: 'Configure versioned steps, transitions, and field permissions.',
    icon: 'workflow',
    path: '/administration/workflows',
    getCount: (summary) => summary.publishedWorkflowCount,
    itemLabel: 'published workflows',
  },
  {
    id: 'request-types',
    title: 'Request types',
    description: 'Define bilingual, versioned forms and custom fields.',
    icon: 'form',
    path: '/administration/request-types',
    getCount: (summary) => summary.activeRequestTypeCount,
    itemLabel: 'active request types',
  },
  {
    id: 'settings',
    title: 'System settings',
    description: 'Choose the application default language.',
    icon: 'settings',
    path: '/administration/settings',
    getCount: () => null,
    itemLabel: null,
  },
]

function getAreaIcon(iconName: AdministrationIconName): ReactNode {
  switch (iconName) {
    case 'people':
      return <People24Regular />
    case 'permissions':
      return <Shield24Regular />
    case 'workflow':
      return <Flow24Regular />
    case 'form':
      return <Form24Regular />
    case 'settings':
      return <Settings24Regular />
  }
}

export function AdministrationPage() {
  const navigate = useNavigate()
  const { data: summary, loading, error, retry } = useAdministrationSummary()

  return (
    <div className="administration-page page-stack">
      <PageHeader
        eyebrow="Configuration"
        title="Administration"
        description="Manage application access, roles, workflows, request types, and system settings."
      />

      <AdministrationResourceState
        loading={loading}
        error={error}
        loadingLabel="Loading administration summary"
        onRetry={retry}
      >
        {summary ? (
          <>
            <section
              className="administration-summary"
              aria-label="Administration summary"
            >
              <SummaryCard
                label="Active users"
                value={summary.activeUserCount}
                description="Accounts currently enabled"
                icon={<People24Regular />}
                tone="brand"
              />
              <SummaryCard
                label="Active roles"
                value={summary.activeRoleCount}
                description="Custom roles available"
                icon={<Shield24Regular />}
                tone="informative"
              />
              <SummaryCard
                label="Published workflows"
                value={summary.publishedWorkflowCount}
                description="Stable workflow versions"
                icon={<Flow24Regular />}
                tone="success"
              />
              <SummaryCard
                label="Request types"
                value={summary.activeRequestTypeCount}
                description="Active request definitions"
                icon={<Form24Regular />}
                tone="neutral"
              />
            </section>

            <SectionPanel
              title="Configuration areas"
              description="Choose an area to manage the Independent Approval application."
            >
              <div className="administration-grid">
                {administrationAreas.map((area) => {
                  const count = area.getCount(summary)

                  return (
                    <Card
                      className="administration-card"
                      key={area.id}
                      role="article"
                    >
                      <div className="administration-card__icon" aria-hidden="true">
                        {getAreaIcon(area.icon)}
                      </div>
                      <div className="administration-card__content">
                        <h3 className="administration-card__title">{area.title}</h3>
                        <p className="administration-card__description">
                          {area.description}
                        </p>
                        <p className="administration-card__count">
                          {count === null || area.itemLabel === null ? (
                            'Application-wide default'
                          ) : (
                            <>
                              <strong>{count}</strong> {area.itemLabel}
                            </>
                          )}
                        </p>
                      </div>
                      <Button
                        type="button"
                        appearance="subtle"
                        icon={<ArrowRight20Regular />}
                        iconPosition="after"
                        aria-label={`Open ${area.title}`}
                        onClick={() => navigate(area.path)}
                      >
                        Manage
                      </Button>
                    </Card>
                  )
                })}
              </div>
            </SectionPanel>
          </>
        ) : null}
      </AdministrationResourceState>
    </div>
  )
}
