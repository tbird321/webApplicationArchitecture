import React, { useState, useRef } from "react";
import "./Login.css";
import Alert from "@mui/material/Alert";
import  Button  from "@mui/material/Button";
import TextField from "@mui/material/TextField";

const Login = ({ username, onLoginClick, onComponentChange, signUpEnabled  }) => {
    const [curUsername, setCurUsername] = useState(username || "");
    const [password, setPassword] = useState("");
    const [usernameError, setUsernameError] = useState(false);
    const [passwordError, setPasswordError] = useState(false);
    const [nameHelperText, setnameHelperText] = useState("");
    const [passwordHelperText, setPasswordHelperText] = useState("");
    const [loginError, setLoginError] = useState("");


    const usernameInputRef = useRef(null);
    const passwordInputRef = useRef(null);

    const handleUserNameChanged = (e) => {
        setCurUsername(e.target.value);
    };
    const handlePasswordChanged = (e) => {
        setPassword(e.target.value);
    };
    const validateUsername = () => {
        let isValid = true;
        if (!curUsername) {
            isValid = false;
            setUsernameError(true);
            setnameHelperText("Username is required");
            usernameInputRef.current.focus();
        } else {
            setUsernameError(false);
            setnameHelperText("");
        }
        return isValid;
    };
    const handleComponentChange = (component) => {
        onComponentChange(component);
    };

    const validatePassword = () => {
        let isValid = true;
        if (!password) {
            isValid = false;
            setPasswordError(true);
            setPasswordHelperText("Password is required");
            passwordInputRef.current.focus();
        } else {
            setPasswordError(false);
            setPasswordHelperText("");
        }
        return isValid;
    };

    const handleLoginClick = (event) => {
        event.preventDefault();
        try {
            if (validateUsername() && validatePassword()) {
                onLoginClick(curUsername, password);
            }
        } catch (err) {
            console.error("Error Logging in", err);
            setLoginError(err.message || "Error Logging in");
        }
    };

    return (
        <form noValidate autoComplete="off" onSubmit={handleLoginClick}>
            <div className="login-container">
                <div className="login-form">
                    <div className="input-group">
                        {loginError && <Alert severity="warning">Login was not successful</Alert>}
                        <TextField error={usernameError}
                            inputRef={usernameInputRef}
                            helperText={nameHelperText}
                            type="username"
                            id="username"
                            label="Username" // You can change "Username" to whatever label you prefer
                            variant="standard"
                            value={curUsername} // value is taken from state
                            onChange={handleUserNameChanged} // method to set state, similar to your original input
                            onBlur={validateUsername }
                        />
                    </div>
                    <div className="input-group">
                        <TextField
                            error={passwordError}
                            inputRef={passwordInputRef}
                            helperText={passwordHelperText}
                            type="password"
                            id="password"
                            label="Password"
                            variant="standard"
                            value={password}
                            onChange={handlePasswordChanged}
                            onBlur={validatePassword}
                        />
                    </div>
                    <div className="actions">
                        <Button
                            color="primary"
                            variant="contained"
                            type="submit"
                            className="login-button"
                        >   Login
                        </Button>
                        <Button
                            color="primary"
                            variant="text"
                            type="button"
                            onClick={() => handleComponentChange("ForgotPasswordRequest")}
                            className="clear-button"
                        >   Forgot Password?
                        </Button>
                        {signUpEnabled === "true" && <Button
                            color="primary"
                            variant="text"
                            type="button"
                            onClick={() => handleComponentChange("SignUp")}
                            className="clear-button"
                        >   Sign up
                        </Button>}
                    </div>
                </div>
            </div>
        </form>
    );
};

export default Login;
