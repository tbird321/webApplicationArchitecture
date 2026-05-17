import React from "react";
import ForgotPasswordSubmit from "../../components/authorization/ForgotPasswordSubmit";

export default {
    title: "Authorization/ForgotPasswordSubmit",
    component: ForgotPasswordSubmit,
    argTypes: {
        onResetPassword: { action: "Reset Password button clicked" },
        onComponentChange: { action: "navigation button clicked" },
    },
};

const Template = (args) => <ForgotPasswordSubmit {...args} />;

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
