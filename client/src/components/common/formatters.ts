const dateFormatter = new Intl.DateTimeFormat('en-US', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
})

const dateTimeFormatter = new Intl.DateTimeFormat('en-US', {
  day: 'numeric',
  hour: 'numeric',
  minute: '2-digit',
  month: 'short',
  year: 'numeric',
})

export function formatDate(value: string | null): string {
  return value ? dateFormatter.format(new Date(value)) : 'Not submitted'
}

export function formatDateTime(value: string): string {
  return dateTimeFormatter.format(new Date(value))
}

export function formatCurrency(
  amount: number | null,
  currency: string | null,
): string {
  if (amount === null || currency === null) {
    return '—'
  }

  return new Intl.NumberFormat('en-US', {
    currency,
    maximumFractionDigits: 0,
    style: 'currency',
  }).format(amount)
}

export function formatFileSize(sizeBytes: number): string {
  if (sizeBytes < 1024) {
    return `${sizeBytes} B`
  }

  if (sizeBytes < 1024 * 1024) {
    return `${Math.round(sizeBytes / 1024)} KB`
  }

  return `${(sizeBytes / (1024 * 1024)).toFixed(1)} MB`
}
