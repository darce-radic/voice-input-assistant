import React, { Suspense } from 'react';
import {
  BrowserRouter as Router,
  Routes,
  Route,
  Navigate,
} from 'react-router-dom';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import { CssBaseline, Box, CircularProgress } from '@mui/material';
// import { Provider } from 'react-redux';
// import { store } from './store/store';
// import Sidebar from './components/Layout/Sidebar';
// import Header from './components/Layout/Header';
import ErrorBoundary from './components/ErrorBoundary';
import * as Sentry from '@sentry/react';
// import { BrowserTracing } from '@sentry/tracing';
// import { VoiceAssistantProvider } from './contexts/VoiceAssistantContext';

const Dashboard = React.lazy(() => import('./pages/Dashboard'));
const Analytics = React.lazy(() => import('./pages/Analytics'));
const Settings = React.lazy(() => import('./pages/Settings'));
const Profiles = React.lazy(() => import('./pages/Profiles'));
const History = React.lazy(() => import('./pages/History'));
const Status = React.lazy(() => import('./pages/Status'));

Sentry.init({
  dsn: 'YOUR_SENTRY_DSN_HERE',
  integrations: [Sentry.browserTracingIntegration()],
  tracesSampleRate: 1.0,
  beforeSend(event, hint) {
    if (process.env.NODE_ENV === 'development') {
      console.log(event);
      return null;
    }
    return event;
  },
});

const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#1976d2',
    },
    secondary: {
      main: '#dc004e',
    },
    background: {
      default: '#f5f5f5',
    },
  },
  typography: {
    fontFamily: '"Roboto", "Helvetica", "Arial", sans-serif',
    h4: {
      fontWeight: 600,
    },
    h5: {
      fontWeight: 600,
    },
    h6: {
      fontWeight: 600,
    },
  },
  components: {
    MuiDrawer: {
      styleOverrides: {
        paper: {
          backgroundColor: '#1e1e2e',
          color: '#ffffff',
        },
      },
    },
    MuiListItemButton: {
      styleOverrides: {
        root: {
          '&:hover': {
            backgroundColor: 'rgba(255, 255, 255, 0.08)',
          },
          '&.Mui-selected': {
            backgroundColor: 'rgba(25, 118, 210, 0.12)',
            '&:hover': {
              backgroundColor: 'rgba(25, 118, 210, 0.16)',
            },
          },
        },
      },
    },
  },
});

const App: React.FC = () => {
  return (
    // <Provider store={store}>
    //   <VoiceAssistantProvider>
        <ThemeProvider theme={theme}>
          <CssBaseline />
          <Router>
            <ErrorBoundary>
              <Box sx={{ display: 'flex', minHeight: '100vh' }}>
                {/* <Sidebar /> */}
                <Box component="main" sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column' }}>
                  {/* <Header /> */}
                  <Box sx={{ flexGrow: 1, p: 3, overflow: 'auto' }}>
                    <Suspense fallback={<CircularProgress />}>
                      <Routes>
                        <Route path="/" element={<Navigate to="/dashboard" replace />} />
                        <Route path="/dashboard" element={<Dashboard />} />
                        <Route path="/analytics" element={<Analytics />} />
                        <Route path="/settings" element={<Settings />} />
                        <Route path="/profiles" element={<Profiles />} />
                        <Route path="/history" element={<History />} />
                        <Route path="/status" element={<Status />} />
                      </Routes>
                    </Suspense>
                  </Box>
                </Box>
              </Box>
            </ErrorBoundary>
          </Router>
        </ThemeProvider>
    //   </VoiceAssistantProvider>
    // </Provider>
  );
};

export default App;