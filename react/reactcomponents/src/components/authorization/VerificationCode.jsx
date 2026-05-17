import React, { useState } from "react";
import "./VerificationCode.css";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import TextField from "@mui/material/TextField";

const VerificationCode = ({ onVerify, onResend, onComponentChange }) => {
    const [code, setCode] = useState("");
    const [email, setEmail] = useState("");
    const [verificationError, setVerificationErrorError] = useState("");

    const handleVerify = (email,code) => {
        try {
            let verified = onVerify(email, code);
            if (verified) {
                handleComponentChange("Login");
            }
        }
        catch (err) {
            console.error("Error Verifying Email", err);
            setVerificationErrorError(err.message || "Error Verifying Email");
        }
    };
    const handleComponentChange = (component) => {
        onComponentChange(component);
    };
    return (
        <div className="code-container">
            <div className="code-form">
                <div className="input-group">
                    {verificationError && <Alert severity="warning">Email Verification not Successful</Alert>}
                    <TextField
                        type="username"
                        id="username"
                        label="Email"
                        variant="standard"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
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
                </div>
                <div className="actions">
                    <Button
                        color="primary"
                        variant="text"
                        type="button"
                        onClick={() => handleVerify(email, code)}
                        className="clear-button"
                    >   Verify
                    </Button>
                    <Button
                        color="primary"
                        variant="text"
                        type="button"
                        onClick={() => onResend(email)}
                        className="clear-button"
                    >   Resend Code
                    </Button>
                    <Button
                        color="primary"
                        variant="text"
                        type="button"
                        onClick={() => handleComponentChange("SignUp")}
                        className="clear-button"
                    >   Back
                    </Button>
                </div>
            </div>
        </div>

    );
};

export default VerificationCode;
