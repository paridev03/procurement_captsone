import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import NavBar from './components/NavBar';
import RoleGuard from './components/RoleGuard';
import Login from './pages/Login';
import Register from './pages/Register';
import Dashboard from './pages/Dashboard';
import Worklist from './pages/Worklist';
import RequestForm from './pages/RequestForm';
import RequestDetail from './pages/RequestDetail';
import ProcurementBuilder from './pages/ProcurementBuilder';
import PaymentPage from './pages/PaymentPage';
import InvoicePage from './pages/InvoicePage';
import NotFound from './pages/NotFound';

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <NavBar />
        <main className="page">
          <Routes>
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />

            <Route
              path="/"
              element={
                <RoleGuard>
                  <Dashboard />
                </RoleGuard>
              }
            />

            <Route
              path="/requests"
              element={
                <RoleGuard>
                  <Worklist />
                </RoleGuard>
              }
            />

            <Route
              path="/requests/new"
              element={
                <RoleGuard roles={['Employee']}>
                  <RequestForm mode="create" />
                </RoleGuard>
              }
            />

            <Route
              path="/requests/:id/edit"
              element={
                <RoleGuard roles={['Employee']}>
                  <RequestForm mode="edit" />
                </RoleGuard>
              }
            />

            <Route
              path="/requests/:id"
              element={
                <RoleGuard>
                  <RequestDetail />
                </RoleGuard>
              }
            />

            <Route
              path="/procurement/new"
              element={
                <RoleGuard roles={['ProcurementAdmin']}>
                  <ProcurementBuilder mode="create" />
                </RoleGuard>
              }
            />

            <Route
              path="/procurement/:id/edit"
              element={
                <RoleGuard roles={['ProcurementAdmin']}>
                  <ProcurementBuilder mode="edit" />
                </RoleGuard>
              }
            />

            <Route
              path="/payment/:id"
              element={
                <RoleGuard roles={['ProcurementAdmin']}>
                  <PaymentPage />
                </RoleGuard>
              }
            />

            <Route
              path="/invoice/:id"
              element={
                <RoleGuard roles={['ProcurementAdmin']}>
                  <InvoicePage />
                </RoleGuard>
              }
            />

            <Route path="*" element={<NotFound />} />
          </Routes>
        </main>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
