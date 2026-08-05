import type { AdministrationOverview } from '../models'

export const administrationOverview: AdministrationOverview = {
  metrics: [
    {
      id: 'active-users',
      label: 'Active users',
      value: 24,
      description: 'Accounts currently enabled',
    },
    {
      id: 'published-workflows',
      label: 'Published workflows',
      value: 6,
      description: 'Approval workflows in use',
    },
    {
      id: 'request-types',
      label: 'Request types',
      value: 9,
      description: 'Configured request forms',
    },
  ],
  areas: [
    {
      id: 'users',
      title: 'Users',
      description: 'Manage on-premises user accounts and account status.',
      icon: 'people',
      itemCount: 24,
      itemLabel: 'active users',
    },
    {
      id: 'roles',
      title: 'Roles and permissions',
      description: 'Control application roles and permission assignments.',
      icon: 'permissions',
      itemCount: 3,
      itemLabel: 'roles',
    },
    {
      id: 'workflows',
      title: 'Approval workflows',
      description: 'Configure approval stages, routing, and escalation rules.',
      icon: 'workflow',
      itemCount: 6,
      itemLabel: 'published workflows',
    },
    {
      id: 'request-types',
      title: 'Request types',
      description: 'Maintain request forms, fields, and supporting documents.',
      icon: 'form',
      itemCount: 9,
      itemLabel: 'request types',
    },
    {
      id: 'settings',
      title: 'System settings',
      description: 'Review application defaults, retention, and notifications.',
      icon: 'settings',
      itemCount: null,
      itemLabel: null,
    },
  ],
}
