import React, { useState } from "react";
import ModalDialog from "../components/ModalDialog"; // Adjust the import path accordingly
import { TextField, Typography } from "@mui/material";
import { action } from "@storybook/addon-actions";

export default {
    title: "Components/ModalDialog",
    component: ModalDialog,
    argTypes: {
        // Define any arg types here if needed
    },
};

const Template = (args) => {
    const [lookupName, setLookupName] = useState("");

    return (
        <ModalDialog {...args}>
            <div>
                <Typography variant="h6">Modal Title</Typography>
                <p>Modal content goes here.</p>
                <TextField
                    label="Name"
                    value={lookupName}
                    onChange={(e) => setLookupName(e.target.value)}
                    style={{ marginRight: "16px" }}
                />
            </div>
        </ModalDialog>
    );
};

export const Default = Template.bind({});
Default.args = {
    open: true,
    onClose: action("onClose"),
};

const TemplateDirty = (args) => {
    const [lookupName, setLookupName] = useState("");

    return (
        <ModalDialog {...args}>
            <div>
                <Typography variant="h6">Modal Title</Typography>
                <p>Modal content goes here.</p>
                <TextField
                    label="Name"
                    value={lookupName}
                    onChange={(e) => setLookupName(e.target.value)}
                    style={{ marginRight: "16px" }}
                />
            </div>
        </ModalDialog>
    );
};

export const Dirty = TemplateDirty.bind({});
Dirty.args = {
    open: true,
    onClose: action("onClose"),
    childDirty: true,
    onDirtyClose: action("onDirtyClose"),
};
