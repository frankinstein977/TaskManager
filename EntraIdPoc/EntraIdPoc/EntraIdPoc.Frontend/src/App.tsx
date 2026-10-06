import { Routes, Route } from 'react-router-dom';
import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react';
import Layout from './components/Layout';
import Home from './pages/Home';
import Dashboard from './pages/Dashboard';
import Projects from './pages/Projects';
import Admin from './pages/Admin';
import Profile from './pages/Profile';
import LoginPage from './pages/LoginPage';

function App() {
  return (
    <>
      <UnauthenticatedTemplate>
        <LoginPage />
      </UnauthenticatedTemplate>

      <AuthenticatedTemplate>
        <Layout>
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/dashboard" element={<Dashboard />} />
            <Route path="/projects" element={<Projects />} />
            <Route path="/admin" element={<Admin />} />
            <Route path="/profile" element={<Profile />} />
          </Routes>
        </Layout>
      </AuthenticatedTemplate>
    </>
  );
}

export default App;
