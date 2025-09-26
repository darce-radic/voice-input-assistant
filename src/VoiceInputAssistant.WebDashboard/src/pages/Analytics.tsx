import React, { Suspense, lazy } from 'react';
import { Box, CircularProgress } from '@mui/material';
import ErrorBoundary from '../components/ErrorBoundary';

const AdvancedAnalytics = lazy(() => import('../components/AdvancedAnalytics'));

const AnalyticsPage: React.FC = () => {
  return (
    <Box>
      <ErrorBoundary>
        <Suspense fallback={<CircularProgress />}>
          <AdvancedAnalytics />
        </Suspense>
      </ErrorBoundary>
    </Box>
  );
};

export default AnalyticsPage;
