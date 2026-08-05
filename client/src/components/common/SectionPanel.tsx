import { Card } from '@fluentui/react-components'
import type { ReactNode } from 'react'

interface SectionPanelProps {
  readonly title: string
  readonly description?: string
  readonly action?: ReactNode
  readonly children: ReactNode
  readonly className?: string
}

export function SectionPanel({
  title,
  description,
  action,
  children,
  className,
}: SectionPanelProps) {
  const classes = ['section-panel', className].filter(Boolean).join(' ')

  return (
    <Card className={classes} role="region">
      <div className="section-panel__header">
        <div>
          <h2 className="section-panel__title">{title}</h2>
          {description ? (
            <p className="section-panel__description">{description}</p>
          ) : null}
        </div>
        {action ? <div className="section-panel__action">{action}</div> : null}
      </div>
      <div className="section-panel__body">{children}</div>
    </Card>
  )
}
