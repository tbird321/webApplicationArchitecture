import React, { useState, useRef } from "react";
import "./SignUp.css";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import TextField from "@mui/material/TextField";

const SignUp = ({ onRegister, onComponentChange }) => {
    const [curUsername, setCurUsername] = useState("");
    const [password, setPassword] = useState("");
    const [usernameError, setUsernameError] = useState(false);
    const [passwordError, setPasswordError] = useState(false);
    const [nameHelperText, setnameHelperText] = useState("");
    const [passwordHelperText, setPasswordHelperText] = useState("");
    const [registrationError, setRegistrationError] = useState("");


    const usernameInputRef = useRef(null);
    const passwordInputRef = useRef(null);

    const handleUserNameChanged = (e) => {
        setCurUsername(e.target.value);
    };
    const handlePasswordChanged = (e) => {
        setPassword(e.target.value);
    };
    const handleComponentChange = (component) => {
        onComponentChange(component);
    };
    const validateUsername = () => {
        let isValid = true;
        if (!curUsername) {
            isValid = false;
            setUsernameError(true);
            setnameHelperText("Username is required");
        } else {
            setUsernameError(false);
            setnameHelperText("");
        }
        return isValid;
    };

    const validatePassword = () => {
        let isValid = true;
        if (!password) {
            isValid = false;
            setPasswordError(true);
            setPasswordHelperText("Password is required");
            //passwordInputRef.current.focus();
        } else {
            setPasswordError(false);
            setPasswordHelperText("");
        }
        return isValid;
    };

    const handleRegisterClick = (e) => {
        e?.preventDefault();
        try {
            if (validateUsername() && validatePassword()) {
                onRegister(e, curUsername, password);
                handleComponentChange("VerificationCode");
            }
        } catch (err) {
            console.error("Error signing up:", err);
            setRegistrationError(err.message || "Error signing up");
        }
    };

    return (
        <form noValidate autoComplete="off" onSubmit={handleRegisterClick}>
            <div className="SignUp-container">
                <div className="SignUp-form">
                    <div className="input-group">
                        {registrationError && <Alert severity="warning">Registration was not successful</Alert>}
                        <TextField error={usernameError}
                            inputRef={usernameInputRef}
                            helperText={nameHelperText}
                            type="username"
                            id="username"
                            label="Username" // You can change "Username" to whatever label you prefer
                            variant="standard"
                            value={curUsername} // value is taken from state
                            onChange={handleUserNameChanged} // method to set state, similar to your original input
                            onBlur={validateUsername}
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
                            className="Register-button"
                            onClick={() => handleRegisterClick()}
                        >   Register
                        </Button>
                        <Button
                            color="primary"
                            variant="text"
                            type="button"
                            onClick={() => handleComponentChange("Login")}
                            className="clear-button"
                        >   Log in
                        </Button>
                    </div>
                </div>
            </div>
        </form>
    );
};

export default SignUp;
