import React, { useState, useEffect, useCallback } from 'react';
import { Outlet, useNavigate, useLocation, Navigate } from 'react-router-dom';
import {
    AppBar,
    Box,
    CssBaseline,
    Drawer,
    IconButton,
    Toolbar,
    Typography,
    useMediaQuery,
    useTheme,
    CircularProgress
} from '@mui/material';
import MenuIcon from '@mui/icons-material/Menu';
import LogoutIcon from '@mui/icons-material/Logout';
import LaunchIcon from '@mui/icons-material/Launch';
import Tooltip from '@mui/material/Tooltip';
import useConfig from '../hooks/configuration/useConfig';
import { useAppStateContext } from '../hooks/appState/useAppStateContext';
import SidebarNav from './components/SidebarNav';

const DRAWER_WIDTH = 240;

function AdminLayout() {
    const theme = useTheme();
    const isMobile = useMediaQuery(theme.breakpoints.down('md'));
    const navigate = useNavigate();
    const location = useLocation();
    const { config } = useConfig();
    const { Authorization } = useAppStateContext();

    const [mobileOpen, setMobileOpen] = useState(false);
    const [authState, setAuthState] = useState({ checked: false, user: null });

    useEffect(() => {
        let cancelled = false;
        const checkAuth = async () => {
            try {
                const user = await Authorization.getAuthenticatedUser();
                if (!cancelled) setAuthState({ checked: true, user });
            } catch (err) {
                if (!cancelled) setAuthState({ checked: true, user: null });
            }
        };
        checkAuth();
        return () => { cancelled = true; };
    }, [Authorization]);

    const handleDrawerToggle = useCallback(() => {
        setMobileOpen((prev) => !prev);
    }, []);

    const handleLogout = useCallback(async () => {
        await Authorization.logoutUser();
        navigate('/');
    }, [Authorization, navigate]);

    const handleNavigate = useCallback((path) => {
        navigate(path);
        if (isMobile) setMobileOpen(false);
    }, [navigate, isMobile]);

    if (!authState.checked) {
        return (
            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
                <CircularProgress />
            </Box>
        );
    }

    if (!authState.user) {
        return <Navigate to="/" replace state={{ from: location.pathname }} />;
    }

    if (location.pathname === '/admin' || location.pathname === '/admin/') {
        return <Navigate to="/admin/dashboard" replace />;
    }

    const drawerContent = (
        <SidebarNav
            activePath={location.pathname}
            onNavigate={handleNavigate}
        />
    );

    return (
        <Box sx={{ display: 'flex', minHeight: '100vh' }}>
            <CssBaseline />
            <AppBar
                position="fixed"
                sx={{
                    width: { md: `calc(100% - ${DRAWER_WIDTH}px)` },
                    ml: { md: `${DRAWER_WIDTH}px` }
                }}
            >
                <Toolbar>
                    <IconButton
                        color="inherit"
                        aria-label="open drawer"
                        edge="start"
                        onClick={handleDrawerToggle}
                        sx={{ mr: 2, display: { md: 'none' } }}
                    >
                        <MenuIcon />
                    </IconButton>
                    <Typography variant="h6" noWrap component="div" sx={{ flexGrow: 1 }}>
                        {config?.Site?.siteName || 'CMS'} Admin
                    </Typography>
                    <Typography variant="body2" sx={{ mr: 2 }}>
                        {authState.user?.username}
                    </Typography>
                    <Tooltip title="View public site">
                        <IconButton color="inherit" onClick={() => navigate('/')}>
                            <LaunchIcon />
                        </IconButton>
                    </Tooltip>
                    <Tooltip title="Logout">
                        <IconButton color="inherit" onClick={handleLogout}>
                            <LogoutIcon />
                        </IconButton>
                    </Tooltip>
                </Toolbar>
            </AppBar>

            <Box
                component="nav"
                sx={{ width: { md: DRAWER_WIDTH }, flexShrink: { md: 0 } }}
                aria-label="admin navigation"
            >
                <Drawer
                    variant="temporary"
                    open={mobileOpen}
                    onClose={handleDrawerToggle}
                    ModalProps={{ keepMounted: true }}
                    sx={{
                        display: { xs: 'block', md: 'none' },
                        '& .MuiDrawer-paper': { boxSizing: 'border-box', width: DRAWER_WIDTH }
                    }}
                >
                    {drawerContent}
                </Drawer>
                <Drawer
                    variant="permanent"
                    sx={{
                        display: { xs: 'none', md: 'block' },
                        '& .MuiDrawer-paper': { boxSizing: 'border-box', width: DRAWER_WIDTH }
                    }}
                    open
                >
                    {drawerContent}
                </Drawer>
            </Box>

            <Box
                component="main"
                sx={{
                    flexGrow: 1,
                    p: 3,
                    width: { md: `calc(100% - ${DRAWER_WIDTH}px)` }
                }}
            >
                <Toolbar />
                <Outlet context={{ config }} />
            </Box>
        </Box>
    );
}

export default AdminLayout;
