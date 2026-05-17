import React from "react";
import ConfirmDialog from "../components/ConfirmDialog"; // Adjust the import path accordingly
import { action } from "@storybook/addon-actions";

export default {
    title: "Components/ConfirmDialog",
    component: ConfirmDialog,
    argTypes: {
        isOpen: { control: "boolean" },
        message: { control: "text" },
        onConfirm: { action: "confirmed" },
        onCancel: { action: "canceled" },
    },
};

const Template = (args) => <ConfirmDialog {...args} />;

export const Default = Template.bind({});
Default.args = {
    isOpen: true,
    message: "Are you sure you want to proceed?",
    onConfirm: action("confirmed"),
    onCancel: action("canceled"),
};
