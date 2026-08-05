import { lazy } from 'react'
import { Navigate, Route, Routes } from 'react-router'
import { AppLayout } from '../components/layout/AppLayout'

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

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<DashboardPage />} />
        <Route path="requests" element={<RequestsPage />} />
        <Route path="requests/new" element={<NewRequestPage />} />
        <Route path="tasks" element={<TasksPage />} />
        <Route path="documents" element={<DocumentsPage />} />
        <Route path="administration" element={<AdministrationPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  )
}
