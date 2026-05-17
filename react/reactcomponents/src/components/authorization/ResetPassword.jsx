import React, { useState, useRef } from "react";
import "./VerificationCode.css";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import TextField from "@mui/material/TextField";

const ResetPassword = ({ onChangeAccountPassword }) => {
    const [currentpassword, setCurrentPassword] = useState("");
    const [newPassword, setNewPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [passwordError, setPasswordError] = useState(false);
    const [newPasswordError, setNewPasswordError] = useState(false);
    const [confirmPasswordError, setConfirmPasswordError] = useState(false);
    const [passwordHelperText, setPasswordHelperText] = useState("");
    const [newPasswordHelperText, setNewPasswordHelperText] = useState("");
    const [confirmPasswordHelperText, setConfirmPasswordHelperText] = useState("");
    const [passwordSubmitError, setPasswordSubmitError] = useState("");

    const passwordInputRef = useRef(null);
    const newPasswordInputRef = useRef(null);
    const confirmPasswordInputRef = useRef(null);

    const handleChangeAccountPassword = async (password, newPassword) => {
        let isValid = validateSubmit();
        if (isValid) {
            try {
                await onChangeAccountPassword(password, newPassword);
            } catch (err) {
                console.error("Could not Change Password", err);
                setPasswordSubmitError(err.message || "Could not Change Password");
            }
        }
        return isValid;
    };
    const validateSubmit = () => {
        let isValid = true;
        if (passwordError | newPasswordError| confirmPasswordError) {
            isValid = false;
        }
        return isValid;
    };

    const handleNewPasswordChanged = (e) => {
        setNewPassword(e.target.value);
    };
    const handleCurrentPasswordChanged = (e) => {
        setCurrentPassword(e.target.value);
    };
    const handleConfirmPasswordChanged = (e) => {
        setConfirmPassword(e.target.value);
    };

    const validateCurrentPassword = () => {
        let isValid = true;
        if (!currentpassword) {
            isValid = false;
            setPasswordError(true);
            setPasswordHelperText("Password is required");
        } else {
            setPasswordError(false);
            setPasswordHelperText("");
        }
        return isValid;
    };
    const validateNewPassword = () => {
        let isValid = true;
        if (!newPassword) {
            isValid = false;
            setNewPasswordError(true);
            setNewPasswordHelperText("Password is required");
        } else {
            setNewPasswordError(false);
            setNewPasswordHelperText("");
        }
        return isValid;
    };
    const validateConfirmPassword = () => {
        let isValid = true;
        if (confirmPassword !== newPassword) {
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
                    <TextField
                        error={passwordError}
                        inputRef={passwordInputRef}
                        helperText={passwordHelperText}
                        type="password"
                        id="password"
                        label="Current Password"
                        variant="standard"
                        className="code-input"
                        value={currentpassword}
                        onChange={handleCurrentPasswordChanged}
                        onBlur={validateCurrentPassword}
                    />
                    <TextField
                        error={newPasswordError}
                        inputRef={newPasswordInputRef}
                        helperText={newPasswordHelperText}
                        type="password"
                        id="password"
                        label=" New Password"
                        variant="standard"
                        className="code-input"
                        value={newPassword}
                        onChange={handleNewPasswordChanged}
                        onBlur={validateNewPassword}
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
                        onClick={() => handleChangeAccountPassword(currentpassword, newPassword)}
                        className="submit-button"
                    >   Reset Password
                    </Button>
                </div>
            </div>
        </div>
    );
};

export default ResetPassword;
