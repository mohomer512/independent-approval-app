export type UserRole = 'System Administrator' | 'Approver' | 'Requester'

export type UserAccountStatus = 'Active' | 'Inactive' | 'Locked'

export interface AppUser {
  readonly id: string
  readonly name: string
  readonly username: string
  readonly role: UserRole
  readonly department: string
  readonly email: string
  readonly initials: string
  readonly status: UserAccountStatus
}
