import React, { useState } from "react";
import "./VerificationCode.css";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import TextField from "@mui/material/TextField";

const ForgotPasswordRequest = ({ onSend, onComponentChange }) => {
    const [curEmail, setCurEmail] = useState("");
    const [requestError, setRequestError] = useState("");


    const handleEmailChanged = (e) => {
        setCurEmail(e.target.value);
    };
    const handleSend = (curEmail) => {
        try {
            let sent = onSend(curEmail);
            if (sent) {
                handleComponentChange("ForgotPasswordSubmit");
            }
        } catch (err) {
            console.error("Error sending request", err);
            setRequestError(err.message || "Error sending request");
        }

    };
    const handleComponentChange = (component) => {
        onComponentChange(component);
    };


    return (
        <div className="code-container">
            <div className="code-form">
                <div className="input-group">
                    {requestError && <Alert severity="warning">Sending Request was not successful</Alert>}
                    <TextField
                        type="username"
                        id="username"
                        label="Email"
                        variant="standard"
                        value={curEmail}
                        onChange={handleEmailChanged}
                    />
                </div>
                <div className="actions">
                    <Button
                        color="primary"
                        variant="text"
                        type="button"
                        onClick={() => handleSend(curEmail)}
                        className="clear-button"
                    >   Send
                    </Button>
                    <Button
                        color="primary"
                        variant="text"
                        type="button"
                        onClick={() => handleComponentChange("Login")}
                        className="clear-button"
                    >   Back
                    </Button>
                </div>
            </div>
        </div>

    );
};

export default ForgotPasswordRequest;
