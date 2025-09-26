import React, { useState, useEffect } from 'react';
import {
  Box,
  BottomNavigation,
  BottomNavigationAction,
  AppBar,
  Toolbar,
  IconButton,
  Typography,
  Drawer,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Badge,
  Fab,
  useTheme,
  useMediaQuery,
  SwipeableDrawer,
} from '@mui/material';
import {
  Dashboard as DashboardIcon,
  Analytics as AnalyticsIcon,
  Settings as SettingsIcon,
  History as HistoryIcon,
  Menu as MenuIcon,
  Mic as MicIcon,
  MicOff as MicOffIcon,
  Notifications as NotificationsIcon,
  Person as PersonIcon,
  Close as CloseIcon,
} from '@mui/icons-material';
import { useLocation, useNavigate } from 'react-router-dom';
// import { useVoiceAssistant } from '../../contexts/VoiceAssistantContext';

interface MobileLayoutProps {
  children: React.ReactNode;
}

const MobileLayout: React.FC<MobileLayoutProps> = ({ children }) => {
  const theme = useTheme();
  const navigate = useNavigate();
  const location = useLocation();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [notificationCount, setNotificationCount] = useState(3);
  // const { status, startRecognition, stopRecognition, loading } = useVoiceAssistant();

  // Bottom navigation value based on current route
  const getBottomNavValue = () => {
    switch (location.pathname) {
      case '/dashboard':
      case '/':
        return 0;
      case '/analytics':
        return 1;
      case '/history':
        return 2;
      case '/settings':
        return 3;
      default:
        return 0;
    }
  };

  const handleBottomNavChange = (event: React.SyntheticEvent, newValue: number) => {
    const routes = ['/dashboard', '/analytics', '/history', '/settings'];
    navigate(routes[newValue]);
  };

  const handleFabClick = async () => {
    try {
      if (status.isListening) {
        await stopRecognition();
      } else {
        await startRecognition();
      }
    } catch (error) {
      console.error('Error toggling recognition:', error);
    }
  };

  const menuItems = [
    { text: 'Dashboard', icon: <DashboardIcon />, path: '/dashboard' },
    { text: 'Analytics', icon: <AnalyticsIcon />, path: '/analytics' },
    { text: 'History', icon: <HistoryIcon />, path: '/history' },
    { text: 'Settings', icon: <SettingsIcon />, path: '/settings' },
    { text: 'Profile', icon: <PersonIcon />, path: '/profile' },
  ];

  const handleMenuItemClick = (path: string) => {
    navigate(path);
    setDrawerOpen(false);
  };

  // PWA install prompt handling
  useEffect(() => {
    let deferredPrompt: any;

    const handleBeforeInstallPrompt = (e: Event) => {
      e.preventDefault();
      deferredPrompt = e;
      
      // Show install button or prompt
      // You could set a state here to show an install banner
    };

    window.addEventListener('beforeinstallprompt', handleBeforeInstallPrompt);

    return () => {
      window.removeEventListener('beforeinstallprompt', handleBeforeInstallPrompt);
    };
  }, []);

  // Mock status for UI development
  const status = {
    isListening: false,
    isInitialized: true,
    activeEngine: 'MockEngine',
  };
  const loading = false;
  const startRecognition = async () => console.log('Start recognition');
  const stopRecognition = async () => console.log('Stop recognition');


  if (!isMobile) {
    // Return desktop layout for larger screens
    return <>{children}</>;
  }

  return (
    <Box sx={{ 
      display: 'flex', 
      flexDirection: 'column', 
      minHeight: '100vh',
      backgroundColor: theme.palette.background.default 
    }}>
      {/* Mobile App Bar */}
      <AppBar 
        position="fixed" 
        sx={{ 
          zIndex: theme.zIndex.drawer + 1,
          backgroundColor: theme.palette.primary.main,
        }}
      >
        <Toolbar>
          <IconButton
            edge="start"
            color="inherit"
            onClick={() => setDrawerOpen(true)}
            sx={{ mr: 2 }}
          >
            <MenuIcon />
          </IconButton>
          
          <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
            Voice Assistant
          </Typography>

          <IconButton color="inherit" onClick={() => navigate('/notifications')}>
            <Badge badgeContent={notificationCount} color="error">
              <NotificationsIcon />
            </Badge>
          </IconButton>

          <IconButton color="inherit" onClick={() => navigate('/profile')}>
            <PersonIcon />
          </IconButton>
        </Toolbar>
      </AppBar>

      {/* Navigation Drawer */}
      <SwipeableDrawer
        anchor="left"
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        onOpen={() => setDrawerOpen(true)}
        swipeAreaWidth={20}
        disableSwipeToOpen={false}
        ModalProps={{
          keepMounted: true, // Better open performance on mobile
        }}
      >
        <Box sx={{ width: 280 }}>
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              p: 2,
              backgroundColor: theme.palette.primary.main,
              color: theme.palette.primary.contrastText,
            }}
          >
            <Typography variant="h6">Menu</Typography>
            <IconButton
              edge="end"
              color="inherit"
              onClick={() => setDrawerOpen(false)}
            >
              <CloseIcon />
            </IconButton>
          </Box>
          
          <List>
            {menuItems.map((item) => (
              <ListItem
                key={item.text}
                onClick={() => handleMenuItemClick(item.path)}
                sx={{
                  cursor: 'pointer',
                  '&:hover': {
                    backgroundColor: theme.palette.action.hover,
                  },
                  backgroundColor: location.pathname === item.path 
                    ? theme.palette.action.selected 
                    : 'transparent',
                }}
              >
                <ListItemIcon>{item.icon}</ListItemIcon>
                <ListItemText primary={item.text} />
              </ListItem>
            ))}
          </List>

          {/* Status indicator in drawer */}
          <Box sx={{ p: 2, mt: 'auto', borderTop: `1px solid ${theme.palette.divider}` }}>
            <Typography variant="body2" color="textSecondary" gutterBottom>
              System Status
            </Typography>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              {status.isListening ? <MicIcon color="primary" /> : <MicOffIcon />}
              <Typography variant="body2">
                {status.isListening ? 'Listening' : 'Idle'}
              </Typography>
            </Box>
            <Typography variant="caption" color="textSecondary">
              Engine: {status.activeEngine}
            </Typography>
          </Box>
        </Box>
      </SwipeableDrawer>

      {/* Main Content */}
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          mt: 7, // Account for AppBar height
          mb: 7, // Account for BottomNavigation height
          px: 1,
          py: 2,
          overflow: 'auto',
          // Add safe area padding for devices with notches/home indicators
          paddingTop: 'env(safe-area-inset-top, 0px)',
          paddingBottom: 'env(safe-area-inset-bottom, 0px)',
        }}
      >
        {children}
      </Box>

      {/* Floating Action Button for Voice Control */}
      <Fab
        color={status.isListening ? "secondary" : "primary"}
        sx={{
          position: 'fixed',
          bottom: theme.spacing(10), // Above bottom navigation
          right: theme.spacing(2),
          zIndex: theme.zIndex.speedDial,
          // Add ripple effect for better touch feedback
          '&:active': {
            transform: 'scale(0.95)',
          },
        }}
        onClick={handleFabClick}
        disabled={loading || !status.isInitialized}
      >
        {status.isListening ? <MicOffIcon /> : <MicIcon />}
      </Fab>

      {/* Bottom Navigation */}
      <BottomNavigation
        value={getBottomNavValue()}
        onChange={handleBottomNavChange}
        showLabels
        sx={{
          position: 'fixed',
          bottom: 0,
          left: 0,
          right: 0,
          zIndex: theme.zIndex.appBar,
          borderTop: `1px solid ${theme.palette.divider}`,
          backgroundColor: theme.palette.background.paper,
          // Add safe area padding for devices with home indicators
          paddingBottom: 'env(safe-area-inset-bottom, 0px)',
        }}
      >
        <BottomNavigationAction
          label="Dashboard"
          icon={<DashboardIcon />}
        />
        <BottomNavigationAction
          label="Analytics"
          icon={<AnalyticsIcon />}
        />
        <BottomNavigationAction
          label="History"
          icon={<HistoryIcon />}
        />
        <BottomNavigationAction
          label="Settings"
          icon={<SettingsIcon />}
        />
      </BottomNavigation>
    </Box>
  );
};

export default MobileLayout;