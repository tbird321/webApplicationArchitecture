import React, { useState,useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { IconButton, Typography } from '@mui/material';
import AccountCircleIcon from '@mui/icons-material/AccountCircle';
import LoginIcon from '@mui/icons-material/Login';
import Tooltip from '@mui/material/Tooltip';
import LogoutIcon from '@mui/icons-material/Logout';
import AdminPanelSettingsIcon from '@mui/icons-material/AdminPanelSettings';
import { ModalDialog} from '@tbirdcomponents/reactcomponents'; // Adjust the import path accordingly
import { useAppStateContext } from '../hooks/appState/useAppStateContext'; // Adjust the import path accordingly
import AuthenticationWrapper from './authentication/AuthenticationWrapper';
import AccountWrapper from './account/AccountWrapper';
function UserStatus({ onLoggedIn, onLoggedOut }) {
    const navigate = useNavigate();
    const [showLogin, setShowLogin] = useState(false);
    const { Authorization } = useAppStateContext();
    const [showAccount, setShowAccount] = useState(false);
    const [curUserName, setCurUserName] = useState('');
    const [isLoggedIn, setIsLoggedIn] = useState(Authorization.isUserLoggedIn());

    const handleShowLogin = (bool) => {
        setShowLogin(bool);
    };
    const handleShowAccount = (bool) => {
        setShowAccount(bool);
    };
    const handleSetCurUserName = (username) => {
        setCurUserName(username);
    };
    const handleLogouot = async (username, password) => {
        await Authorization.logoutUser();
        handleSetIsLoggedIn(false);
        if (onLoggedOut) {
            onLoggedOut();
        }
    };
    const handleSetIsLoggedIn = (bool) => {
        setIsLoggedIn(bool)
    };

    useEffect(() => {
        const getAuthUser = async () => {
            try {
                const authUser = await Authorization.getAuthenticatedUser();
                if (authUser) {
                    handleSetIsLoggedIn(true);
                    handleSetCurUserName(authUser.username);
                } else {
                    handleSetIsLoggedIn(false);
                    handleSetCurUserName('');
                }
                
            } catch (error) {
                // Handle error, could set user to null or show error message
                console.error('Failed to get authenticated user', error);
            }
        };

        getAuthUser();
    }, [Authorization]);


    const handleGoToAdmin = () => {
        navigate('/admin');
    };


    return (
        <div>
            {!isLoggedIn && <IconButton color="inherit" onClick={handleShowLogin} style={{ cursor: 'pointer' }}>
                <LoginIcon /> <Typography variant="body1" style={{ marginLeft: 8 }}>
                    Login
                </Typography>
            </IconButton>}
            {isLoggedIn && <div>
                <div style={{ display: 'flex', alignItems: 'center', cursor: 'pointer' }}>
                    <Tooltip title="Site Admin" placement="top"><AdminPanelSettingsIcon onClick={handleGoToAdmin} /></Tooltip>
                    <Tooltip title="Change Password" placement="top"><AccountCircleIcon onClick={handleShowAccount} /></Tooltip>
                    <Typography variant="body1" style={{ marginLeft: 8 }}>
                        {curUserName}
                    </Typography>
                    <Tooltip title="Logout" placement="top"><LogoutIcon onClick={handleLogouot} style={{ cursor: 'pointer' }} /></Tooltip>
                </div>

            </div>}
            {showLogin && <ModalDialog open={showLogin} onClose={() => { handleShowLogin(false); } }>
                <div>
                    <AuthenticationWrapper onLoggedIn={onLoggedIn} onShowLogin={handleShowLogin} onSetIsLoggedIn={handleSetIsLoggedIn} onSetCurUserName={handleSetCurUserName} />
                </div>
            </ModalDialog>}
            {showAccount && <ModalDialog open={showAccount} onClose={() => { handleShowAccount(false); }}>
                <div>
                    <AccountWrapper onShowAccount={handleShowAccount} />
                </div>
            </ModalDialog>}
        </div>
    );
}
export default UserStatus;