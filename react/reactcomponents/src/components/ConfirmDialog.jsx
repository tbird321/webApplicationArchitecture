import React, { useState } from "react";
import ModalDialog from "./ModalDialog"; // Adjust the import path accordingly
import Button from "@mui/material/Button";

function ConfirmDialog({ isOpen, message, onConfirm, onCancel }) {
    const [dialogOpen, setDialogOpen] = useState(isOpen);

    const handleConfirm = () => {
        if (onConfirm) {
            onConfirm();
        }
        setDialogOpen(false); // Close the modal after confirming
    };

    const handleCancel = () => {
        if (onCancel) {
            onCancel();
        }
        setDialogOpen(false); // Close the modal after canceling
    };

    return (
        <ModalDialog open={dialogOpen} onClose={handleCancel}>
            <div className="confirm-dialog">
                <p>{message}</p>
                <div style={{ display: "flex", justifyContent: "space-between" }}>
                    <Button
                        color="primary"
                        variant="contained"
                        type="submit"
                        className="login-button"
                        onClick={handleConfirm}
                    >
                        Confirm
                    </Button>
                    <Button
                        color="primary"
                        variant="contained"
                        type="submit"
                        className="login-button"
                        onClick={handleCancel}
                    >
                        Cancel
                    </Button>
                </div>
            </div>
        </ModalDialog>
    );
}

export default ConfirmDialog;
