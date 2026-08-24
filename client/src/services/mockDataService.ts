import {
  approvalDocuments,
  approvalRequests,
  approvalTasks,
  currentUser,
  dashboardData,
  dashboardMetrics,
  navigationItems,
  quickActions,
} from '../mocks'
import type {
  AppUser,
  ApprovalDocument,
  ApprovalRequest,
  ApprovalTask,
  DashboardData,
  DashboardMetric,
  NavigationItem,
  QuickAction,
} from '../models'

export interface MockDataService {
  readonly getCurrentUser: () => AppUser
  readonly getNavigationItems: () => readonly NavigationItem[]
  readonly getDashboardData: () => DashboardData
  readonly getDashboardMetrics: () => readonly DashboardMetric[]
  readonly getQuickActions: () => readonly QuickAction[]
  readonly getRequests: () => readonly ApprovalRequest[]
  readonly getRecentRequests: (limit?: number) => readonly ApprovalRequest[]
  readonly getRequestById: (id: string) => ApprovalRequest | undefined
  readonly getTasks: () => readonly ApprovalTask[]
  readonly getPendingTasks: () => readonly ApprovalTask[]
  readonly getTaskById: (id: string) => ApprovalTask | undefined
  readonly getDocuments: () => readonly ApprovalDocument[]
  readonly getDocumentById: (id: string) => ApprovalDocument | undefined
}

export const mockDataService: MockDataService = {
  getCurrentUser: () => currentUser,
  getNavigationItems: () => navigationItems,
  getDashboardData: () => dashboardData,
  getDashboardMetrics: () => dashboardMetrics,
  getQuickActions: () => quickActions,
  getRequests: () => approvalRequests,
  getRecentRequests: (limit = 5) =>
    approvalRequests.slice(0, Math.max(0, Math.trunc(limit))),
  getRequestById: (id) => approvalRequests.find((request) => request.id === id),
  getTasks: () => approvalTasks,
  getPendingTasks: () =>
    approvalTasks.filter(
      (task) => task.status === 'Pending' || task.status === 'In Review',
    ),
  getTaskById: (id) => approvalTasks.find((task) => task.id === id),
  getDocuments: () => approvalDocuments,
  getDocumentById: (id) =>
    approvalDocuments.find((document) => document.id === id),
}
