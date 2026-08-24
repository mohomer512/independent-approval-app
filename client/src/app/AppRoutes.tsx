import { lazy } from 'react'
import { Navigate, Route, Routes } from 'react-router'
import { ApplicationAccessBoundary } from '../components/access/ApplicationAccessBoundary'
import { RequireSystemAdministrator } from '../components/access/RequireSystemAdministrator'
import { AppLayout } from '../components/layout/AppLayout'
import { AdministrationFoundationPendingPage } from '../pages/administration/AdministrationFoundationPendingPage'

const DashboardPage = lazy(() =>
  import('../pages/dashboard/DashboardPage').then((module) => ({
    default: module.DashboardPage,
  })),
)
const RequestsPage = lazy(() =>
  import('../pages/requests/RequestsPage').then((module) => ({
    default: module.RequestsPage,
  })),
)
const NewRequestPage = lazy(() =>
  import('../pages/requests/NewRequestPage').then((module) => ({
    default: module.NewRequestPage,
  })),
)
const TasksPage = lazy(() =>
  import('../pages/tasks/TasksPage').then((module) => ({
    default: module.TasksPage,
  })),
)
const DocumentsPage = lazy(() =>
  import('../pages/documents/DocumentsPage').then((module) => ({
    default: module.DocumentsPage,
  })),
)
const AdministrationPage = lazy(() =>
  import('../pages/administration/AdministrationPage').then((module) => ({
    default: module.AdministrationPage,
  })),
)
const UsersPage = lazy(() =>
  import('../pages/administration/users/UsersPage').then((module) => ({
    default: module.UsersPage,
  })),
)
const RolesPage = lazy(() =>
  import('../pages/administration/roles/RolesPage').then((module) => ({
    default: module.RolesPage,
  })),
)
const RequestTypesPage = lazy(() =>
  import('../pages/administration/requestTypes/RequestTypesPage').then(
    (module) => ({ default: module.RequestTypesPage }),
  ),
)
const WorkflowsPage = lazy(() =>
  import('../pages/administration/workflows/WorkflowsPage').then((module) => ({
    default: module.WorkflowsPage,
  })),
)

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<ApplicationAccessBoundary />}>
        <Route element={<AppLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="requests" element={<RequestsPage />} />
          <Route path="requests/new" element={<NewRequestPage />} />
          <Route path="tasks" element={<TasksPage />} />
          <Route path="documents" element={<DocumentsPage />} />
          <Route
            path="administration"
            element={<RequireSystemAdministrator />}
          >
            <Route index element={<AdministrationPage />} />
            <Route path="users" element={<UsersPage />} />
            <Route path="roles" element={<RolesPage />} />
            <Route path="request-types" element={<RequestTypesPage />} />
            <Route path="workflows" element={<WorkflowsPage />} />
            <Route
              path="settings"
              element={
                <AdministrationFoundationPendingPage
                  title="System settings"
                  description="Manage the application default language."
                />
              }
            />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Route>
    </Routes>
  )
}
