import { Badge, Button, Card, Spinner } from '@fluentui/react-components'
import {
  ArrowClockwise20Regular,
  ArrowRight20Regular,
  Building20Regular,
  CheckmarkCircle24Regular,
  DismissCircle24Regular,
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
import { useSystemInfo } from '../../hooks/useSystemInfo'
import type { AdministrationIconName } from '../../models'
import { mockDataService } from '../../services'

const systemTimestampFormatter = new Intl.DateTimeFormat(undefined, {
  day: 'numeric',
  hour: 'numeric',
  minute: '2-digit',
  month: 'short',
  second: '2-digit',
  timeZone: 'UTC',
  timeZoneName: 'short',
  year: 'numeric',
})

function formatSystemTimestamp(value: string): string {
  const timestamp = new Date(value)
  return Number.isNaN(timestamp.getTime())
    ? 'Unavailable'
    : systemTimestampFormatter.format(timestamp)
}

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
  const { data: systemInfo, loading, error, retry } = useSystemInfo()
  const connectionStatus = loading
    ? 'Loading'
    : systemInfo
      ? 'Connected'
      : 'Unavailable'
  const connectionColor = loading
    ? 'informative'
    : systemInfo
      ? 'success'
      : 'danger'

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
        title="System Information"
        description="Live deployment details reported by the independent ASP.NET API."
        action={
          <div className="system-information__connection">
            <span className="system-information__connection-label">
              API connection status
            </span>
            <Badge
              appearance="tint"
              color={connectionColor}
              aria-label={`API connection status: ${connectionStatus}`}
            >
              {connectionStatus}
            </Badge>
          </div>
        }
      >
        <div className="system-information" aria-busy={loading}>
          {loading ? (
            <div className="system-information__loading" role="status">
              <Spinner size="small" label="Connecting to the API" />
              <p>Requesting live system details.</p>
            </div>
          ) : systemInfo ? (
            <dl className="system-information__grid">
              <div className="system-information__item">
                <dt>Application name</dt>
                <dd>{systemInfo.applicationName}</dd>
              </div>
              <div className="system-information__item">
                <dt>Version</dt>
                <dd>{systemInfo.version}</dd>
              </div>
              <div className="system-information__item">
                <dt>Environment</dt>
                <dd>{systemInfo.environment}</dd>
              </div>
              <div className="system-information__item">
                <dt>UTC timestamp</dt>
                <dd>
                  <time dateTime={systemInfo.currentUtcTimestamp}>
                    {formatSystemTimestamp(systemInfo.currentUtcTimestamp)}
                  </time>
                </dd>
              </div>
            </dl>
          ) : (
            <div className="system-information__unavailable" role="alert">
              <div className="system-information__unavailable-icon" aria-hidden="true">
                <DismissCircle24Regular />
              </div>
              <div className="system-information__unavailable-copy">
                <h3>System information is unavailable</h3>
                <p>{error ?? 'The API could not be reached. Please try again.'}</p>
              </div>
              <Button
                type="button"
                appearance="primary"
                icon={<ArrowClockwise20Regular />}
                onClick={retry}
              >
                Retry
              </Button>
            </div>
          )}
        </div>
      </SectionPanel>

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
