import React, { useState, useRef } from "react";
import "./VerificationCode.css";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import TextField from "@mui/material/TextField";

const ForgotPasswordSubmit = ({ onResetPassword, onComponentChange }) => {
    const [curEmail, setCurEmail] = useState("");
    const [code, setCode] = useState("");
    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [emailError, setEmailError] = useState(false);
    const [passwordError, setPasswordError] = useState(false);
    const [confirmPasswordError, setConfirmPasswordError] = useState(false);
    const [nameHelperText, setnameHelperText] = useState("");
    const [passwordHelperText, setPasswordHelperText] = useState("");
    const [confirmPasswordHelperText, setConfirmPasswordHelperText] = useState("");
    const [passwordSubmitError, setPasswordSubmitError] = useState("");

    const EmailInputRef = useRef(null);
    const passwordInputRef = useRef(null);
    const confirmPasswordInputRef = useRef(null);

    const handleComponentChange = (component) => {
        onComponentChange(component);
    };
    const handleResetPassword = (curEmail, code, password) => {
        try {
            const isValid = validateSubmit();
            if (isValid) {
                onResetPassword(curEmail, code, password);
            }
        } catch (err) {
            console.error("Error Changing Password", err);
            setPasswordSubmitError(err.message || "Error Changing Password");
        }

    };
    const handleEmailChanged = (e) => {
        setCurEmail(e.target.value);
    };
    const handlePasswordChanged = (e) => {
        setPassword(e.target.value);
    };
    const handleConfirmPasswordChanged = (e) => {
        setConfirmPassword(e.target.value);
    };
    const validateEmail = () => {
        let isValid = true;
        if (!curEmail) {
            isValid = false;
            setEmailError(true);
            setnameHelperText("Username is required");
            EmailInputRef.current.focus();
        } else {
            setEmailError(false);
            setnameHelperText("");
        }
        return isValid;
    };
    const validateSubmit = () => {
        let isValid = true;
        if (emailError | passwordError | confirmPasswordError) {
            isValid = false;
            setEmailError(true);
            setnameHelperText("Username is required");
            EmailInputRef.current.focus();
        } else {
            setEmailError(false);
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
            passwordInputRef.current.focus();
        } else {
            setPasswordError(false);
            setPasswordHelperText("");
        }
        return isValid;
    };
    const validateConfirmPassword = () => {
        let isValid = true;
        if (confirmPassword !== password) {
            isValid = false;
            setConfirmPasswordError(true);
            setConfirmPasswordHelperText("Passwords don't match");
        } else {
            setConfirmPasswordError(false);
            setConfirmPasswordHelperText("");
        }
        return isValid;
    };

    return (
        <div className="code-container">
            <div className="code-form">
                <div className="input-group">
                    {passwordSubmitError && <Alert severity="warning">Changing password was not successful</Alert>}
                    <TextField error={emailError}
                        inputRef={EmailInputRef}
                        helperText={nameHelperText}
                        type="username"
                        id="username"
                        label="Email" // You can change "Username" to whatever label you prefer
                        variant="standard"
                        value={curEmail} // value is taken from state
                        onChange={handleEmailChanged} // method to set state, similar to your original input
                        onBlur={validateEmail}
                        className="code-input"

                    />
                    <TextField
                        type="input"
                        id="outlined-input"
                        placeholder="Enter verification code"
                        value={code}
                        variant="standard"
                        label="Verification Code"
                        onChange={(e) => setCode(e.target.value)}
                        className="code-input"
                    />
                    <TextField
                        error={passwordError}
                        inputRef={passwordInputRef}
                        helperText={passwordHelperText}
                        type="password"
                        id="password"
                        label="Password"
                        variant="standard"
                        className="code-input"
                        value={password}
                        onChange={handlePasswordChanged}
                        onBlur={validatePassword}
                    />
                    <TextField
                        error={confirmPasswordError}
                        inputRef={confirmPasswordInputRef}
                        helperText={confirmPasswordHelperText}
                        type="password"
                        id="password"
                        label="Confirm Password"
                        variant="standard"
                        className="code-input"
                        value={confirmPassword}
                        onChange={handleConfirmPasswordChanged}
                        onBlur={validateConfirmPassword}
                    />
                </div>
                <div className="actions">
                    <Button
                        color="primary"
                        variant="contained"
                        type="button"
                        onClick={() => handleResetPassword(curEmail, code, password)}
                        className="submit-button"
                    >   Reset Password
                    </Button>
                    <Button
                        color="primary"
                        variant="text"
                        type="button"
                        onClick={() => handleComponentChange("ForgotPasswordRequest")}
                        className="clear-button"
                    >   Back
                    </Button>
                </div>
            </div>
        </div>
    );
};

export default ForgotPasswordSubmit;
