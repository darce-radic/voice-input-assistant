import React, { useState, useEffect, useRef } from 'react';
import { useSwipeable } from 'react-swipeable';
import { DeviceFeatures } from '../utils/pwaUtils';

interface MobileNavigationProps {
  isOpen: boolean;
  onToggle: () => void;
  currentPath: string;
  onNavigate: (path: string) => void;
}

interface NavItem {
  path: string;
  label: string;
  icon: string;
  badge?: number;
}

const MobileNavigation: React.FC<MobileNavigationProps> = ({
  isOpen,
  onToggle,
  currentPath,
  onNavigate
}) => {
  const [touchStart, setTouchStart] = useState<number | null>(null);
  const [touchEnd, setTouchEnd] = useState<number | null>(null);
  const navRef = useRef<HTMLDivElement>(null);

  const navItems: NavItem[] = [
    { path: '/dashboard', label: 'Dashboard', icon: '📊' },
    { path: '/voice-input', label: 'Voice Input', icon: '🎤', badge: 2 },
    { path: '/history', label: 'History', icon: '📝' },
    { path: '/analytics', label: 'Analytics', icon: '📈' },
    { path: '/profiles', label: 'Profiles', icon: '👤' },
    { path: '/settings', label: 'Settings', icon: '⚙️' },
  ];

  // Gesture handlers
  const swipeHandlers = useSwipeable({
    onSwipedLeft: () => {
      if (isOpen) {
        onToggle();
        DeviceFeatures.vibrate(50); // Light haptic feedback
      }
    },
    onSwipedRight: () => {
      if (!isOpen) {
        onToggle();
        DeviceFeatures.vibrate(50);
      }
    },
    preventScrollOnSwipe: true,
    trackMouse: true
  });

  const minSwipeDistance = 50;

  const onTouchStart = (e: React.TouchEvent) => {
    setTouchEnd(null);
    setTouchStart(e.targetTouches[0].clientX);
  };

  const onTouchMove = (e: React.TouchEvent) => {
    setTouchEnd(e.targetTouches[0].clientX);
  };

  const onTouchEnd = () => {
    if (!touchStart || !touchEnd) return;
    
    const distance = touchStart - touchEnd;
    const isLeftSwipe = distance > minSwipeDistance;
    const isRightSwipe = distance < -minSwipeDistance;

    if (isLeftSwipe && isOpen) {
      onToggle();
      DeviceFeatures.vibrate(50);
    }
    
    if (isRightSwipe && !isOpen) {
      onToggle();
      DeviceFeatures.vibrate(50);
    }
  };

  const handleNavClick = (path: string) => {
    onNavigate(path);
    onToggle(); // Close nav after navigation
    DeviceFeatures.vibrate(30); // Light feedback
  };

  const handleBackdropClick = (e: React.MouseEvent) => {
    if (e.target === e.currentTarget) {
      onToggle();
    }
  };

  // Close nav on escape key
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        onToggle();
      }
    };

    document.addEventListener('keydown', handleEscape);
    return () => document.removeEventListener('keydown', handleEscape);
  }, [isOpen, onToggle]);

  // Prevent body scroll when nav is open
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = 'unset';
    }

    return () => {
      document.body.style.overflow = 'unset';
    };
  }, [isOpen]);

  return (
    <>
      {/* Mobile Nav Toggle Button */}
      <button
        className="mobile-nav-toggle"
        onClick={onToggle}
        aria-label="Toggle navigation menu"
        aria-expanded={isOpen}
      >
        <div className={`hamburger ${isOpen ? 'active' : ''}`}>
          <span></span>
          <span></span>
          <span></span>
        </div>
      </button>

      {/* Backdrop */}
      {isOpen && (
        <div 
          className="mobile-nav-backdrop"
          onClick={handleBackdropClick}
          {...swipeHandlers}
        />
      )}

      {/* Navigation Drawer */}
      <nav
        {...swipeHandlers}
        ref={(el) => {
          swipeHandlers.ref(el);
          (navRef as React.MutableRefObject<HTMLDivElement | null>).current = el as HTMLDivElement | null;
        }}
        className={`mobile-nav ${isOpen ? 'open' : ''}`}
        onTouchStart={onTouchStart}
        onTouchMove={onTouchMove}
        onTouchEnd={onTouchEnd}
      >
        <div className="mobile-nav-header">
          <div className="nav-logo">
            <span className="logo-icon">🎤</span>
            <span className="logo-text">Voice Assistant</span>
          </div>
          <button 
            className="nav-close"
            onClick={onToggle}
            aria-label="Close navigation menu"
          >
            ✕
          </button>
        </div>

        <div className="mobile-nav-content">
          <ul className="nav-items">
            {navItems.map((item) => (
              <li key={item.path} className="nav-item">
                <button
                  className={`nav-link ${currentPath === item.path ? 'active' : ''}`}
                  onClick={() => handleNavClick(item.path)}
                  aria-current={currentPath === item.path ? 'page' : undefined}
                >
                  <span className="nav-icon">{item.icon}</span>
                  <span className="nav-label">{item.label}</span>
                  {item.badge && (
                    <span className="nav-badge" aria-label={`${item.badge} notifications`}>
                      {item.badge}
                    </span>
                  )}
                </button>
              </li>
            ))}
          </ul>

          <div className="nav-footer">
            <div className="user-info">
              <div className="user-avatar">👤</div>
              <div className="user-details">
                <span className="user-name">John Doe</span>
                <span className="user-email">john@example.com</span>
              </div>
            </div>
          </div>
        </div>
      </nav>
    </>
  );
};

export default MobileNavigation;