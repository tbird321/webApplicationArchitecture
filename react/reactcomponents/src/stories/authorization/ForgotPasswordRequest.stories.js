import React from "react";
import ForgotPasswordRequest from "../../components/authorization/ForgotPasswordRequest";

export default {
    title: "Authorization/ForgotPasswordRequest",
    component: ForgotPasswordRequest,
    argTypes: {
        onResend: { action: "resend code button clicked" },
        onComponentChange: { action: "navigation button clicked" },
    },
};

const Template = (args) => <ForgotPasswordRequest {...args} />;

export const DefaultVerificationCode = Template.bind({});
DefaultVerificationCode.args = {};

export const WithCallbacks = Template.bind({});
WithCallbacks.args = {
    onVerify: (code) => {
        console.log(`Verifying with code: ${code}`);
    },
    onResend: () => {
        console.log("Resend code button clicked");
    },
};
