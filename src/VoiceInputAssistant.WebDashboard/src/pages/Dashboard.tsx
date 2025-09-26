import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Grid,
  Typography,
  Chip,
  LinearProgress,
  Alert,
} from '@mui/material';
import {
  Mic,
  MicOff,
  Speed,
  Analytics,
  CheckCircle,
  Error,
} from '@mui/icons-material';
import { Line } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js';

// import { useVoiceAssistant } from '../contexts/VoiceAssistantContext';
// import { VoiceAssistantService } from '../services/VoiceAssistantService';

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
);

interface DashboardStats {
  totalRecognitions: number;
  totalWords: number;
  averageAccuracy: number;
  averageProcessingTime: number;
  isListening: boolean;
  isHealthy: boolean;
  activeEngine: string;
  activeProfile: string;
}

const Dashboard: React.FC = () => {
  // const { status } = useVoiceAssistant();
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        setLoading(true);
        setError(null);

        // Fetch current status and analytics
        // const [statusResponse, analyticsResponse] = await Promise.all([
        //   VoiceAssistantService.getStatus(),
        //   VoiceAssistantService.getAnalytics(),
        // ]);

        // setStats({
        //   totalRecognitions: analyticsResponse.totalRecognitions,
        //   totalWords: analyticsResponse.totalWords,
        //   averageAccuracy: analyticsResponse.averageAccuracy,
        //   averageProcessingTime: analyticsResponse.averageProcessingTime,
        //   isListening: statusResponse.isListening,
        //   isHealthy: statusResponse.isHealthy,
        //   activeEngine: statusResponse.activeEngine,
        //   activeProfile: statusResponse.activeProfile,
        // });

        setStats({
          totalRecognitions: 1234,
          totalWords: 56789,
          averageAccuracy: 95.4,
          averageProcessingTime: 250,
          isListening: false,
          isHealthy: true,
          activeEngine: 'MockEngine',
          activeProfile: 'Default',
        });
        setLoading(false); // Manually set loading to false as API calls are commented out
      } catch (err) {
        setError((err as any) instanceof Error ? (err as any).message : 'Failed to fetch dashboard data');
      } finally {
        setLoading(false);
      }
    };

    fetchDashboardData();
    
    // Refresh data every 30 seconds
    const interval = setInterval(fetchDashboardData, 30000);
    
    return () => clearInterval(interval);
  }, []);

  const chartData = {
    labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
    datasets: [
      {
        label: 'Recognition Accuracy',
        data: [92, 94, 93, 95, 96, 94],
        borderColor: 'rgb(75, 192, 192)',
        backgroundColor: 'rgba(75, 192, 192, 0.2)',
        tension: 0.1,
      },
      {
        label: 'Processing Speed (ms)',
        data: [250, 240, 235, 220, 210, 205],
        borderColor: 'rgb(255, 99, 132)',
        backgroundColor: 'rgba(255, 99, 132, 0.2)',
        tension: 0.1,
      },
    ],
  };

  const chartOptions = {
    responsive: true,
    plugins: {
      legend: {
        position: 'top' as const,
      },
      title: {
        display: true,
        text: 'Performance Over Time',
      },
    },
  };

  if (loading) {
    return (
      <Box>
        <Typography variant="h4" gutterBottom>
          Dashboard
        </Typography>
        <LinearProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Box>
        <Typography variant="h4" gutterBottom>
          Dashboard
        </Typography>
        <Alert severity="error" sx={{ mt: 2 }}>
          {error}
        </Alert>
      </Box>
    );
  }

  if (!stats) {
    return (
      <Box>
        <Typography variant="h4" gutterBottom>
          Dashboard
        </Typography>
        <Alert severity="info" sx={{ mt: 2 }}>
          No data available
        </Alert>
      </Box>
    );
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Dashboard
      </Typography>

      <Grid container spacing={3}>
        {/* Status Cards */}
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography variant="h6" component="div">
                    Status
                  </Typography>
                  <Chip
                    icon={stats.isHealthy ? <CheckCircle /> : <Error />}
                    label={stats.isHealthy ? 'Healthy' : 'Error'}
                    color={stats.isHealthy ? 'success' : 'error'}
                    size="small"
                  />
                </Box>
                {stats.isListening ? (
                  <Mic color="primary" fontSize="large" />
                ) : (
                  <MicOff color="disabled" fontSize="large" />
                )}
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography variant="h6" component="div">
                    Recognitions
                  </Typography>
                  <Typography variant="h4" component="div" color="primary.main">
                    {stats.totalRecognitions.toLocaleString()}
                  </Typography>
                </Box>
                <Analytics color="primary" fontSize="large" />
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography variant="h6" component="div">
                    Accuracy
                  </Typography>
                  <Typography variant="h4" component="div" color="primary.main">
                    {stats.averageAccuracy.toFixed(1)}%
                  </Typography>
                </Box>
                <CheckCircle color="primary" fontSize="large" />
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography variant="h6" component="div">
                    Avg Speed
                  </Typography>
                  <Typography variant="h4" component="div" color="primary.main">
                    {Math.round(stats.averageProcessingTime)}ms
                  </Typography>
                </Box>
                <Speed color="primary" fontSize="large" />
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Current Configuration */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Current Configuration
              </Typography>
              <Box display="flex" flexDirection="column" gap={2}>
                <Box display="flex" justifyContent="space-between" alignItems="center">
                  <Typography variant="body1">Active Engine:</Typography>
                  <Chip label={stats.activeEngine} color="primary" variant="outlined" />
                </Box>
                <Box display="flex" justifyContent="space-between" alignItems="center">
                  <Typography variant="body1">Active Profile:</Typography>
                  <Chip label={stats.activeProfile} color="secondary" variant="outlined" />
                </Box>
                <Box display="flex" justifyContent="space-between" alignItems="center">
                  <Typography variant="body1">Recognition Status:</Typography>
                  <Chip
                    icon={stats.isListening ? <Mic /> : <MicOff />}
                    label={stats.isListening ? 'Listening' : 'Idle'}
                    color={stats.isListening ? 'success' : 'default'}
                  />
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Quick Stats */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Quick Stats
              </Typography>
              <Box display="flex" flexDirection="column" gap={2}>
                <Box>
                  <Box display="flex" justifyContent="space-between" mb={1}>
                    <Typography variant="body2">Total Words Processed</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {stats.totalWords.toLocaleString()}
                    </Typography>
                  </Box>
                  <LinearProgress
                    variant="determinate"
                    value={Math.min((stats.totalWords / 100000) * 100, 100)}
                    sx={{ height: 8, borderRadius: 4 }}
                  />
                </Box>
                <Box>
                  <Box display="flex" justifyContent="space-between" mb={1}>
                    <Typography variant="body2">Accuracy Rate</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {stats.averageAccuracy.toFixed(1)}%
                    </Typography>
                  </Box>
                  <LinearProgress
                    variant="determinate"
                    value={stats.averageAccuracy}
                    sx={{ height: 8, borderRadius: 4 }}
                    color="success"
                  />
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Performance Chart */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Performance Trends
              </Typography>
              <Box height={400}>
                <Line data={chartData} options={chartOptions} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default Dashboard;