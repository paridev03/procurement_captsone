import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import NavBar from './components/NavBar';
import RoleGuard from './components/RoleGuard';
import Login from './pages/Login';
import Worklist from './pages/Worklist';
import RequestForm from './pages/RequestForm';
import RequestDetail from './pages/RequestDetail';
import NotFound from './pages/NotFound';

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <NavBar />
        <main className="page">
          <Routes>
            <Route path="/login" element={<Login />} />

            <Route
              path="/"
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

            <Route path="*" element={<NotFound />} />
          </Routes>
        </main>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
