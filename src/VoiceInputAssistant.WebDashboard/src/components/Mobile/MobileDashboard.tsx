import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Grid,
  Typography,
  Chip,
  LinearProgress,
  IconButton,
  Collapse,
  Alert,
  useTheme,
  Avatar,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  Divider,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Slide,
} from '@mui/material';
import {
  Mic,
  MicOff,
  Speed,
  Analytics,
  CheckCircle,
  Error,
  ExpandMore,
  ExpandLess,
  Refresh,
  TrendingUp,
  TrendingDown,
  VolumeUp,
  History,
  Settings,
} from '@mui/icons-material';
import { TransitionProps } from '@mui/material/transitions';
// import { VoiceAssistantService } from '../../services/VoiceAssistantService';
// import { useVoiceAssistant } from '../../contexts/VoiceAssistantContext';

interface MobileDashboardProps {
  className?: string;
}

const Transition = React.forwardRef(function Transition(
  props: TransitionProps & {
    children: React.ReactElement<any, any>;
  },
  ref: React.Ref<unknown>,
) {
  return <Slide direction="up" ref={ref} {...props} />;
});

const MobileDashboard: React.FC<MobileDashboardProps> = ({ className }) => {
  const theme = useTheme();
  // const { status } = useVoiceAssistant();
  const [stats, setStats] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [expandedSection, setExpandedSection] = useState<string | null>(null);
  const [showQuickActions, setShowQuickActions] = useState(false);
  const [recentActivity, setRecentActivity] = useState<any[]>([]);

  useEffect(() => {
    fetchDashboardData();
    
    // Auto-refresh every 30 seconds
    const interval = setInterval(fetchDashboardData, 30000);
    
    return () => clearInterval(interval);
  }, []);

  const fetchDashboardData = async () => {
    try {
      setError(null);
      
      // const [statusResponse, analyticsResponse, historyResponse] = await Promise.all([
      //   VoiceAssistantService.getStatus(),
      //   VoiceAssistantService.getAnalytics(),
      //   VoiceAssistantService.getHistory(5), // Get last 5 items
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
      
      // setRecentActivity(historyResponse.slice(0, 3)); // Show only 3 most recent
      setLoading(false); // Manually set loading to false as API calls are commented out
    } catch (err) {
      setError((err as any) instanceof Error ? (err as any).message : 'Failed to fetch dashboard data');
    } finally {
      setLoading(false);
    }
  };

  const handleSectionToggle = (section: string) => {
    setExpandedSection(expandedSection === section ? null : section);
  };

  const handleRefresh = () => {
    setLoading(true);
    fetchDashboardData();
  };

  const quickActions = [
    {
      icon: <Settings />,
      label: 'Settings',
      action: () => {/* Navigate to settings */},
    },
    {
      icon: <History />,
      label: 'View All History',
      action: () => {/* Navigate to history */},
    },
    {
      icon: <Analytics />,
      label: 'Detailed Analytics',
      action: () => {/* Navigate to analytics */},
    },
  ];

  if (loading && !stats) {
    return (
      <Box className={className} sx={{ p: 2 }}>
        <Typography variant="h5" gutterBottom>
          Dashboard
        </Typography>
        <LinearProgress sx={{ mb: 2 }} />
        <Typography variant="body2" color="text.secondary">
          Loading dashboard data...
        </Typography>
      </Box>
    );
  }

  if (error) {
    return (
      <Box className={className} sx={{ p: 2 }}>
        <Typography variant="h5" gutterBottom>
          Dashboard
        </Typography>
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
        <Button variant="outlined" onClick={handleRefresh} startIcon={<Refresh />}>
          Try Again
        </Button>
      </Box>
    );
  }

  return (
    <Box className={className} sx={{ p: 1 }}>
      {/* Header with refresh button */}
      <Box sx={{ 
        display: 'flex', 
        justifyContent: 'space-between', 
        alignItems: 'center', 
        mb: 2 
      }}>
        <Typography variant="h5" fontWeight="bold">
          Dashboard
        </Typography>
        <IconButton onClick={handleRefresh} disabled={loading}>
          <Refresh />
        </IconButton>
      </Box>

      {/* Status Cards - Optimized for mobile */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        {/* Main Status Card */}
        <Grid item xs={12}>
          <Card 
            sx={{ 
              background: stats?.isHealthy 
                ? 'linear-gradient(135deg, #4caf50 0%, #8bc34a 100%)'
                : 'linear-gradient(135deg, #f44336 0%, #e57373 100%)',
              color: 'white',
              position: 'relative',
              overflow: 'hidden',
            }}
          >
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                {stats?.isListening ? (
                  <Mic sx={{ mr: 1, fontSize: 28 }} />
                ) : (
                  <MicOff sx={{ mr: 1, fontSize: 28 }} />
                )}
                <Box>
                  <Typography variant="h6" fontWeight="bold">
                    {stats?.isListening ? 'Listening' : 'Idle'}
                  </Typography>
                  <Typography variant="body2" sx={{ opacity: 0.9 }}>
                    {stats?.activeEngine} • {stats?.activeProfile}
                  </Typography>
                </Box>
              </Box>
              
              <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                <Chip
                  icon={stats?.isHealthy ? <CheckCircle /> : <Error />}
                  label={stats?.isHealthy ? 'Healthy' : 'Error'}
                  size="small"
                  sx={{ 
                    backgroundColor: 'rgba(255, 255, 255, 0.2)',
                    color: 'white',
                  }}
                />
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Stats Grid - 2x2 layout for mobile */}
        <Grid item xs={6}>
          <Card sx={{ height: '100%' }}>
            <CardContent sx={{ textAlign: 'center', p: 2 }}>
              <Analytics color="primary" sx={{ fontSize: 32, mb: 1 }} />
              <Typography variant="h6" color="primary" fontWeight="bold">
                {stats?.totalRecognitions?.toLocaleString() || '0'}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Recognitions
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={6}>
          <Card sx={{ height: '100%' }}>
            <CardContent sx={{ textAlign: 'center', p: 2 }}>
              <CheckCircle color="success" sx={{ fontSize: 32, mb: 1 }} />
              <Typography variant="h6" color="success.main" fontWeight="bold">
                {stats?.averageAccuracy?.toFixed(1) || '0'}%
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Accuracy
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={6}>
          <Card sx={{ height: '100%' }}>
            <CardContent sx={{ textAlign: 'center', p: 2 }}>
              <Speed color="info" sx={{ fontSize: 32, mb: 1 }} />
              <Typography variant="h6" color="info.main" fontWeight="bold">
                {Math.round(stats?.averageProcessingTime || 0)}ms
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Avg Speed
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={6}>
          <Card sx={{ height: '100%' }}>
            <CardContent sx={{ textAlign: 'center', p: 2 }}>
              <VolumeUp color="secondary" sx={{ fontSize: 32, mb: 1 }} />
              <Typography variant="h6" color="secondary.main" fontWeight="bold">
                {stats?.totalWords?.toLocaleString() || '0'}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Words
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Recent Activity */}
      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Box 
            sx={{ 
              display: 'flex', 
              justifyContent: 'space-between', 
              alignItems: 'center',
              mb: 2,
              cursor: 'pointer',
            }}
            onClick={() => handleSectionToggle('activity')}
          >
            <Typography variant="h6" fontWeight="bold">
              Recent Activity
            </Typography>
            <IconButton size="small">
              {expandedSection === 'activity' ? <ExpandLess /> : <ExpandMore />}
            </IconButton>
          </Box>
          
          <Collapse in={expandedSection === 'activity'} timeout="auto" unmountOnExit>
            {recentActivity.length > 0 ? (
              <List dense>
                {recentActivity.map((item, index) => (
                  <React.Fragment key={item.id || index}>
                    <ListItem sx={{ px: 0 }}>
                      <ListItemAvatar>
                        <Avatar sx={{ 
                          bgcolor: theme.palette.primary.main,
                          width: 32, 
                          height: 32,
                          fontSize: '0.75rem',
                        }}>
                          {item.engine?.charAt(0) || 'V'}
                        </Avatar>
                      </ListItemAvatar>
                      <ListItemText
                        primary={
                          <Typography variant="body2" noWrap>
                            {item.text || 'No text available'}
                          </Typography>
                        }
                        secondary={
                          <Typography variant="caption" color="text.secondary">
                            {item.confidence ? `${Math.round(item.confidence * 100)}% • ` : ''}
                            {new Date(item.completedTime).toLocaleTimeString()}
                          </Typography>
                        }
                      />
                    </ListItem>
                    {index < recentActivity.length - 1 && <Divider variant="inset" />}
                  </React.Fragment>
                ))}
              </List>
            ) : (
              <Typography variant="body2" color="text.secondary" textAlign="center" py={2}>
                No recent activity
              </Typography>
            )}
          </Collapse>
        </CardContent>
      </Card>

      {/* Performance Summary */}
      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Typography variant="h6" fontWeight="bold" gutterBottom>
            Performance
          </Typography>
          
          <Box sx={{ mb: 2 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
              <Typography variant="body2">Recognition Accuracy</Typography>
              <Typography variant="body2" fontWeight="bold">
                {stats?.averageAccuracy?.toFixed(1) || '0'}%
              </Typography>
            </Box>
            <LinearProgress
              variant="determinate"
              value={stats?.averageAccuracy || 0}
              sx={{ 
                height: 8, 
                borderRadius: 4,
                bgcolor: theme.palette.grey[200],
              }}
            />
          </Box>

          <Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
              <Typography variant="body2">Processing Speed</Typography>
              <Typography variant="body2" fontWeight="bold">
                {Math.round(stats?.averageProcessingTime || 0)}ms
              </Typography>
            </Box>
            <LinearProgress
              variant="determinate"
              value={Math.min((500 - (stats?.averageProcessingTime || 500)) / 5, 100)}
              color="info"
              sx={{ 
                height: 8, 
                borderRadius: 4,
                bgcolor: theme.palette.grey[200],
              }}
            />
          </Box>
        </CardContent>
      </Card>

      {/* Quick Actions */}
      <Card>
        <CardContent>
          <Typography variant="h6" fontWeight="bold" gutterBottom>
            Quick Actions
          </Typography>
          
          <Grid container spacing={2}>
            {quickActions.map((action, index) => (
              <Grid item xs={4} key={index}>
                <Button
                  variant="outlined"
                  fullWidth
                  startIcon={action.icon}
                  onClick={action.action}
                  sx={{
                    flexDirection: 'column',
                    py: 2,
                    minHeight: 80,
                    '& .MuiButton-startIcon': {
                      m: 0,
                      mb: 1,
                    },
                  }}
                >
                  <Typography variant="caption" textAlign="center">
                    {action.label}
                  </Typography>
                </Button>
              </Grid>
            ))}
          </Grid>
        </CardContent>
      </Card>

      {/* Loading overlay for refresh */}
      {loading && stats && (
        <LinearProgress 
          sx={{ 
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            zIndex: theme.zIndex.appBar + 1,
          }} 
        />
      )}
    </Box>
  );
};

export default MobileDashboard;
