import { Card } from '@fluentui/react-components'
import type { ReactNode } from 'react'

interface SummaryCardProps {
  readonly label: string
  readonly value: number | string
  readonly description: string
  readonly icon: ReactNode
  readonly tone?: 'brand' | 'warning' | 'informative' | 'success' | 'neutral'
}

export function SummaryCard({
  label,
  value,
  description,
  icon,
  tone = 'neutral',
}: SummaryCardProps) {
  return (
    <Card className={`summary-card summary-card--${tone}`}>
      <div className="summary-card__icon" aria-hidden="true">
        {icon}
      </div>
      <div className="summary-card__content">
        <p className="summary-card__label">{label}</p>
        <p className="summary-card__value">{value}</p>
        <p className="summary-card__description">{description}</p>
      </div>
    </Card>
  )
}
