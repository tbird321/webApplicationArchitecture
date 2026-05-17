/// <reference path="../../../../reactcomponents/src/components/authorization/forgotpasswordsubmit.jsx" />
import React, { useState, useEffect } from 'react';
import { Auth } from 'aws-amplify';
import { Login, SignUp, VerificationCode, ForgotPasswordRequest, ForgotPasswordSubmit } from '@tbirdcomponents/reactcomponents';
import { useAppStateContext } from '../../hooks/appState/useAppStateContext';

function AuthenticationWrapper({ onLoggedIn, onShowLogin, onSetIsLoggedIn,onSetCurUserName }) {
    const [Component, setComponent] = useState("Login");
    const { Authorization } = useAppStateContext();


    const signUpEnabled = Authorization.getConfiguration().Site.signUpEnabled;

    const handleComponentChange = (component) => {
        console.log(component);
        setComponent(component);
    }
    const handleLogin = async (username, password) => {
        const loggedInUser = await Authorization.authenticateUser(username, password);
        if (loggedInUser != null) {
            onSetCurUserName(loggedInUser.username);
            onShowLogin(false);
            onSetIsLoggedIn(true);
            if (onLoggedIn) {
                onLoggedIn(loggedInUser);
            }
        } else {
            alert('error Logging in');
        }
    }

    
    const handleRegister = async (e, email, password) => {
        e?.preventDefault();
        try {
            const data = await Auth.signUp({
                username: email,
                password,
                attributes: {
                    email,
                },
            });
            console.log(data);
            handleComponentChange("VerificationCode");
        } catch (err) {
            console.error('Error signing up:', err);
        }
    };
    const handleResend = async (email) => {
        try {
            let result = await Auth.resendSignUp(email);
            console.log(result);
        } catch (err) {
            console.error('Error Sending Code:', err);

        }
    };
   

    const handleVerify = async (email, code) => {
        try {
            await Auth.confirmSignUp(email, code);
        } catch (err) {
            console.error('Error verifying email:', err);
        }

    };
    const handleForgotPasswordRequest = async(email) => {
        try {
            let result = await Auth.forgotPassword(email);
            console.log(result);
            handleComponentChange('ForgotPasswordSubmit');
        } catch (err) {
            console.error('Error sending Code:', err);
        }
    };

    const handleForgotPasswordSubmit = async (email, code, password) => {
        try {
            let result = await Auth.forgotPasswordSubmit(email, code, password);
            console.log(result);
            handleComponentChange('Login');
        } catch (err) {
            console.error('Error resetting password:', err);
        }
    };

    useEffect(() => {
        const getAuthUser = async () => {
            try {
                const authUser = await Authorization.getAuthenticatedUser();
                if (authUser) {
                    onSetIsLoggedIn(true);
                    onSetCurUserName(authUser.username);
                } else {
                    onSetIsLoggedIn(false);
                    onSetCurUserName('');
                }

            } catch (error) {
                // Handle error, could set user to null or show error message
                console.error('Failed to get authenticated user', error);
            }
        };

        getAuthUser();
    }, [Authorization, onSetCurUserName, onSetIsLoggedIn]);
    return (
        <div>
            {Component === "Login" && <Login signUpEnabled={signUpEnabled} onLoginClick={handleLogin} onComponentChange={handleComponentChange}></Login>}
            {Component === "SignUp" && <SignUp onRegister={handleRegister} onComponentChange={handleComponentChange}></SignUp>}
            {Component === "VerificationCode" && <VerificationCode onVerify={handleVerify} onResend={handleResend} onComponentChange={handleComponentChange}></VerificationCode>}
            {Component === "ForgotPasswordRequest" && <ForgotPasswordRequest onSend={handleForgotPasswordRequest} onComponentChange={handleComponentChange}></ForgotPasswordRequest>}
            {Component === "ForgotPasswordSubmit" && <ForgotPasswordSubmit onResetPassword={handleForgotPasswordSubmit} onComponentChange={handleComponentChange}></ForgotPasswordSubmit>}
        </div>
    );
}
export default AuthenticationWrapper;