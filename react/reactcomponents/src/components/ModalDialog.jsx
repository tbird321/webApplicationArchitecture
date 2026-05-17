import React, { useState, useEffect, useMemo } from "react";
import Modal from "@mui/material/Modal";
import { styled } from "@mui/system";
import IconButton from "@mui/material/IconButton";
import CloseIcon from "@mui/icons-material/Close";

function ModalDialog({ children, open, onClose, dialogStyle, childDirty, onDirtyClose }) {
    const [modalState, setModalState] = useState({ open, isDirty: childDirty ?? false });

    const ModalWrapper = useMemo(() => styled("div")(({ theme }) => ({
        ...dialogStyle,
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        backgroundColor: theme.palette.background.paper,
        boxShadow: theme.shadows[5],
        padding: theme.spacing(2, 4, 3),
        position: "absolute",
        top: "50%",
        left: "50%",
        transform: "translate(-50%, -50%)",
        maxWidth: "90%",
        maxHeight: "80%",
        outline: "none",
        borderRadius: theme.shape.borderRadius,
    })), [dialogStyle]);

    useEffect(() => {
        setModalState(prev => ({ ...prev, open }));
    }, [open]);

    useEffect(() => {
        setModalState(prev => ({ ...prev, isDirty: childDirty ?? false }));
    }, [childDirty]);

    const handleClose = (event, reason) => {
        if (!modalState.isDirty) {
            setModalState(prev => ({ ...prev, open: false }));
            onClose?.(event, reason);
        } else {
            onDirtyClose?.(event, reason);
        }
    };

    return (
        <Modal
            open={modalState.open}
            onClose={handleClose}
            aria-labelledby="modal-modal-title"
            aria-describedby="modal-modal-description"
            slotProps={{
                backdrop: {
                    style: {
                        backgroundColor: "rgba(0, 0, 0, 0.7)",
                    },
                },
            }}
        >
            <ModalWrapper>
                <IconButton
                    edge="end"
                    color="inherit"
                    onClick={handleClose}
                    aria-label="close"
                    sx={{ position: "absolute", top: 0, right: 0, marginRight: "-3px" }}
                >
                    <CloseIcon fontSize="small" />
                </IconButton>
                <div style={{ paddingTop: "30px" }}>
                    {children}
                </div>
            </ModalWrapper>
        </Modal>
    );
}

export default ModalDialog;
