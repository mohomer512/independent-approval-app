import { Button, Card } from '@fluentui/react-components'
import {
  ArrowRight20Regular,
  Building20Regular,
  CheckmarkCircle24Regular,
  Flow24Regular,
  Form24Regular,
  People24Regular,
  Settings24Regular,
  Shield24Regular,
} from '@fluentui/react-icons'
import type { ReactNode } from 'react'
import {
  PageHeader,
  SectionPanel,
  StatusBadge,
  SummaryCard,
} from '../../components/common'
import type { AdministrationIconName } from '../../models'
import { mockDataService } from '../../services'

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

function getMetricIcon(metricId: string): ReactNode {
  switch (metricId) {
    case 'active-users':
      return <People24Regular />
    case 'published-workflows':
      return <Flow24Regular />
    default:
      return <Form24Regular />
  }
}

export function AdministrationPage() {
  const overview = mockDataService.getAdministrationOverview()
  const areas = mockDataService.getAdministrationAreas()

  return (
    <div className="administration-page page-stack">
      <PageHeader
        eyebrow="Configuration"
        title="Administration"
        description="Manage users, roles, workflows, request types, and application settings."
      />

      <section className="administration-summary" aria-label="System summary">
        {overview.metrics.map((metric) => (
          <SummaryCard
            key={metric.id}
            label={metric.label}
            value={metric.value}
            description={metric.description}
            icon={getMetricIcon(metric.id)}
            tone="brand"
          />
        ))}
      </section>

      <SectionPanel
        title="Configuration areas"
        description="Choose an area to manage the on-premises approval environment."
      >
        <div className="administration-grid">
          {areas.map((area) => (
            <Card className="administration-card" key={area.id} role="article">
              <div className="administration-card__icon" aria-hidden="true">
                {getAreaIcon(area.icon)}
              </div>
              <div className="administration-card__content">
                <h3 className="administration-card__title">{area.title}</h3>
                <p className="administration-card__description">
                  {area.description}
                </p>
                {area.itemCount !== null && area.itemLabel !== null ? (
                  <p className="administration-card__count">
                    <strong>{area.itemCount}</strong> {area.itemLabel}
                  </p>
                ) : (
                  <p className="administration-card__count">
                    Organization-wide defaults
                  </p>
                )}
              </div>
              <Button
                type="button"
                appearance="subtle"
                icon={<ArrowRight20Regular />}
                iconPosition="after"
                aria-label={`Open ${area.title}`}
              >
                Manage
              </Button>
            </Card>
          ))}
        </div>
      </SectionPanel>

      <SectionPanel
        title="Deployment readiness"
        description="Stage 1 establishes the application shell without external dependencies."
      >
        <div className="readiness-grid">
          <article className="readiness-item">
            <div className="readiness-item__icon" aria-hidden="true">
              <Building20Regular />
            </div>
            <div className="readiness-item__content">
              <div className="readiness-item__heading">
                <h3>On-premises application</h3>
                <StatusBadge status="Active" />
              </div>
              <p>The client application is designed for local deployment.</p>
            </div>
          </article>
          <article className="readiness-item">
            <div className="readiness-item__icon" aria-hidden="true">
              <Shield24Regular />
            </div>
            <div className="readiness-item__content">
              <div className="readiness-item__heading">
                <h3>Directory integration</h3>
                <StatusBadge status="Planned" />
              </div>
              <p>Enterprise identity integration will be configured later.</p>
            </div>
          </article>
          <article className="readiness-item">
            <div className="readiness-item__icon" aria-hidden="true">
              <CheckmarkCircle24Regular />
            </div>
            <div className="readiness-item__content">
              <div className="readiness-item__heading">
                <h3>Mock data boundary</h3>
                <StatusBadge status="Active" />
              </div>
              <p>Pages consume typed data through a replaceable service layer.</p>
            </div>
          </article>
        </div>
      </SectionPanel>
    </div>
  )
}
